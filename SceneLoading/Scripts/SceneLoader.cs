using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using VContainer;

namespace FakeMG.SceneLoading
{
    public enum SceneLoadStatus
    {
        Ready = 0,
        RawLoadFailed = 1,
        DataApplicationFailed = 2,
        Cancelled = 3,
        Busy = 4,
    }

    public readonly struct SceneLoadResult
    {
        public SceneLoadStatus Status { get; }
        public Scene LoadedScene { get; }
        public SceneDataApplicationResult DataApplicationResult { get; }
        public string FailureReason { get; }
        public bool Succeeded => Status == SceneLoadStatus.Ready;

        public SceneLoadResult(
            SceneLoadStatus status,
            Scene loadedScene,
            SceneDataApplicationResult dataApplicationResult,
            string failureReason)
        {
            Status = status;
            LoadedScene = loadedScene;
            DataApplicationResult = dataApplicationResult;
            FailureReason = failureReason ?? string.Empty;
        }
    }

    public sealed class SceneDataApplicationConfiguration
    {
        public float TimeoutSeconds { get; }

        public SceneDataApplicationConfiguration(float timeoutSeconds)
        {
            TimeoutSeconds = timeoutSeconds > 0f
                ? timeoutSeconds
                : throw new ArgumentOutOfRangeException(nameof(timeoutSeconds));
        }
    }

    public sealed class SceneLoader : MonoBehaviour
    {
        private readonly Dictionary<string, SceneHandleState> _sceneStatesByAssetGuid = new();

        private ISceneDataApplicationCoordinator _dataApplicationCoordinator;
        private SceneDataApplicationConfiguration _dataApplicationConfiguration;
        private CancellationTokenSource _lifetimeCancellationSource;

        public event Action<AssetReferenceScene> OnSceneLoaded;
        public event Action<AssetReferenceScene> OnSceneUnloaded;
        public event Action<AssetReferenceScene, string> OnSceneLoadFailed;
        public event Action<AssetReferenceScene, string> OnSceneUnloadFailed;

        #region Unity Lifecycle

        private void Awake()
        {
            _lifetimeCancellationSource = new CancellationTokenSource();
        }

        private void OnDestroy()
        {
            _lifetimeCancellationSource.Cancel();
            foreach (SceneHandleState sceneState in _sceneStatesByAssetGuid.Values)
            {
                ReleaseSceneHandleDuringTeardown(sceneState);
            }

            _sceneStatesByAssetGuid.Clear();
            _lifetimeCancellationSource.Dispose();
            _lifetimeCancellationSource = null;
        }

        #endregion

        #region Public Methods

        [Inject]
        public void Construct(
            ISceneDataApplicationCoordinator dataApplicationCoordinator,
            SceneDataApplicationConfiguration dataApplicationConfiguration)
        {
            _dataApplicationCoordinator = dataApplicationCoordinator;
            _dataApplicationConfiguration = dataApplicationConfiguration;
        }

