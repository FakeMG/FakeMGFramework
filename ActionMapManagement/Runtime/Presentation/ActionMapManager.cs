using System;
using System.Collections.Generic;
using System.Linq;
using FakeMG.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FakeMG.ActionMapManagement
{
    /// <summary>
    /// Enables and disables the project's action maps. Enabling a map disables the maps it conflicts with until it is
    /// disabled again, and suspensions hold maps off without forgetting which ones are enabled.
    /// </summary>
    public class ActionMapManager : MonoBehaviour, IActionMapManager
    {
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private ActionMapConflictsPairsSO _conflictPairsSO;
        [SerializeField] private List<ActionMapSO> _initialActionMapSOs;

        [Header("Debug")]
        [SerializeField] private bool _enableLogging;

        /// <remarks>Maps the game wants on. Each is live unless a suspension holds it off.</remarks>
        private readonly HashSet<string> _activeMaps = new();
        /// <remarks>Maps switched off by enabling a conflicting map, with the maps that did so.</remarks>
        private readonly Dictionary<string, HashSet<string>> _suppressedBy = new();
        private readonly ActionMapSuspensions _suspensions = new();

        #region Unity Lifecycle

        /// <remarks>
        /// Project-wide actions arrive with every map enabled. Left on, a map outside the initial set would be recorded as
        /// suppressed by its conflicts and switched on again once they are disabled, without anyone enabling it.
        /// Done in Awake, before any caller can enable a map through this manager.
        /// </remarks>
        private void Awake()
        {
            _inputActions.Disable();
            _suspensions.OnChanged += ApplyAllMapStates;
        }

        private void Start()
        {
            foreach (ActionMapSO actionMapSO in _initialActionMapSOs)
            {
                EnableActionMap(actionMapSO.ActionMapName);
            }
        }

        private void OnDestroy()
        {
            _suspensions.OnChanged -= ApplyAllMapStates;
        }

        #endregion

        #region Public Methods

        public bool IsActionMapActive(string mapName) => _activeMaps.Contains(mapName);

        public void EnableActionMap(string mapName)
        {
            if (_inputActions.FindActionMap(mapName) == null || _activeMaps.Contains(mapName))
            {
                Echo.Warning($"Attempted to enable action map: {mapName}, but it was not found or already enabled.", _enableLogging);
                return;
            }

            _activeMaps.Add(mapName);
            _suppressedBy.Remove(mapName);
            ApplyMapState(mapName);
            Echo.Log($"Enabled action map: {mapName}", _enableLogging);
            SuppressConflictingActionMaps(mapName);
        }

        public void DisableActionMap(string mapName)
        {
            if (_inputActions.FindActionMap(mapName) != null && _activeMaps.Remove(mapName))
            {
                ApplyMapState(mapName);
                Echo.Log($"Disabled action map: {mapName}", _enableLogging);
            }
            else
            {
                // Suppression tracking is still cleaned up below.
                Echo.Warning($"Attempted to disable action map: {mapName}, but it was not found or already disabled.", _enableLogging);
            }

            RemoveFromSuppressionTracking(mapName);
        }

        public IDisposable SuspendAllActionMaps(string reason)
        {
            Echo.Log($"Suspended all action maps: {reason}", _enableLogging);
            return _suspensions.SuspendAll(reason);
        }

        public IDisposable SuspendActionMaps(string reason, IReadOnlyCollection<string> mapNames)
        {
            Echo.Log($"Suspended action maps {string.Join(", ", mapNames)}: {reason}", _enableLogging);
            return _suspensions.Suspend(reason, mapNames);
        }

        /// <summary>Disables every enabled map, then enables the given one.</summary>
        public void SwitchToActionMap(string newMapName)
        {
            foreach (string activeMap in _activeMaps.ToArray())
            {
                DisableActionMap(activeMap);
            }

            EnableActionMap(newMapName);
        }

        #endregion

        #region Private Methods

        private void SuppressConflictingActionMaps(string mapName)
        {
            foreach (string conflictName in _conflictPairsSO.GetConflictsFor(mapName))
            {
                if (_inputActions.FindActionMap(conflictName) == null)
                {
                    continue;
                }

                if (_activeMaps.Remove(conflictName))
                {
                    ApplyMapState(conflictName);
                    _suppressedBy[conflictName] = new HashSet<string>();
                    Echo.Log($"Disabled conflicting action map: {conflictName} due to enabling {mapName}", _enableLogging);
                }

                // Also adds this map as a further suppressor of a map already suppressed by another.
                if (_suppressedBy.TryGetValue(conflictName, out HashSet<string> suppressors))
                {
                    suppressors.Add(mapName);
                    Echo.Log($"Action map: {conflictName} is now suppressed by {mapName}", _enableLogging);
                }
            }
        }

        private void RemoveFromSuppressionTracking(string mapName)
        {
            _suppressedBy.Remove(mapName);
            foreach ((string suppressedName, HashSet<string> suppressors) in _suppressedBy.ToList())
            {
                if (!suppressors.Remove(mapName) || suppressors.Count > 0)
                {
                    continue;
                }

                _suppressedBy.Remove(suppressedName);
                _activeMaps.Add(suppressedName);
                ApplyMapState(suppressedName);
                Echo.Log($"Re-enabled action map: {suppressedName} after disabling {mapName}", _enableLogging);
            }
        }

        private void ApplyAllMapStates()
        {
            foreach (InputActionMap map in _inputActions.actionMaps)
            {
                ApplyMapState(map.name);
            }
        }

        /// <summary>Turns the map on exactly when it is enabled and no suspension holds it off.</summary>
        private void ApplyMapState(string mapName)
        {
            InputActionMap map = _inputActions.FindActionMap(mapName);
            bool isLive = _activeMaps.Contains(mapName) && !_suspensions.IsSuspended(mapName);
            if (isLive)
            {
                map.Enable();
            }
            else
            {
                map.Disable();
            }
        }

        #endregion
    }
}
