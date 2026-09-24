using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using UnityEngine;
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
        Busy = 4
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

    public interface ISceneLoader
    {
        event Action<AssetReferenceScene> OnSceneLoaded;
        event Action<AssetReferenceScene> OnSceneUnloaded;
        event Action<AssetReferenceScene, string> OnSceneLoadFailed;
        event Action<AssetReferenceScene, string> OnSceneUnloadFailed;

        UniTask<SceneLoadResult> ReloadSceneAsync(AssetReferenceScene sceneReference, CancellationToken cancellationToken = default);
        UniTask<SceneLoadResult> LoadSceneAsync(
            AssetReferenceScene sceneReference,
            LoadSceneMode loadMode = LoadSceneMode.Additive,
            CancellationToken cancellationToken = default);
        UniTask<bool> UnloadSceneAsync(AssetReferenceScene sceneReference, CancellationToken cancellationToken = default);
        bool IsSceneLoaded(AssetReferenceScene sceneReference);
        bool SetActiveScene(AssetReferenceScene sceneReference);
    }

    public sealed class SceneLoader : MonoBehaviour, ISceneLoader
    {
        private ISceneDataApplicationCoordinator _dataApplicationCoordinator;
        private ISceneGateway _sceneGateway;

        public event Action<AssetReferenceScene> OnSceneLoaded;
        public event Action<AssetReferenceScene> OnSceneUnloaded;
        public event Action<AssetReferenceScene, string> OnSceneLoadFailed;
        public event Action<AssetReferenceScene, string> OnSceneUnloadFailed;

        #region Public Methods

        public async UniTask<SceneLoadResult> ReloadSceneAsync(
            AssetReferenceScene sceneReference,
            CancellationToken cancellationToken = default)
        {
            bool didUnload = await UnloadSceneAsync(sceneReference, cancellationToken);
            if (!didUnload)
            {
                return CreateFailure(
                    SceneLoadStatus.RawLoadFailed,
                    $"Scene '{sceneReference}' could not be unloaded for reload.");
            }

            return await LoadSceneAsync(sceneReference, LoadSceneMode.Additive, cancellationToken);
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

            try
            {
                if (_sceneGateway.TryGetLoadedScene(sceneReference, out Scene existingScene))
                {
                    return await CompleteSceneLoadAsync(
                        sceneReference,
                        existingScene,
                        cancellationToken);
                }

                SceneGatewayLoadResult rawLoadResult = await _sceneGateway.LoadAsync(
                    sceneReference,
                    loadMode,
                    cancellationToken);
                if (!rawLoadResult.Succeeded)
                {
                    SceneLoadStatus failureStatus = rawLoadResult.Status switch
                    {
                        SceneGatewayOperationStatus.Cancelled => SceneLoadStatus.Cancelled,
                        SceneGatewayOperationStatus.Busy => SceneLoadStatus.Busy,
                        SceneGatewayOperationStatus.Failed => SceneLoadStatus.RawLoadFailed,
                        SceneGatewayOperationStatus.NotLoaded => SceneLoadStatus.RawLoadFailed,
                        _ => SceneLoadStatus.RawLoadFailed
                    };

                    LogRawLoadFailure(failureStatus, rawLoadResult.FailureReason);
                    if (failureStatus != SceneLoadStatus.Cancelled)
                    {
                        OnSceneLoadFailed?.Invoke(sceneReference, rawLoadResult.FailureReason);
                    }

                    return CreateFailure(failureStatus, rawLoadResult.FailureReason);
                }

                return await CompleteSceneLoadAsync(
                    sceneReference,
                    rawLoadResult.Scene,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await CleanupFailedSceneLoadAsync(sceneReference);
                string failureReason = $"Loading scene '{sceneReference}' was cancelled.";
                Echo.Warning(failureReason);
                return CreateFailure(SceneLoadStatus.Cancelled, failureReason);
            }
            catch (Exception exception)
            {
                await CleanupFailedSceneLoadAsync(sceneReference);
                string failureReason = $"Loading scene '{sceneReference}' failed: {exception}";
                Echo.Error(failureReason);
                OnSceneLoadFailed?.Invoke(sceneReference, failureReason);
                return CreateFailure(SceneLoadStatus.RawLoadFailed, failureReason);
            }
        }

        public async UniTask<bool> UnloadSceneAsync(AssetReferenceScene sceneReference, CancellationToken cancellationToken = default)
        {
            if (sceneReference == null)
            {
                Echo.Warning("Cannot unload a scene without a scene reference.");
                return false;
            }

            SceneGatewayUnloadResult unloadResult = await _sceneGateway.UnloadAsync(sceneReference, cancellationToken);
            if (unloadResult.Succeeded)
            {
                OnSceneUnloaded?.Invoke(sceneReference);
                return true;
            }

            if (unloadResult.Status == SceneGatewayOperationStatus.Cancelled ||
                unloadResult.Status == SceneGatewayOperationStatus.NotLoaded ||
                unloadResult.Status == SceneGatewayOperationStatus.Busy)
            {
                Echo.Warning(unloadResult.FailureReason);
            }
            else
            {
                Echo.Error(unloadResult.FailureReason);
                OnSceneUnloadFailed?.Invoke(sceneReference, unloadResult.FailureReason);
            }

            return false;
        }

        public bool IsSceneLoaded(AssetReferenceScene sceneReference)
        {
            return _sceneGateway.IsSceneLoaded(sceneReference);
        }

        public bool SetActiveScene(AssetReferenceScene sceneReference)
        {
            if (_sceneGateway.TryGetLoadedScene(sceneReference, out Scene scene))
            {
                return SceneManager.SetActiveScene(scene);
            }

            Echo.Warning($"Cannot activate scene '{sceneReference}' because it is not loaded.");
            return false;
        }

        #endregion

        #region Private Methods

        [Inject]
        private void Construct(ISceneDataApplicationCoordinator dataApplicationCoordinator, ISceneGateway sceneGateway)
        {
            _dataApplicationCoordinator = dataApplicationCoordinator;
            _sceneGateway = sceneGateway;
        }

        private async UniTask<SceneLoadResult> CompleteSceneLoadAsync(
            AssetReferenceScene sceneReference,
            Scene scene,
            CancellationToken cancellationToken)
        {
            SceneDataApplicationResult applicationResult =
                await _dataApplicationCoordinator.ApplyLoadedDataAsync(UnitySceneContextFactory.Create(scene), cancellationToken);
            if (!applicationResult.Succeeded)
            {
                Echo.Error(applicationResult.FailureReason);
                OnSceneLoadFailed?.Invoke(sceneReference, applicationResult.FailureReason);
                await CleanupFailedSceneLoadAsync(sceneReference);
                return new SceneLoadResult(
                    SceneLoadStatus.DataApplicationFailed,
                    default,
                    applicationResult,
                    applicationResult.FailureReason);
            }

            OnSceneLoaded?.Invoke(sceneReference);
            return new SceneLoadResult(
                SceneLoadStatus.Ready,
                scene,
                applicationResult,
                string.Empty);
        }

        private async UniTask CleanupFailedSceneLoadAsync(AssetReferenceScene sceneReference)
        {
            if (sceneReference == null || !_sceneGateway.IsSceneLoaded(sceneReference))
            {
                return;
            }

            SceneGatewayUnloadResult cleanupResult = await _sceneGateway.UnloadAsync(sceneReference, CancellationToken.None);
            if (!cleanupResult.Succeeded && cleanupResult.Status != SceneGatewayOperationStatus.NotLoaded)
            {
                Echo.Error($"Failed to clean up scene '{sceneReference}' after load failure: {cleanupResult.FailureReason}");
            }
        }

        private static void LogRawLoadFailure(SceneLoadStatus failureStatus, string failureReason)
        {
            if (failureStatus == SceneLoadStatus.Cancelled || failureStatus == SceneLoadStatus.Busy)
            {
                Echo.Warning(failureReason);
                return;
            }

            Echo.Error(failureReason);
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

        #endregion
    }
}
