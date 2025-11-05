using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.Framework.ActionMapManagement
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.ROOT + "/ActionMapConflictsPairsSO")]
    public class ActionMapConflictsPairsSO : ScriptableObject
    {
        [SerializeField] private List<ConflictPair> _conflictPairs = new();

        public IReadOnlyList<ConflictPair> ConflictPairs => _conflictPairs;

        public HashSet<string> GetConflictsFor(string actionMapName)
        {
            HashSet<string> conflicts = new();

            foreach (ConflictPair pair in _conflictPairs)
            {
                if (pair.ActionMapA == actionMapName)
                {
                    conflicts.Add(pair.ActionMapB);
                }
                else if (pair.ActionMapB == actionMapName)
                {
                    conflicts.Add(pair.ActionMapA);
                }
            }

            return conflicts;
        }
    }

    [Serializable]
    public class ConflictPair
    {
        [ActionMapName]
        public string ActionMapA;

        [ActionMapName]
        public string ActionMapB;
    }
}
