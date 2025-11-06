using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FakeMG.Framework.ActionMapManagement
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.ACTION_MAP_MANAGEMENT + "/ActionMapConflictsPairsSO")]
    public class ActionMapConflictsPairsSO : ScriptableObject
    {
        [SerializeField]
        [ValidateInput(nameof(ValidateNoDuplicates), "Duplicate conflict pairs detected! Click 'Remove Duplicate Pairs' button to clean up.", InfoMessageType.Warning)]
        private List<ConflictPair> _conflictPairs = new();

        public IReadOnlyList<ConflictPair> ConflictPairs => _conflictPairs;

        private bool ValidateNoDuplicates(List<ConflictPair> pairs)
        {
            return !HasDuplicates();

            bool HasDuplicates()
            {
                HashSet<(int, int)> seen = new();

                foreach (ConflictPair pair in _conflictPairs)
                {
                    if (!pair.ActionMapA || !pair.ActionMapB)
                        continue;

                    if (pair.ActionMapA == pair.ActionMapB)
                        return true;

                    int idA = pair.ActionMapA.GetInstanceID();
                    int idB = pair.ActionMapB.GetInstanceID();

                    (int, int) normalizedPair = idA < idB ? (idA, idB) : (idB, idA);

                    if (!seen.Add(normalizedPair))
                        return true;
                }

                return false;
            }
        }

#if UNITY_EDITOR
        [Button]
        [PropertyOrder(-1)]
        private void RemoveDuplicatePairs()
        {
            HashSet<(int, int)> seen = new();
            List<ConflictPair> uniquePairs = new();

            foreach (ConflictPair pair in _conflictPairs)
            {
                if (!pair.ActionMapA || !pair.ActionMapB)
                    continue;

                if (pair.ActionMapA == pair.ActionMapB)
                    continue;

                int idA = pair.ActionMapA.GetInstanceID();
                int idB = pair.ActionMapB.GetInstanceID();

                // Normalize pair order to treat (A,B) and (B,A) as identical
                var normalizedPair = idA < idB ? (idA, idB) : (idB, idA);

                if (seen.Add(normalizedPair))
                {
                    uniquePairs.Add(pair);
                }
            }

            if (_conflictPairs.Count != uniquePairs.Count)
            {
                _conflictPairs = uniquePairs;
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif

        public HashSet<string> GetConflictsFor(string actionMapName)
        {
            HashSet<string> conflicts = new();

            foreach (ConflictPair pair in _conflictPairs)
            {
                if (pair.ActionMapA && pair.ActionMapA.ActionMapName == actionMapName)
                {
                    if (pair.ActionMapB)
                    {
                        conflicts.Add(pair.ActionMapB.ActionMapName);
                    }
                }
                else if (pair.ActionMapB && pair.ActionMapB.ActionMapName == actionMapName)
                {
                    if (pair.ActionMapA)
                    {
                        conflicts.Add(pair.ActionMapA.ActionMapName);
                    }
                }
            }

            return conflicts;
        }
    }

    [Serializable]
    public class ConflictPair
    {
        [Required("Action Map A is required")]
        [ValidateInput(nameof(ValidateDifferentMaps), "Action Maps must be different")]
        public ActionMapSO ActionMapA;

        [Required("Action Map B is required")]
        public ActionMapSO ActionMapB;

        private bool ValidateDifferentMaps()
        {
            return !ActionMapA || !ActionMapB || ActionMapA != ActionMapB;
        }
    }
}