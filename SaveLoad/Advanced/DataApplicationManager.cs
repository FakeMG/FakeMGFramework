using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FakeMG.Framework.SaveLoad.Advanced
{
    /// <summary>
    /// Manages data application for systems in loaded scenes.
    /// Handles registration, timeout management, and completion tracking.
    /// </summary>
    public class DataApplicationManager : MonoBehaviour
    {
        [Header("Timeout Settings")]
        [SerializeField] private float registrationTimeoutSeconds = 5f;
        [SerializeField] private float applicationTimeoutSeconds = 10f;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        public static DataApplicationManager Instance { get; private set; }

        private readonly Dictionary<string, List<IDataRequester>> _sceneRequesters = new();
        private readonly Dictionary<string, HashSet<IDataRequester>> _pendingRequesters = new();
        private readonly Dictionary<string, UniTaskCompletionSource<bool>> _sceneCompletionSources = new();

        public event Action<string> OnSceneDataApplicationComplete;
        public event Action<string, IDataRequester> OnSystemDataApplicationComplete;
        public event Action<string, IDataRequester, string> OnSystemDataApplicationFailed;

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        #region Helper Methods
        /// <summary>
        /// Get a readable identifier for a system for logging purposes
        /// </summary>
        private string GetSystemIdentifier(IDataRequester requester)
        {
            if (requester is MonoBehaviour monoBehaviour)
            {
                return $"{monoBehaviour.GetType().Name}({monoBehaviour.name})";
            }

            return requester.GetType().Name;
        }
        #endregion

        #region System Registration
        /// <summary>
        /// Register a system that needs data for a specific scene
        /// </summary>
        public void RegisterDataRequester(IDataRequester requester)
        {
            if (requester == null)
            {
                Debug.LogError("Cannot register null data requester");
                return;
            }

            string sceneName = requester.SceneName;

            if (!_sceneRequesters.ContainsKey(sceneName))
            {
                _sceneRequesters[sceneName] = new List<IDataRequester>();
            }

            _sceneRequesters[sceneName].Add(requester);

            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[DataApplicationManager] Registered {GetSystemIdentifier(requester)} for scene {sceneName}");
            }
        }

        /// <summary>
        /// Unregister a system
        /// </summary>
        public void UnregisterDataRequester(IDataRequester requester)
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

            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[DataApplicationManager] Unregistered {GetSystemIdentifier(requester)} from scene {sceneName}");
            }
        }
        #endregion

        #region Data Application
        /// <summary>
        /// Trigger data application for all systems in a specific scene
        /// </summary>
        public async UniTask<bool> ApplyDataForSceneAsync(string sceneName)
        {
            if (SaveLoadSystem.Instance == null)
            {
                Debug.LogError("SaveLoadSystem instance not found");
                return false;
            }

            if (!_sceneRequesters.ContainsKey(sceneName) || _sceneRequesters[sceneName].Count == 0)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"[DataApplicationManager] No systems registered for scene {sceneName}");
                }

                return true;
            }

            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[DataApplicationManager] Starting data application for scene {sceneName} with {_sceneRequesters[sceneName].Count} systems");
            }

            // Set up completion tracking
            var completionSource = new UniTaskCompletionSource<bool>();
            _sceneCompletionSources[sceneName] = completionSource;
            _pendingRequesters[sceneName] = new HashSet<IDataRequester>(_sceneRequesters[sceneName]);

            // Start application for all systems
            foreach (var requester in _sceneRequesters[sceneName])
            {
                ApplyDataForRequesterAsync(sceneName, requester).Forget();
            }

            // Wait for completion or timeout
            var timeoutTask = UniTask.Delay(TimeSpan.FromSeconds(applicationTimeoutSeconds));
            var completionTask = completionSource.Task;

            var (hasResultLeft, hasResultRight) = await UniTask.WhenAny(completionTask, timeoutTask);

            if (!hasResultLeft) // Timeout occurred
            {
                Debug.LogError(
                    $"[DataApplicationManager] Timeout applying data for scene {sceneName}. Remaining systems: {string.Join(", ", _pendingRequesters[sceneName].Select(r => GetSystemIdentifier(r)))}");

                // Complete with failure, but continue
                completionSource.TrySetResult(false);
            }

            bool success = await completionSource.Task;

            // Cleanup
            _pendingRequesters.Remove(sceneName);
            _sceneCompletionSources.Remove(sceneName);

            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[DataApplicationManager] Data application for scene {sceneName} completed. Success: {success}");
            }

            OnSceneDataApplicationComplete?.Invoke(sceneName);
            return success;
        }

        private async UniTask ApplyDataForRequesterAsync(string sceneName, IDataRequester requester)
        {
            try
            {
                // Apply data
                await requester.ApplyDataAsync();

                if (enableDebugLogs)
                {
                    Debug.Log(
                        $"[DataApplicationManager] Successfully applied data for {GetSystemIdentifier(requester)}");
                }

                // Mark as complete
                OnSystemDataApplicationComplete?.Invoke(sceneName, requester);
                MarkRequesterComplete(sceneName, requester);
            }
            catch (Exception e)
            {
                string errorMsg = $"Error applying data for {GetSystemIdentifier(requester)}: {e.Message}";
                Debug.LogError($"[DataApplicationManager] {errorMsg}");

                OnSystemDataApplicationFailed?.Invoke(sceneName, requester, errorMsg);
                MarkRequesterComplete(sceneName, requester); // Still mark as complete to not block others
            }
        }

        private void MarkRequesterComplete(string sceneName, IDataRequester requester)
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
        /// Wait for data application to complete for a specific scene
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
                Debug.LogError($"[DataApplicationManager] Timeout waiting for scene {sceneName} data application");
                return false;
            }

            return await completionTask;
        }

        /// <summary>
        /// Get the number of systems registered for a scene
        /// </summary>
        public int GetRegisteredSystemCount(string sceneName)
        {
            return _sceneRequesters.TryGetValue(sceneName, out var requester) ? requester.Count : 0;
        }

        /// <summary>
        /// Get the number of systems still pending for a scene
        /// </summary>
        public int GetPendingSystemCount(string sceneName)
        {
            return _pendingRequesters.TryGetValue(sceneName, out var pendingRequester) ? pendingRequester.Count : 0;
        }

        /// <summary>
        /// Check if data application is in progress for a scene
        /// </summary>
        public bool IsDataApplicationInProgress(string sceneName)
        {
            return _sceneCompletionSources.ContainsKey(sceneName);
        }
        #endregion

        #region Debug
        [ContextMenu("Log Registered Systems")]
        private void LogRegisteredSystems()
        {
            Debug.Log("=== DataApplicationManager Registered Systems ===");
            foreach (var kvp in _sceneRequesters)
            {
                Debug.Log($"Scene: {kvp.Key} ({kvp.Value.Count} systems)");
                foreach (var requester in kvp.Value)
                {
                    Debug.Log($"  - {GetSystemIdentifier(requester)}");
                }
            }
        }
        #endregion
    }
}