using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FakeMG.Framework.ActionMapManagement
{
    public class ActionMapManager : MonoBehaviour
    {
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private ActionMapConflictsPairsSO _conflictPairsSO;
        [ActionMapName]
        [SerializeField] private string _initialActionMapName;

        [Header("Debug")]
        [SerializeField] private bool _enableLogging;

        private readonly HashSet<string> _activeMaps = new();
        // Track suppressors for suppressed maps
        private readonly Dictionary<string, HashSet<string>> _suppressedBy = new();

        private void Start()
        {
            if (!string.IsNullOrEmpty(_initialActionMapName))
            {
                EnableActionMap(_initialActionMapName);
            }
        }

        public void EnableActionMap(string mapName)
        {
            // Disable conflicts first
            HashSet<string> conflicts = _conflictPairsSO.GetConflictsFor(mapName);
            foreach (string conflictName in conflicts)
            {
                InputActionMap conflictMap = _inputActions.FindActionMap(conflictName);
                if (conflictMap != null && conflictMap.enabled)
                {
                    conflictMap.Disable();
                    _activeMaps.Remove(conflictName);

                    if (!_suppressedBy.TryGetValue(conflictName, out var suppressors))
                    {
                        suppressors = new HashSet<string>();
                        _suppressedBy[conflictName] = suppressors;
                    }

                    suppressors.Add(mapName);

                    Echo.Log($"Disabled conflicting action map: {conflictName} due to enabling {mapName}", _enableLogging);
                }
            }

            // Enable the map if not already active
            InputActionMap map = _inputActions.FindActionMap(mapName);
            if (map != null && !map.enabled)
            {
                map.Enable();
                _activeMaps.Add(mapName);
                _suppressedBy.Remove(mapName);
                Echo.Log($"Enabled action map: {mapName}", _enableLogging);
            }
        }

        public void DisableActionMap(string mapName)
        {
            // Disable the map if active
            InputActionMap map = _inputActions.FindActionMap(mapName);
            if (map != null && map.enabled)
            {
                map.Disable();
                _activeMaps.Remove(mapName);
                Echo.Log($"Disabled action map: {mapName}", _enableLogging);
            }

            // Remove this map as a suppressor from any suppressed maps
            // ToList to avoid modification during iteration
            foreach (var (suppressedName, suppressors) in _suppressedBy.ToList())
            {
                if (suppressors.Remove(mapName))
                {
                    if (suppressors.Count == 0)
                    {
                        // No more suppressors—re-enable the map
                        InputActionMap suppressedMap = _inputActions.FindActionMap(suppressedName);
                        if (suppressedMap != null)
                        {
                            suppressedMap.Enable();
                            _activeMaps.Add(suppressedName);
                            _suppressedBy.Remove(suppressedName);
                            Echo.Log($"Re-enabled action map: {suppressedName} after disabling {mapName}", _enableLogging);
                        }
                    }
                }
            }
        }

        public bool IsActionMapActive(string mapName) => _activeMaps.Contains(mapName);

        // Optional: Switch to a new map, disabling all non-global first
        public void SwitchToActionMap(string newMapName)
        {
            // Disable all non-always-enabled active maps
            foreach (string activeMap in _activeMaps.ToArray())
            {
                DisableActionMap(activeMap);
            }

            EnableActionMap(newMapName);
        }
    }
}