using System;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Events;

using Alteracia.Patterns;
using Alteracia.Patterns.ScriptableObjects;

namespace Alteracia.Screenplay
{
    public interface ISceneActionGroup
    {
        string Name { get; }
        Task Execute();
        void Replace(string newSceneId);
        void Cancel();
    }

    [CreateAssetMenu(fileName = "SceneActionGroup", menuName = "AltScreenplay/SceneActionGroup", order = 2)]
    [Serializable]
    public class SceneGroupWithPreAndPostActions : NestedScriptableObject, ISceneActionGroup
    {
        public enum SceneOperation { Add, Load, Reload }

        [Header("Actions")]
        [SerializeField] private  UnityEvent preActions;
        [Space]
        
        [NonSerialized] private string _scene;
        public string SceneName => string.IsNullOrEmpty(_scene) ? this.name : _scene;
        
        [SerializeField] private SceneOperation operation = SceneOperation.Add;
        [SerializeField] private bool active;
        [Space]
        [SerializeField] private  UnityEvent postActions;

        [NonSerialized] private bool _canceled;
        [NonSerialized] private bool _executed;

        public string Name => this.name;

        public async Task Execute()
        {
            _canceled = false;
            _executed = true;
            
            // Should we block this actions in case: scene loading -- scene changed -- executing with new scene?
            preActions.Invoke();

            SceneLoader loader = SceneLoadingUtils.IsSceneRemote(SceneName)
                ? new AddressableSceneLoader(SceneName) as SceneLoader
                : new BuildInSceneLoader(SceneName) as SceneLoader;

            switch (operation)
            {
                case SceneOperation.Load: await loader.LoadScene(); break;
                case SceneOperation.Add: await loader.AddScene(); break;
                case SceneOperation.Reload: await loader.ReloadScene(); break;
                default: throw new ArgumentOutOfRangeException();
            }
            
            if (_canceled || loader.Id != SceneName) // canceled or scene changed
            {
                SceneLoadingUtils.RemoveScene(loader.Id);
                return;
            }

            if (active)
            {
                SceneLoadingUtils.ActivateScene(SceneName);
                await AltTasks.WaitFrames(1);
            }

            postActions.Invoke();
        }
        
        public async void Replace(string newSceneId)
        {
            if (newSceneId == SceneName) return;
            
            var update = _executed && operation == SceneOperation.Add; // TODO case if  || operation == SceneOperation.Load =? in cases other scenes added
            if (update) SceneLoadingUtils.RemoveScene(SceneName);

            _scene = newSceneId;

            if (update) await Execute();
        }

        public void Cancel()
        {
            _executed = false;
            
            if (operation != SceneOperation.Reload) SceneLoadingUtils.RemoveScene(SceneName);
            
            _canceled = true;
        }
    }
}