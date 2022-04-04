using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Alteracia.Screenplay
{
    [CreateAssetMenu(fileName = "ScreenplayDirector", menuName = "AltScreenplay/ScreenplayDirector", order = 0)]
    public class ScreenplayDirector : ScriptableObject
    {
        public Screenplay[] Screenplays;

        private List<Screenplay[]> _saved = new List<Screenplay[]>();
        
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
        
            // Cancel all unsaved screenplays
            foreach (var cur in Screenplays.Where(s => s.Executed))
            {
                if (lastSaved.All(saved => saved != cur)) cur.Cancel();
            }

            foreach (var save in lastSaved) save.Execute();
        }
    }
}