using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;

namespace FakeMG.SaveLoad
{
    public sealed class PersistenceStartupCoordinator : IStartupReadiness
    {
        private readonly IGlobalSaveInitializer _globalSaveInitializer;
        private readonly IWorldStartupContext _worldStartupContext;
        private readonly WorldSaveConfiguration _configuration;
        private readonly UniTaskCompletionSource<StartupReadinessResult> _completionSource = new();
        private bool _hasStarted;

        #region Public Methods

        public PersistenceStartupCoordinator(
            IGlobalSaveInitializer globalSaveInitializer,
            IWorldStartupContext worldStartupContext,
            WorldSaveConfiguration configuration)
        {
            _globalSaveInitializer = globalSaveInitializer;
            _worldStartupContext = worldStartupContext;
            _configuration = configuration;
        }

        public UniTask<StartupReadinessResult> WaitUntilReadyAsync(CancellationToken cancellationToken)
        {
            return _completionSource.Task.AttachExternalCancellation(cancellationToken);
        }

        public async UniTask<StartupReadinessResult> InitializeAsync(CancellationToken cancellationToken)
        {
            if (_hasStarted)
            {
                Echo.Warning("Persistence startup was requested more than once; waiting for the first initialization.");
                return await WaitUntilReadyAsync(cancellationToken);
            }

            _hasStarted = true;
            StartupReadinessResult result;
            try
            {
                result = await InitializeCoreAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                result = StartupReadinessResult.Cancelled("Persistence startup was cancelled.");
            }
            catch (Exception exception)
            {
                result = StartupReadinessResult.Failed($"Persistence startup failed: {exception}");
            }

            _completionSource.TrySetResult(result);
            return result;
        }

        #endregion

        #region Private Methods

        private async UniTask<StartupReadinessResult> InitializeCoreAsync(CancellationToken cancellationToken)
        {
            GlobalSaveInitializationResult globalResult = await _globalSaveInitializer.InitializeAsync(cancellationToken);
            if (!globalResult.Succeeded)
            {
                string failureReason = string.Join(Environment.NewLine, globalResult.FailureReasons);
                return StartupReadinessResult.Failed($"Global save initialization failed: {failureReason}");
            }

            StartupReadinessResult worldResult = await _configuration.StartupPolicySO.InitializeAsync(
                _worldStartupContext,
                _configuration.DefaultWorldDisplayName,
                cancellationToken);

            if (!worldResult.Succeeded)
            {
                return worldResult;
            }

            if (string.IsNullOrWhiteSpace(_worldStartupContext.ActiveWorldId))
            {
                return StartupReadinessResult.Failed(
                    "World startup completed without an active world. Gameplay was not loaded.");
            }

            return StartupReadinessResult.Ready();
        }

        #endregion
    }
}
