using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Events;

using Alteracia.Patterns;
using Alteracia.Patterns.ScriptableObjects;

namespace Alteracia.Screenplay
{
    public interface ISceneActionGroup
    {
        Action<float> OnLoadingProgress { get; set; }
        string Name { get; }
        Task Execute();
        void Replace(string newSceneId);
        void Cancel();
    }

    [CreateAssetMenu(fileName = "SceneActionGroup", menuName = "AltScreenplay/SceneActionGroup", order = 2)]
    [Serializable]
    public class SceneGroupWithPreAndPostActions : NestedScriptableObject, ISceneActionGroup
    {
        private Action<float> _onLoadingProgress;
        public  Action<float> OnLoadingProgress
        {
            get => _onLoadingProgress;
            set => _onLoadingProgress = value;
        }

        public string Name => this.name;
        public enum SceneOperation { Add, Load, Reload }

        [Header("Actions")]
        [SerializeField] private UnityEvent<string> preActions;

        [Space] [SerializeField] private bool repeatPreActionsWhenReplaced; // TODO Deprecate
        [SerializeField] private UnityEvent<string> preReplaceActions;
        
        [NonSerialized] private string _scene; 
        public string SceneName => string.IsNullOrEmpty(_scene) ? this.name : _scene;
        
        [Space] 
        [SerializeField] private SceneOperation operation = SceneOperation.Add;
        [SerializeField] private bool active;
        [Space][Space] 
        [SerializeField] private  UnityEvent<string> postActions;
        
        [NonSerialized] private bool _canceled;
        [NonSerialized] private bool _executed;
        [NonSerialized] private Task _execution;
        [NonSerialized] private bool _replaced;
        public bool Replaced => _replaced;

        public async Task Execute()
        {
            if (repeatPreActionsWhenReplaced || !Replaced) preActions.Invoke(SceneName); // TODO remove preReplaceActions
            if (Replaced) preReplaceActions.Invoke(SceneName);
            
            _canceled = false;
            _executed = true;

            SceneLoader loader = SceneLoadingUtils.IsSceneRemote(SceneName)
                ? new AddressableSceneLoader(SceneName, _onLoadingProgress) as SceneLoader
                : new BuildInSceneLoader(SceneName, _onLoadingProgress) as SceneLoader;

            switch (operation)
            {
                case SceneOperation.Load: _execution = loader.LoadScene(); break;
                case SceneOperation.Add: _execution = loader.AddScene(); break;
                case SceneOperation.Reload: _execution = loader.ReloadScene(); break;
                default: throw new ArgumentOutOfRangeException();
            }

            await _execution;

            _execution = null;
            
            if (_canceled || loader.Id != SceneName) // canceled or scene changed
            {
                SceneLoadingUtils.RemoveScene(loader.Id);
                return;
            }

            if (active && !_replaced) // Do not activate if scene is replaced Will activate after removing scene
            {
                SceneLoadingUtils.ActivateScene(SceneName);
                await AltTasks.WaitFrames(1);
            }
            // TODO Add options as in pre
            postActions.Invoke(SceneName);
        }

        private List<string> _oldScenes = new List<string>();

        public async void Replace(string newSceneId)
        {
            if (newSceneId == SceneName) return;
            
            var update = _executed && operation == SceneOperation.Add; // TODO case if  || operation == SceneOperation.Load =? in cases other scenes added

            var oldScene = SceneName;
            _oldScenes.Add(oldScene);
            
            _scene = newSceneId;

            if (!update) return;
            
            var id = newSceneId; // TODO check

            if (_execution != null)
            {
                await _execution;
                if (_scene != id || _canceled)
                {
                    SceneLoadingUtils.RemoveScene(oldScene); // TODO Check
                    return; // Scene was replaced again
                }
            }

            _replaced = true;
            await Execute();
            SceneLoadingUtils.RemoveScene(oldScene);
            if (_canceled) return;
            if (active) SceneLoadingUtils.ActivateScene(SceneName);
        }

        public void Cancel()
        {
            _executed = false;
            _replaced = false;
            
            if (operation != SceneOperation.Reload) SceneLoadingUtils.RemoveScene(SceneName);
            if (_oldScenes.Count != 0) foreach (var scene in _oldScenes) SceneLoadingUtils.RemoveScene(scene);
            _oldScenes.Clear();
            _canceled = true;
        }
    }
}