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
        [SerializeField] private ActionMapSO _initialActionMap;

        [Header("Debug")]
        [SerializeField] private bool _enableLogging;

        private readonly HashSet<string> _activeMaps = new();
        // Track suppressors for suppressed maps
        private readonly Dictionary<string, HashSet<string>> _suppressedBy = new();
        private bool _isPaused;

        private void Start()
        {
            if (_initialActionMap)
            {
                EnableActionMap(_initialActionMap.ActionMapName);
            }
        }

        public void EnableActionMap(string mapName)
        {
            // Enable the map if not already active
            InputActionMap map = _inputActions.FindActionMap(mapName);
            if (map != null && !_activeMaps.Contains(mapName))
            {
                map.Enable();
                _activeMaps.Add(mapName);
                _suppressedBy.Remove(mapName);
                Echo.Log($"Enabled action map: {mapName}", _enableLogging);
            }
            else
            {
                Echo.Warning($"Attempted to enable action map: {mapName}, but it was not found or already enabled.", _enableLogging);
                return;
            }

            DisableConflictingActionMaps(mapName);
        }

        public void DisableActionMap(string mapName)
        {
            // Disable the map if active
            InputActionMap map = _inputActions.FindActionMap(mapName);
            if (map != null && _activeMaps.Contains(mapName))
            {
                map.Disable();
                _activeMaps.Remove(mapName);
                Echo.Log($"Disabled action map: {mapName}", _enableLogging);
            }
            else
            {
                Echo.Warning($"Attempted to disable action map: {mapName}, but it was not found or already disabled.", _enableLogging);
                // Don't return here to ensure suppressed tracking is cleaned up
            }

            RemoveFromSuppressionTracking(mapName);
        }

        private void DisableConflictingActionMaps(string mapName)
        {
            HashSet<string> conflicts = _conflictPairsSO.GetConflictsFor(mapName);
            foreach (string conflictName in conflicts)
            {
                InputActionMap conflictMap = _inputActions.FindActionMap(conflictName);
                if (conflictMap != null)
                {
                    // If conflict is active, disable it and track suppression
                    if (conflictMap.enabled)
                    {
                        conflictMap.Disable();
                        _activeMaps.Remove(conflictName);

                        if (!_suppressedBy.TryGetValue(conflictName, out var suppressors))
                        {
                            suppressors = new HashSet<string>();
                            _suppressedBy[conflictName] = suppressors;
                        }

                        Echo.Log($"Disabled conflicting action map: {conflictName} due to enabling {mapName}", _enableLogging);
                    }

                    // If is disabled due to prior suppression, add another suppressor
                    if (_suppressedBy.TryGetValue(conflictName, out HashSet<string> suppressors2))
                    {
                        suppressors2.Add(mapName);
                        Echo.Log($"Action map: {conflictName} is now suppressed by {mapName}", _enableLogging);
                    }
                }
            }
        }

        private void RemoveFromSuppressionTracking(string mapName)
        {
            // Remove from suppressed tracking
            _suppressedBy.Remove(mapName);

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

        public void PauseAllActionMaps()
        {
            if (_isPaused)
            {
                Echo.Warning("Inputs are already paused.", _enableLogging);
                return;
            }

            _inputActions.Disable();
            _isPaused = true;
            Echo.Log("Paused all inputs.", _enableLogging);
        }

        public void ResumeAllActionMaps()
        {
            if (!_isPaused)
            {
                Echo.Warning("Inputs are not paused.", _enableLogging);
                return;
            }

            // Re-enable only the action maps that were active before pausing
            foreach (string mapName in _activeMaps)
            {
                InputActionMap map = _inputActions.FindActionMap(mapName);
                map?.Enable();
            }

            _isPaused = false;
            Echo.Log("Unpaused all inputs.", _enableLogging);
        }

        // Optional: Switch to a new map, disabling all others
        public void SwitchToActionMap(string newMapName)
        {
            // Disable all active maps
            foreach (string activeMap in _activeMaps.ToArray())
            {
                DisableActionMap(activeMap);
            }

            EnableActionMap(newMapName);
        }
    }
}