        public async UniTask<SceneLoadResult> LoadSceneAsync(
            AssetReferenceScene sceneReference,
            LoadSceneMode loadMode = LoadSceneMode.Additive,
            CancellationToken cancellationToken = default)
        {
            if (sceneReference == null)
            {
                const string FAILURE_REASON = "Scene reference is required.";
                Echo.Error(FAILURE_REASON);
                return CreateFailure(SceneLoadStatus.RawLoadFailed, FAILURE_REASON);
            }

            SceneHandleState sceneState = GetOrCreateState(sceneReference);
            if (sceneState.IsBusy)
            {
                string failureReason = $"Scene '{sceneReference}' is busy.";
                Echo.Warning(failureReason);
                return CreateFailure(SceneLoadStatus.Busy, failureReason);
            }

            if (TryGetLoadedScene(sceneState, out Scene existingScene))
            {
                return await ApplySceneDataAsync(
                    sceneReference,
                    existingScene,
                    cancellationToken);
            }

            using CancellationTokenSource linkedCancellationSource =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetimeCancellationSource.Token);
            AsyncOperationHandle<SceneInstance> loadHandle = default;
            sceneState.IsLoading = true;
            try
            {
                loadHandle = Addressables.LoadSceneAsync(sceneReference, loadMode);
                await loadHandle.ToUniTask(cancellationToken: linkedCancellationSource.Token);
                if (loadHandle.Status != AsyncOperationStatus.Succeeded || !loadHandle.Result.Scene.IsValid())
                {
                    string failureReason = loadHandle.OperationException?.ToString() ?? $"Addressables did not return a valid scene for '{sceneReference}'.";
                    ReleaseFailedLoadHandle(loadHandle);
                    OnSceneLoadFailed?.Invoke(sceneReference, failureReason);
                    Echo.Error(failureReason);
                    return CreateFailure(SceneLoadStatus.RawLoadFailed, failureReason);
                }

                sceneState.LoadedSceneHandle = loadHandle;
                SceneLoadResult result = await ApplySceneDataAsync(sceneReference, loadHandle.Result.Scene, linkedCancellationSource.Token);
                if (!result.Succeeded)
                {
                    OnSceneLoadFailed?.Invoke(sceneReference, result.FailureReason);
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                ReleaseFailedLoadHandle(loadHandle);
                string failureReason = $"Loading scene '{sceneReference}' was cancelled.";
                Echo.Warning(failureReason);
                return CreateFailure(SceneLoadStatus.Cancelled, failureReason);
            }
            catch (Exception exception)
            {
                ReleaseFailedLoadHandle(loadHandle);
                string failureReason = $"Loading scene '{sceneReference}' failed: {exception}";
                OnSceneLoadFailed?.Invoke(sceneReference, failureReason);
                Echo.Error(failureReason);
                return CreateFailure(SceneLoadStatus.RawLoadFailed, failureReason);
            }
            finally
            {
                sceneState.IsLoading = false;
            }
        }

        public async UniTask<bool> UnloadSceneAsync(AssetReferenceScene sceneReference, CancellationToken cancellationToken = default)
        {
            if (sceneReference == null ||
                !_sceneStatesByAssetGuid.TryGetValue(sceneReference.AssetGUID, out SceneHandleState sceneState) ||
                !sceneState.LoadedSceneHandle.HasValue)
            {
                string failureReason = $"Scene '{sceneReference}' is not loaded.";
                Echo.Warning(failureReason);
                return false;
            }

            if (sceneState.IsBusy)
            {
                Echo.Warning($"Scene '{sceneReference}' is busy and cannot be unloaded.");
                return false;
            }

            sceneState.IsUnloading = true;
            AsyncOperationHandle<SceneInstance> loadedHandle = sceneState.LoadedSceneHandle.Value;
            try
            {
                AsyncOperationHandle<SceneInstance> unloadHandle = Addressables.UnloadSceneAsync(loadedHandle, true);
                await unloadHandle.ToUniTask(cancellationToken: cancellationToken);
                sceneState.LoadedSceneHandle = null;
                _sceneStatesByAssetGuid.Remove(sceneReference.AssetGUID);
                OnSceneUnloaded?.Invoke(sceneReference);
                return true;
            }
            catch (Exception exception)
            {
                string failureReason = $"Unloading scene '{sceneReference}' failed: {exception}";
                OnSceneUnloadFailed?.Invoke(sceneReference, failureReason);
                Echo.Error(failureReason);
                return false;
            }
            finally
            {
                sceneState.IsUnloading = false;
            }
        }

        public async UniTask<SceneLoadResult> ReloadSceneAsync(AssetReferenceScene sceneReference, CancellationToken cancellationToken = default)
        {
            bool didUnload = await UnloadSceneAsync(sceneReference, cancellationToken);
            if (!didUnload)
            {
                return CreateFailure(SceneLoadStatus.RawLoadFailed, $"Scene '{sceneReference}' could not be unloaded for reload.");
            }

            return await LoadSceneAsync(sceneReference, LoadSceneMode.Additive, cancellationToken);
        }

        public bool IsSceneLoaded(AssetReferenceScene sceneReference)
        {
            return sceneReference != null &&
                   _sceneStatesByAssetGuid.TryGetValue(sceneReference.AssetGUID, out SceneHandleState sceneState) &&
                   TryGetLoadedScene(sceneState, out _);
        }

