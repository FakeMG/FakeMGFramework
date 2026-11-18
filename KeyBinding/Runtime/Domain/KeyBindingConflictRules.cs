using System;
using System.Collections.Generic;

namespace FakeMG.KeyBinding
{
    /// <summary>
    /// Two bindings clash when they use the same control and their action maps can be live at the same time. Maps that
    /// exclude each other (a menu that silences gameplay) may share keys, e.g. Q rotates the camera and pages the build menu.
    /// </summary>
    public sealed class KeyBindingConflictRules
    {
        public const int NO_CONFLICT = -1;

        private readonly HashSet<(string, string)> _exclusiveMapPairs = new();

        public KeyBindingConflictRules(IEnumerable<(string firstMapName, string secondMapName)> exclusiveMapPairs)
        {
            foreach ((string firstMapName, string secondMapName) in exclusiveMapPairs)
            {
                _exclusiveMapPairs.Add((firstMapName, secondMapName));
                _exclusiveMapPairs.Add((secondMapName, firstMapName));
            }
        }

        #region Public Methods

        /// <returns>The index of the first other slot that would clash with the new control, or <see cref="NO_CONFLICT"/>.</returns>
        public int FindConflictingSlotIndex(IReadOnlyList<BoundKey> slots, int reboundSlotIndex, string newControlPath)
        {
            if (string.IsNullOrEmpty(newControlPath))
            {
                return NO_CONFLICT;
            }

            string reboundMapName = slots[reboundSlotIndex].ActionMapName;
            for (int slotIndex = 0; slotIndex < slots.Count; slotIndex++)
            {
                if (slotIndex == reboundSlotIndex)
                {
                    continue;
                }

                BoundKey other = slots[slotIndex];
                if (string.Equals(other.ControlPath, newControlPath, StringComparison.OrdinalIgnoreCase) &&
                    CanBeLiveTogether(reboundMapName, other.ActionMapName))
                {
                    return slotIndex;
                }
            }

            return NO_CONFLICT;
        }

        #endregion

        #region Private Methods

        private bool CanBeLiveTogether(string firstMapName, string secondMapName)
        {
            return !_exclusiveMapPairs.Contains((firstMapName, secondMapName));
        }

        #endregion
    }
}
