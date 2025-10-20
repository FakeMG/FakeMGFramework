using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FakeMG.Framework.ActionMapManagement
{
    public class ActionMapManager : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private List<ActionMapConfigSO> allConfigs;

        [Header("Debug")]
        [SerializeField] private bool enableLogging;

        // For quick access by name
        private Dictionary<string, ActionMapConfigSO> _configLookup;
        private readonly HashSet<string> _activeMaps = new();
        // Track suppressors for suppressed maps
        private readonly Dictionary<string, HashSet<string>> _suppressedBy = new();

        private void Awake()
        {
            // Build lookup dictionary
            _configLookup = allConfigs.ToDictionary(config => config.ActionMapName, config => config);

            // Enable always-on maps at start
            foreach (var config in allConfigs.Where(c => c.isAlwaysEnabled))
            {
                EnableActionMap(config.ActionMapName);
            }
        }

        public void EnableActionMap(string mapName)
        {
            if (!_configLookup.TryGetValue(mapName, out var config))
            {
                Echo.Warning(enableLogging, $"No config found for action map: {mapName}");
                return;
            }

            // Disable conflicts first
            foreach (var conflictName in config.ConflictsWith)
            {
                if (!_configLookup.TryGetValue(conflictName, out var conflictConfig))
                {
                    continue;
                }

                if (conflictConfig.isAlwaysEnabled)
                {
                    continue; // Don't disable always-enabled maps
                }

                var conflictMap = inputActions.FindActionMap(conflictName);
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

                    Echo.Log(enableLogging, $"Disabled conflicting action map: {conflictName} due to enabling {mapName}");
                }
            }

            // Enable the map if not already active
            var map = inputActions.FindActionMap(mapName);
            if (map != null && !map.enabled)
            {
                map.Enable();
                _activeMaps.Add(mapName);
                // Clear any suppression entry (since it's now active)
                _suppressedBy.Remove(mapName);
                Echo.Log(enableLogging, $"Enabled action map: {mapName}");
            }
        }

        public void DisableActionMap(string mapName)
        {
            if (!_configLookup.TryGetValue(mapName, out var config))
            {
                Echo.Warning(enableLogging, $"No config found for action map: {mapName}");
                return;
            }

            // Skip if it's always enabled (explicit disable ignored for them)
            if (config.isAlwaysEnabled)
            {
                return;
            }

            // Disable the map if active
            var map = inputActions.FindActionMap(mapName);
            if (map != null && map.enabled)
            {
                map.Disable();
                _activeMaps.Remove(mapName);
                Echo.Log(enableLogging, $"Disabled action map: {mapName}");
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
                        var suppressedMap = inputActions.FindActionMap(suppressedName);
                        if (suppressedMap != null)
                        {
                            suppressedMap.Enable();
                            _activeMaps.Add(suppressedName);
                            _suppressedBy.Remove(suppressedName);
                            Echo.Log(enableLogging, $"Re-enabled action map: {suppressedName} after disabling {mapName}");
                        }
                    }
                }
            }
        }

        // Helper to check if a map is active
        public bool IsActionMapActive(string mapName) => _activeMaps.Contains(mapName);

        // Optional: Switch to a new map, disabling all non-global first
        public void SwitchToActionMap(string newMapName)
        {
            // Disable all non-always-enabled active maps
            foreach (var activeMap in _activeMaps.ToArray())
            {
                if (!_configLookup[activeMap].isAlwaysEnabled)
                {
                    DisableActionMap(activeMap);
                }
            }

            EnableActionMap(newMapName);
        }
    }
}