        public bool SetActiveScene(AssetReferenceScene sceneReference)
        {
            if (sceneReference != null &&
                _sceneStatesByAssetGuid.TryGetValue(sceneReference.AssetGUID, out SceneHandleState sceneState) &&
                TryGetLoadedScene(sceneState, out Scene scene))
            {
                return SceneManager.SetActiveScene(scene);
            }

            Echo.Warning($"Cannot activate scene '{sceneReference}' because it is not loaded.");
            return false;
        }

        #endregion

        #region Private Methods

        private async UniTask<SceneLoadResult> ApplySceneDataAsync(
            AssetReferenceScene sceneReference,
            Scene scene,
            CancellationToken cancellationToken)
        {
            SceneDataApplicationResult applicationResult =
                await _dataApplicationCoordinator.ApplyLoadedDataAsync(
                    scene,
                    _dataApplicationConfiguration.TimeoutSeconds,
                    cancellationToken);
            if (!applicationResult.Succeeded)
            {
                return new SceneLoadResult(
                    SceneLoadStatus.DataApplicationFailed,
                    scene,
                    applicationResult,
                    applicationResult.FailureReason);
            }

            OnSceneLoaded?.Invoke(sceneReference);
            return new SceneLoadResult(SceneLoadStatus.Ready, scene, applicationResult, string.Empty);
        }

        private SceneHandleState GetOrCreateState(AssetReferenceScene sceneReference)
        {
            if (!_sceneStatesByAssetGuid.TryGetValue(sceneReference.AssetGUID, out SceneHandleState sceneState))
            {
                sceneState = new SceneHandleState(sceneReference);
                _sceneStatesByAssetGuid.Add(sceneReference.AssetGUID, sceneState);
            }

            return sceneState;
        }

        private static bool TryGetLoadedScene(SceneHandleState sceneState, out Scene scene)
        {
            if (sceneState.LoadedSceneHandle.HasValue)
            {
                AsyncOperationHandle<SceneInstance> handle = sceneState.LoadedSceneHandle.Value;
                if (handle.IsValid() &&
                    handle.Status == AsyncOperationStatus.Succeeded &&
                    handle.Result.Scene.IsValid())
                {
                    scene = handle.Result.Scene;
                    return true;
                }

                Echo.Warning($"Clearing invalid Addressables handle for '{sceneState.SceneReference}'.");
                sceneState.LoadedSceneHandle = null;
            }

            scene = default;
            return false;
        }

        private static void ReleaseFailedLoadHandle(AsyncOperationHandle<SceneInstance> loadHandle)
        {
            if (loadHandle.IsValid())
            {
                Addressables.Release(loadHandle);
            }
        }

        private static void ReleaseSceneHandleDuringTeardown(SceneHandleState sceneState)
        {
            if (!sceneState.LoadedSceneHandle.HasValue)
            {
                return;
            }

            AsyncOperationHandle<SceneInstance> handle = sceneState.LoadedSceneHandle.Value;
            sceneState.LoadedSceneHandle = null;
            if (!handle.IsValid())
            {
                Echo.Warning($"Scene '{sceneState.SceneReference}' had an invalid handle during teardown.");
                return;
            }

            if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result.Scene.IsValid())
            {
                Addressables.UnloadSceneAsync(handle, true);
            }
            else
            {
                Addressables.Release(handle);
            }
        }

        private static SceneLoadResult CreateFailure(SceneLoadStatus status, string failureReason)
        {
            return new SceneLoadResult(
                status,
                default,
                new SceneDataApplicationResult(
                    false,
                    false,
                    Array.Empty<string>(),
                    failureReason),
                failureReason);
        }

        private sealed class SceneHandleState
        {
            public AssetReferenceScene SceneReference { get; }
            public AsyncOperationHandle<SceneInstance>? LoadedSceneHandle { get; set; }
            public bool IsLoading { get; set; }
            public bool IsUnloading { get; set; }
            public bool IsBusy => IsLoading || IsUnloading;

            public SceneHandleState(AssetReferenceScene sceneReference)
            {
                SceneReference = sceneReference;
            }
        }

        #endregion
    }
}
