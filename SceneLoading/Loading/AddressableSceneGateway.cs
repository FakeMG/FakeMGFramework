using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace FakeMG.SceneLoading
{
    internal interface ISceneGateway : IDisposable
    {
        UniTask<SceneGatewayLoadResult> LoadAsync(
            AssetReferenceScene sceneReference,
            LoadSceneMode loadMode,
            CancellationToken cancellationToken);
        UniTask<SceneGatewayUnloadResult> UnloadAsync(AssetReferenceScene sceneReference, CancellationToken cancellationToken);
        bool IsSceneLoaded(AssetReferenceScene sceneReference);
        bool TryGetLoadedScene(AssetReferenceScene sceneReference, out Scene scene);
    }

    internal enum SceneGatewayOperationStatus
    {
        Succeeded = 0,
        Failed = 1,
        Cancelled = 2,
        Busy = 3,
        NotLoaded = 4
    }

    internal readonly struct SceneGatewayLoadResult
    {
        public SceneGatewayOperationStatus Status { get; }
        public Scene Scene { get; }
        public string FailureReason { get; }
        public bool Succeeded => Status == SceneGatewayOperationStatus.Succeeded;

        public SceneGatewayLoadResult(
            SceneGatewayOperationStatus status,
            Scene scene,
            string failureReason)
        {
            Status = status;
            Scene = scene;
            FailureReason = failureReason ?? string.Empty;
        }
    }

    internal readonly struct SceneGatewayUnloadResult
    {
        public SceneGatewayOperationStatus Status { get; }
        public string FailureReason { get; }
        public bool Succeeded => Status == SceneGatewayOperationStatus.Succeeded;

        public SceneGatewayUnloadResult(SceneGatewayOperationStatus status, string failureReason)
        {
            Status = status;
            FailureReason = failureReason ?? string.Empty;
        }
    }

    internal sealed class AddressableSceneGateway : ISceneGateway
    {
        private readonly Dictionary<string, SceneHandleState> _statesByAssetGuid = new();
        private bool _isDisposed;

        #region Public Methods

        public async UniTask<SceneGatewayLoadResult> LoadAsync(
            AssetReferenceScene sceneReference,
            LoadSceneMode loadMode,
            CancellationToken cancellationToken)
        {
            if (sceneReference == null)
            {
                return CreateLoadFailure(SceneGatewayOperationStatus.Failed, "Scene reference is required.");
            }

            if (_isDisposed)
            {
                return CreateLoadFailure(SceneGatewayOperationStatus.Failed, "Scene gateway has been disposed.");
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return CreateLoadFailure(
                    SceneGatewayOperationStatus.Cancelled,
                    $"Loading scene '{sceneReference}' was cancelled before it started.");
            }

            SceneHandleState state = GetOrCreateState(sceneReference);
            if (state.IsBusy)
            {
                return CreateLoadFailure(SceneGatewayOperationStatus.Busy, $"Scene '{sceneReference}' is busy.");
            }

            if (TryGetLoadedScene(state, out Scene existingScene))
            {
                return new SceneGatewayLoadResult(
                    SceneGatewayOperationStatus.Succeeded,
                    existingScene,
                    string.Empty);
            }

            state.IsLoading = true;
            AsyncOperationHandle<SceneInstance> loadHandle = Addressables.LoadSceneAsync(sceneReference, loadMode);
            state.PendingLoadHandle = loadHandle;
            try
            {
                await loadHandle.ToUniTask();
                state.PendingLoadHandle = null;
                if (loadHandle.Status != AsyncOperationStatus.Succeeded || !loadHandle.Result.Scene.IsValid())
                {
                    string failureReason = loadHandle.OperationException?.ToString() ??
                                           $"Addressables did not return a valid scene for '{sceneReference}'.";
                    ReleaseFailedLoadHandle(loadHandle);
                    _statesByAssetGuid.Remove(sceneReference.AssetGUID);
                    return CreateLoadFailure(SceneGatewayOperationStatus.Failed, failureReason);
                }

                if (_isDisposed || cancellationToken.IsCancellationRequested)
                {
                    await UnloadOwnedHandleAsync(loadHandle);
                    _statesByAssetGuid.Remove(sceneReference.AssetGUID);
                    return CreateLoadFailure(SceneGatewayOperationStatus.Cancelled, $"Loading scene '{sceneReference}' was cancelled.");
                }

                state.LoadedSceneHandle = loadHandle;
                return new SceneGatewayLoadResult(
                    SceneGatewayOperationStatus.Succeeded,
                    loadHandle.Result.Scene,
                    string.Empty);
            }
            catch (Exception exception)
            {
                state.PendingLoadHandle = null;
                if (loadHandle.IsValid())
                {
                    if (loadHandle.Status == AsyncOperationStatus.Succeeded && loadHandle.Result.Scene.IsValid())
                    {
                        await UnloadOwnedHandleAsync(loadHandle);
                    }
                    else
                    {
                        Addressables.Release(loadHandle);
                    }
                }

                _statesByAssetGuid.Remove(sceneReference.AssetGUID);
                return CreateLoadFailure(SceneGatewayOperationStatus.Failed, $"Loading scene '{sceneReference}' failed: {exception}");
            }
            finally
            {
                state.IsLoading = false;
            }
        }

        public async UniTask<SceneGatewayUnloadResult> UnloadAsync(
            AssetReferenceScene sceneReference,
            CancellationToken cancellationToken)
        {
            if (sceneReference == null)
            {
                return new SceneGatewayUnloadResult(SceneGatewayOperationStatus.NotLoaded, "Scene reference is required.");
            }

            if (!_statesByAssetGuid.TryGetValue(sceneReference.AssetGUID, out SceneHandleState state) ||
                !state.LoadedSceneHandle.HasValue)
            {
                return new SceneGatewayUnloadResult(SceneGatewayOperationStatus.NotLoaded, $"Scene '{sceneReference}' is not loaded.");
            }

            if (state.IsBusy)
            {
                return new SceneGatewayUnloadResult(SceneGatewayOperationStatus.Busy, $"Scene '{sceneReference}' is busy.");
            }

            state.IsUnloading = true;
            AsyncOperationHandle<SceneInstance> loadedHandle = state.LoadedSceneHandle.Value;
            state.LoadedSceneHandle = null;
            try
            {
                await UnloadOwnedHandleAsync(loadedHandle);
                _statesByAssetGuid.Remove(sceneReference.AssetGUID);
                if (cancellationToken.IsCancellationRequested)
                {
                    return new SceneGatewayUnloadResult(
                        SceneGatewayOperationStatus.Cancelled,
                        $"Waiting for scene '{sceneReference}' to unload was cancelled after the unload completed.");
                }

                return new SceneGatewayUnloadResult(SceneGatewayOperationStatus.Succeeded, string.Empty);
            }
            catch (Exception exception)
            {
                _statesByAssetGuid.Remove(sceneReference.AssetGUID);
                return new SceneGatewayUnloadResult(
                    SceneGatewayOperationStatus.Failed,
                    $"Unloading scene '{sceneReference}' failed: {exception}");
            }
            finally
            {
                state.IsUnloading = false;
            }
        }

        public bool IsSceneLoaded(AssetReferenceScene sceneReference)
        {
            return sceneReference != null &&
                   _statesByAssetGuid.TryGetValue(sceneReference.AssetGUID, out SceneHandleState state) &&
                   IsLoadedHandleValid(state.LoadedSceneHandle);
        }

        public bool TryGetLoadedScene(AssetReferenceScene sceneReference, out Scene scene)
        {
            if (sceneReference != null &&
                _statesByAssetGuid.TryGetValue(sceneReference.AssetGUID, out SceneHandleState state) &&
                TryGetLoadedScene(state, out scene))
            {
                return true;
            }

            scene = default;
            return false;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            foreach (SceneHandleState state in _statesByAssetGuid.Values)
            {
                if (state.LoadedSceneHandle.HasValue)
                {
                    AsyncOperationHandle<SceneInstance> loadedHandle = state.LoadedSceneHandle.Value;
                    state.LoadedSceneHandle = null;
                    if (loadedHandle.IsValid())
                    {
                        if (Application.isPlaying)
                        {
                            Addressables.UnloadSceneAsync(loadedHandle, true);
                        }
                        else
                        {
                            Addressables.Release(loadedHandle);
                        }
                    }
                }
            }
        }

        #endregion

        #region Private Methods

        private SceneHandleState GetOrCreateState(AssetReferenceScene sceneReference)
        {
            if (!_statesByAssetGuid.TryGetValue(sceneReference.AssetGUID, out SceneHandleState state))
            {
                state = new SceneHandleState();
                _statesByAssetGuid.Add(sceneReference.AssetGUID, state);
            }

            return state;
        }

        private static bool TryGetLoadedScene(SceneHandleState state, out Scene scene)
        {
            if (IsLoadedHandleValid(state.LoadedSceneHandle))
            {
                scene = state.LoadedSceneHandle.Value.Result.Scene;
                return true;
            }

            scene = default;
            return false;
        }

        private static bool IsLoadedHandleValid(AsyncOperationHandle<SceneInstance>? handle)
        {
            return handle.HasValue &&
                   handle.Value.IsValid() &&
                   handle.Value.Status == AsyncOperationStatus.Succeeded &&
                   handle.Value.Result.Scene.IsValid();
        }

        private static async UniTask UnloadOwnedHandleAsync(
            AsyncOperationHandle<SceneInstance> loadedHandle)
        {
            AsyncOperationHandle<SceneInstance> unloadHandle = Addressables.UnloadSceneAsync(loadedHandle, true);
            await unloadHandle.ToUniTask();
        }

        private static void ReleaseFailedLoadHandle(AsyncOperationHandle<SceneInstance> loadHandle)
        {
            if (loadHandle.IsValid())
            {
                Addressables.Release(loadHandle);
            }
        }

        private static SceneGatewayLoadResult CreateLoadFailure(
            SceneGatewayOperationStatus status,
            string failureReason)
        {
            return new SceneGatewayLoadResult(status, default, failureReason);
        }

        private sealed class SceneHandleState
        {
            public AsyncOperationHandle<SceneInstance>? LoadedSceneHandle { get; set; }
            public AsyncOperationHandle<SceneInstance>? PendingLoadHandle { get; set; }
            public bool IsLoading { get; set; }
            public bool IsUnloading { get; set; }
            public bool IsBusy => IsLoading || IsUnloading;
        }

        #endregion
    }
}
