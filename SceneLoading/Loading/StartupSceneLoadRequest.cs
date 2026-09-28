using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;

namespace FakeMG.SceneLoading
{
    public sealed class StartupSceneLoadRequest
    {
        private readonly IStartupReadiness _readiness;
        private readonly ISceneLoader _sceneLoader;

        #region Public Methods

        public StartupSceneLoadRequest(IStartupReadiness readiness, ISceneLoader sceneLoader)
        {
            _readiness = readiness;
            _sceneLoader = sceneLoader;
        }

        public async UniTask<SceneLoadResult> LoadAsync(
            AssetReferenceScene sceneToLoad,
            CancellationToken cancellationToken)
        {
            try
            {
                StartupReadinessResult readinessResult = await _readiness.WaitUntilReadyAsync(cancellationToken);
                if (!readinessResult.Succeeded)
                {
                    SceneLoadStatus status = readinessResult.Status == StartupReadinessStatus.Cancelled
                        ? SceneLoadStatus.Cancelled
                        : SceneLoadStatus.StartupFailed;

                    if (status == SceneLoadStatus.Cancelled)
                    {
                        Echo.Log($"Gameplay scene load skipped: {readinessResult.FailureReason}");
                    }
                    else
                    {
                        Echo.Error($"Gameplay scene load skipped: {readinessResult.FailureReason}");
                    }

                    return new SceneLoadResult(status, default, default, readinessResult.FailureReason);
                }

                return await _sceneLoader.LoadSceneAsync(sceneToLoad, cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                const string FAILURE_REASON = "Gameplay scene load was cancelled while waiting for persistence startup.";
                Echo.Log(FAILURE_REASON);
                return new SceneLoadResult(SceneLoadStatus.Cancelled, default, default, FAILURE_REASON);
            }
        }

        #endregion
    }
}
