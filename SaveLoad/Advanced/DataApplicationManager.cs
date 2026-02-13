using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FakeMG.Framework.SaveLoad.Advanced
{
    /// <summary>
    /// Manages data application for systems in loaded scenes.
    /// Handles registration, timeout management, and completion tracking.
    /// </summary>
    public class DataApplicationManager : Singleton<DataApplicationManager>
    {
        [Header("Timeout Settings")]
        [SerializeField] private float _applicationTimeoutSeconds = 10f;

        [Header("Debug")]
        [SerializeField] private bool _enableDebug = true;

        private readonly Dictionary<string, List<DataRequester>> _sceneRequesters = new();
        private readonly Dictionary<string, HashSet<DataRequester>> _pendingRequesters = new();
        private readonly Dictionary<string, UniTaskCompletionSource<bool>> _sceneCompletionSources = new();

        public event Action<string> OnSceneDataApplicationComplete;
        public event Action<string, DataRequester> OnSystemDataApplicationComplete;
        public event Action<string, DataRequester, string> OnSystemDataApplicationFailed;

        #region Helper Methods
        /// <summary>
        /// Get a readable identifier for a system for logging purposes
        /// </summary>
        private string GetSystemIdentifier(DataRequester requester)
        {
            if (requester is MonoBehaviour monoBehaviour)
            {
                return $"{monoBehaviour.GetType().Name}({monoBehaviour.name})";
            }

            return requester.GetType().Name;
        }
        #endregion

        #region System Registration
        public void RegisterDataRequester(DataRequester requester)
        {
            if (requester == null)
            {
                Echo.Error("Cannot register null data requester", _enableDebug, this);
                return;
            }

            string sceneName = requester.SceneName;

            if (!_sceneRequesters.ContainsKey(sceneName))
            {
                _sceneRequesters[sceneName] = new List<DataRequester>();
            }

            _sceneRequesters[sceneName].Add(requester);

            Echo.Log(
                $"[DataApplicationManager] Registered {GetSystemIdentifier(requester)} for scene {sceneName}", _enableDebug, this);
        }

        public void UnregisterDataRequester(DataRequester requester)
        {
            if (requester == null) return;

            string sceneName = requester.SceneName;

            if (_sceneRequesters.ContainsKey(sceneName))
            {
                _sceneRequesters[sceneName].Remove(requester);

                if (_sceneRequesters[sceneName].Count == 0)
                {
                    _sceneRequesters.Remove(sceneName);
                }
            }

            // Remove from pending if present
            if (_pendingRequesters.TryGetValue(sceneName, out var pendingRequester))
            {
                pendingRequester.Remove(requester);
            }

            Echo.Log(
                $"[DataApplicationManager] Unregistered {GetSystemIdentifier(requester)} from scene {sceneName}", _enableDebug, this);
        }
        #endregion

        #region Data Application
        /// <summary>
        /// Trigger data application for all systems in a specific scene
        /// </summary>
        public async UniTask<bool> ApplyDataForSceneAsync(string sceneName)
        {
            if (!_sceneRequesters.ContainsKey(sceneName) || _sceneRequesters[sceneName].Count == 0)
            {
                Echo.Log($"[DataApplicationManager] No systems registered for scene {sceneName}", _enableDebug, this);

                return true;
            }

            Echo.Log(
                $"[DataApplicationManager] Starting data application for scene {sceneName} with {_sceneRequesters[sceneName].Count} systems", _enableDebug, this);

            // Set up completion tracking
            var completionSource = new UniTaskCompletionSource<bool>();
            _sceneCompletionSources[sceneName] = completionSource;
            _pendingRequesters[sceneName] = new HashSet<DataRequester>(_sceneRequesters[sceneName]);

            // Start application for all systems
            foreach (var requester in _sceneRequesters[sceneName])
            {
                ApplyDataForRequesterAsync(sceneName, requester).Forget();
            }

            // Wait for completion or timeout
            var timeoutTask = UniTask.Delay(TimeSpan.FromSeconds(_applicationTimeoutSeconds));
            var completionTask = completionSource.Task;

            var (hasResultLeft, hasResultRight) = await UniTask.WhenAny(completionTask, timeoutTask);

            if (!hasResultLeft) // Timeout occurred
            {
                Echo.Error(
                    $"[DataApplicationManager] Timeout applying data for scene {sceneName}. Remaining systems: {string.Join(", ", _pendingRequesters[sceneName].Select(GetSystemIdentifier))}", _enableDebug, this);

                // Complete with failure, but continue
                completionSource.TrySetResult(false);
            }

            bool success = await completionSource.Task;

            // Cleanup
            _pendingRequesters.Remove(sceneName);
            _sceneCompletionSources.Remove(sceneName);

            Echo.Log(
                $"[DataApplicationManager] Data application for scene {sceneName} completed. Success: {success}", _enableDebug, this);

            OnSceneDataApplicationComplete?.Invoke(sceneName);
            return success;
        }

        private async UniTask ApplyDataForRequesterAsync(string sceneName, DataRequester requester)
        {
            try
            {
                // Apply data
                await requester.ApplyDataAsync();

                Echo.Log(
                    $"[DataApplicationManager] Successfully applied data for {GetSystemIdentifier(requester)}", _enableDebug, this);

                // Mark as complete
                OnSystemDataApplicationComplete?.Invoke(sceneName, requester);
                MarkRequesterComplete(sceneName, requester);
            }
            catch (Exception e)
            {
                string errorMsg = $"Error applying data for {GetSystemIdentifier(requester)}: {e.Message}";
                Echo.Error($"[DataApplicationManager] {errorMsg}", _enableDebug, this);

                OnSystemDataApplicationFailed?.Invoke(sceneName, requester, errorMsg);
                MarkRequesterComplete(sceneName, requester); // Still mark as complete to not block others
            }
        }

        private void MarkRequesterComplete(string sceneName, DataRequester requester)
        {
            if (_pendingRequesters.ContainsKey(sceneName))
            {
                _pendingRequesters[sceneName].Remove(requester);

                // Check if all systems are complete
                if (_pendingRequesters[sceneName].Count == 0)
                {
                    _sceneCompletionSources[sceneName]?.TrySetResult(true);
                }
            }
        }
        #endregion

        #region Public API
        /// <summary>
        /// Wait for the data application to complete for a specific scene
        /// </summary>
        public async UniTask<bool> WaitForSceneDataApplicationAsync(string sceneName, float timeoutSeconds = 30f)
        {
            if (!_sceneCompletionSources.ContainsKey(sceneName))
            {
                // No data application in progress for this scene
                return true;
            }

            var timeoutTask = UniTask.Delay(TimeSpan.FromSeconds(timeoutSeconds));
            var completionTask = _sceneCompletionSources[sceneName].Task;

            var (hasResultLeft, hasResultRight) = await UniTask.WhenAny(completionTask, timeoutTask);

            if (!hasResultLeft) // Timeout
            {
                Echo.Error($"[DataApplicationManager] Timeout waiting for scene {sceneName} data application", _enableDebug, this);
                return false;
            }

            return await completionTask;
        }

        public int GetRegisteredSystemCount(string sceneName)
        {
            return _sceneRequesters.TryGetValue(sceneName, out var requester) ? requester.Count : 0;
        }

        public int GetPendingSystemCount(string sceneName)
        {
            return _pendingRequesters.TryGetValue(sceneName, out var pendingRequester) ? pendingRequester.Count : 0;
        }

        public bool IsDataApplicationInProgress(string sceneName)
        {
            return _sceneCompletionSources.ContainsKey(sceneName);
        }
        #endregion

#if UNITY_EDITOR
        [Button("Log Registered Systems")]
        private void LogRegisteredSystems()
        {
            Echo.Log("=== DataApplicationManager Registered Systems ===", _enableDebug, this);
            foreach (var kvp in _sceneRequesters)
            {
                Echo.Log($"Scene: {kvp.Key} ({kvp.Value.Count} systems)", _enableDebug, this);
                foreach (var requester in kvp.Value)
                {
                    Echo.Log($"  - {GetSystemIdentifier(requester)}", _enableDebug, this);
                }
            }
        }
#endif
    }
}