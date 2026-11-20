using System;
using System.Collections.Generic;

namespace FakeMG.KeyBinding
{
    /// <summary>
    /// Two bindings conflict when they use the same control and their action maps can be live at the same time. Maps that
    /// can never be live together (a menu that silences gameplay) may share keys, e.g. Q rotates the camera and pages the
    /// build menu.
    /// </summary>
    public sealed class KeyBindingConflictRules
    {
        private readonly HashSet<(string, string)> _neverLiveTogetherMapPairs = new();

        /// <param name="neverLiveTogetherMapPairs">Pairs of action maps that are never enabled at the same time.</param>
        public KeyBindingConflictRules(IEnumerable<(string firstMapName, string secondMapName)> neverLiveTogetherMapPairs)
        {
            foreach ((string firstMapName, string secondMapName) in neverLiveTogetherMapPairs)
            {
                _neverLiveTogetherMapPairs.Add((firstMapName, secondMapName));
                _neverLiveTogetherMapPairs.Add((secondMapName, firstMapName));
            }
        }

        #region Public Methods

        /// <returns>The first other slot that conflicts with the changed slot's control, or null when none does.</returns>
        public TSlot FindConflictingSlot<TSlot>(IReadOnlyList<TSlot> slots, TSlot changedSlot) where TSlot : class, IBoundKey
        {
            if (string.IsNullOrEmpty(changedSlot.ControlPath))
            {
                return null;
            }

            foreach (TSlot other in slots)
            {
                if (other != changedSlot &&
                    string.Equals(other.ControlPath, changedSlot.ControlPath, StringComparison.OrdinalIgnoreCase) &&
                    CanBeLiveTogether(changedSlot.ActionMapName, other.ActionMapName))
                {
                    return other;
                }
            }

            return null;
        }

        #endregion

        #region Private Methods

        private bool CanBeLiveTogether(string firstMapName, string secondMapName)
        {
            return !_neverLiveTogetherMapPairs.Contains((firstMapName, secondMapName));
        }

        #endregion
    }
}
