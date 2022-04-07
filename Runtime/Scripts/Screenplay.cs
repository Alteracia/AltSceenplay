using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using UnityEngine;

using Alteracia.Patterns.ScriptableObjects;

namespace Alteracia.Screenplay
{
    [CreateAssetMenu(fileName = "Screenplay", menuName = "AltScreenplay/Screenplay", order = 1)]
    [Serializable]
    public class Screenplay : RootScriptableObject
    {
        [Tooltip("If screenplay was already executed play it again.")]
        [SerializeField] private bool executeAgain;
        
        [NonSerialized] private bool _executed;
        public bool Executed => _executed;

        public void Execute()
        {
            if (!executeAgain && _executed) return;
            
            foreach (var group in Nested.Cast<ISceneActionGroup>()) group.Execute();
            _executed = true;
        }

        public async Task ExecuteTask()
        {
            if (!executeAgain && _executed) return;
            
            List<Task> tasks = Nested.Cast<ISceneActionGroup>().Select(@group => @group.Execute()).ToList();
            foreach (var tsk in tasks) await tsk;
            
            _executed = true;
        }

        public void ReplaceScene(string olAndNewSceneName)
        {
            var oldnew = olAndNewSceneName.Split('=');
            // TODO Add exception
            var actionsGroups = Nested.Cast<ISceneActionGroup>().ToArray();
            if (actionsGroups.All(g => g.Name != oldnew[0])) return; // TODO Add exception
            actionsGroups.First(g => g.Name == oldnew[0]).Replace(oldnew[1]);
        }

        public void Cancel()
        {
            _executed = false;
            foreach (var group in Nested.Cast<ISceneActionGroup>()) group.Cancel();
        }
        
#if UNITY_EDITOR
        
        [ContextMenu("Add New Scene ActionGroup")]
        private void AddSceneActionGroup() => AddNested<SceneGroupWithPreAndPostActions>();
        
#endif
    }
}