using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Alteracia.Screenplay
{
    [CreateAssetMenu(fileName = "ScreenplayDirector", menuName = "AltScreenplay/ScreenplayDirector", order = 0)]
    public class ScreenplayDirector : ScriptableObject
    {
        [SerializeField] private Screenplay[] screenplays;
        [NonSerialized] private List<Screenplay> _screenplays = new List<Screenplay>();
        public Screenplay[] Screenplays => _screenplays.Count == 0 ? screenplays : _screenplays.ToArray();
        
        [NonSerialized] private readonly List<Screenplay[]> _saved = new List<Screenplay[]>();
        public int SavedCount => _saved.Count;
        
        public void Execute(string screenplay)
        {
            if (Screenplays.All(s => s.name != screenplay)) return; // TODO Add exception
            Screenplays.First(s => s.name == screenplay).Execute();
        }

        public void SaveState()
        {
            _saved.Add(Screenplays.Where(s => s.Executed).ToArray());
        }

        public void Restore()
        {
            if (_saved.Count == 0) return;
        
            var lastSaved = _saved.Last();
            _saved.RemoveAt(_saved.Count - 1);
        
            RestoreState(lastSaved);
        }

        public void RestoreFirstState()
        {
            if (_saved.Count == 0) return;
        
            var first = _saved[0];
            _saved.Clear();
            
           RestoreState(first);
        }

        public void CancelAllExcept(string screenPlays)
        {
            var screenPlaysSplit = ScreenPlaysSplit(screenPlays);

            foreach (var sp in Screenplays)
            {
                if (sp.Executed && !screenPlaysSplit.Contains(sp.name))
                    sp.Cancel();
            }
        }
        
        public void Cancel(string screenPlays)
        {
            var screenPlaysSplit = ScreenPlaysSplit(screenPlays);
            
            foreach (var sp in Screenplays)
            {
                if (sp.Executed && screenPlaysSplit.Contains(sp.name))
                    sp.Cancel();
            }
        }

        public void CancelAll()
        {
            foreach (var sp in Screenplays) if (sp.Executed) sp.Cancel();
        }

        public void AddScreenPlay(Screenplay screenplay)
        {
            _screenplays = Screenplays.ToList();
            
            if (_screenplays.Any(s => s.name == screenplay.name))
            {
                _screenplays.RemoveAt(_screenplays.FindIndex(s => s.name == screenplay.name));
            }
            
            _screenplays.Add(screenplay);
        }

        public void ReplaceScene(string screenplayOldSceneNewScene)
        {
            var screenplayScenes = screenplayOldSceneNewScene.Split('/');
           // var oldnew = screenplayScenes[1].Split('=');
            var screenplay = Screenplays.First(s => s.name == screenplayScenes[0]); // Add Check
            screenplay.ReplaceScene(screenplayScenes[1]);
        }
        
        private static string[] ScreenPlaysSplit(string screenPlays)
        {
            var screenPlaysSplit = screenPlays.Split('&');
            if (screenPlaysSplit.Length == 0) screenPlaysSplit = new[] {screenPlays};
            return screenPlaysSplit;
        }
        
        private void RestoreState(Screenplay[] state)
        {
            // Cancel all unsaved screenplays
            foreach (var cur in Screenplays.Where(s => s.Executed))
            {
                if (state.All(saved => saved != cur)) cur.Cancel();
            }

            foreach (var save in state) save.Execute();
        }
    }
}