using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;

namespace FakeMG.SceneLoading
{
    internal interface ISceneDataApplicationSceneLifecycle
    {
        void RemoveScene(LoadedSceneContext sceneContext);
    }

    public sealed class SceneDataApplicationCoordinator :
        ISceneDataApplicationCoordinator,
        ISceneDataApplicationSceneLifecycle,
        IDisposable
    {
        private readonly ISceneDataApplicationExecutionPlanBuilder _executionPlanBuilder;
        private readonly SceneDataApplicationConfiguration _configuration;
        private readonly object _stateLock = new();
        private readonly Dictionary<int, SceneApplicationState> _statesBySceneId = new();
        private bool _isDisposed;

        public SceneDataApplicationCoordinator(
            ISceneDataApplicationExecutionPlanBuilder executionPlanBuilder,
            SceneDataApplicationConfiguration configuration)
        {
            _executionPlanBuilder = executionPlanBuilder ?? throw new ArgumentNullException(nameof(executionPlanBuilder));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        #region Public Methods

        public IDisposable Register(LoadedSceneContext sceneContext, ILoadedSceneDataApplier dataApplier)
        {
            if (!sceneContext.IsValid)
            {
                throw new ArgumentException("A valid loaded-scene context is required.", nameof(sceneContext));
            }

            if (dataApplier == null || string.IsNullOrWhiteSpace(dataApplier.DataApplierId))
            {
                throw new ArgumentException(
                    "A scene data applier with a stable ID is required.",
                    nameof(dataApplier));
            }

            lock (_stateLock)
            {
                ThrowIfDisposed();
                SceneApplicationState state = GetOrCreateState(sceneContext);
                if (state.IsApplicationRunning || state.HasApplicationCompleted)
                {
                    throw new InvalidOperationException(
                        $"Scene '{sceneContext.Name}' no longer accepts data appliers because application has started or completed.");
                }

                if (!state.DataAppliersById.TryAdd(dataApplier.DataApplierId, dataApplier))
                {
                    throw new InvalidOperationException(
                        $"Duplicate scene data applier ID '{dataApplier.DataApplierId}'.");
                }

                return new SceneDataApplierRegistration(this, sceneContext.Id, dataApplier.DataApplierId);
            }
        }

        public UniTask<SceneDataApplicationResult> ApplyLoadedDataAsync(
            LoadedSceneContext sceneContext,
            CancellationToken cancellationToken)
        {
            if (!sceneContext.IsValid)
            {
                const string FAILURE_REASON = "Loaded-data application requires a valid scene context.";
                Echo.Error(FAILURE_REASON);
                return UniTask.FromResult(CreateFailure(FAILURE_REASON));
            }

            UniTask<SceneDataApplicationResult> applicationTask;
            lock (_stateLock)
            {
                if (_isDisposed)
                {
                    const string FAILURE_REASON = "Loaded-data application coordinator has been disposed.";
                    Echo.Error(FAILURE_REASON);
                    return UniTask.FromResult(CreateFailure(FAILURE_REASON));
                }

                SceneApplicationState state = GetOrCreateState(sceneContext);
                if (state.HasApplicationCompleted)
                {
                    return UniTask.FromResult(CreateSuccess());
                }

                if (state.IsApplicationRunning)
                {
                    return state.ApplicationCompletionSource.Task.AttachExternalCancellation(cancellationToken);
                }

                state.BeginApplication();
                applicationTask = state.ApplicationCompletionSource.Task.Preserve();
                RunApplicationAndPublishResultAsync(state).Forget();
            }

            return applicationTask.AttachExternalCancellation(cancellationToken);
        }

        public void Dispose()
        {
            List<CancellationTokenSource> cancellationSources = new();
            lock (_stateLock)
            {
                if (_isDisposed)
                {
                    return;
                }

                _isDisposed = true;
                foreach (SceneApplicationState state in _statesBySceneId.Values)
                {
                    if (state.ApplicationCancellationSource != null)
                    {
                        cancellationSources.Add(state.ApplicationCancellationSource);
                    }
                }

                _statesBySceneId.Clear();
            }

            foreach (CancellationTokenSource cancellationSource in cancellationSources)
            {
                cancellationSource.Cancel();
            }
        }

        void ISceneDataApplicationSceneLifecycle.RemoveScene(LoadedSceneContext sceneContext)
        {
            CancellationTokenSource cancellationSource = null;
            lock (_stateLock)
            {
                if (_statesBySceneId.Remove(sceneContext.Id, out SceneApplicationState state))
                {
                    cancellationSource = state.ApplicationCancellationSource;
                }
            }

            cancellationSource?.Cancel();
        }

        #endregion

        #region Private Methods

        private async UniTaskVoid RunApplicationAndPublishResultAsync(SceneApplicationState state)
        {
            CancellationToken applicationCancellationToken = state.ApplicationCancellationSource.Token;
            Task<SceneDataApplicationResult> executionTask = null;
            bool hasPublishedResult = false;
            try
            {
                ILoadedSceneDataApplier[] dataAppliers = state.DataAppliersById.Values.ToArray();
                SceneDataApplicationExecutionPlan executionPlan = _executionPlanBuilder.Build(dataAppliers);
                if (!executionPlan.Succeeded)
                {
                    Echo.Error(executionPlan.FailureReason);
                    state.ApplicationCompletionSource.TrySetResult(new SceneDataApplicationResult(
                        false,
                        false,
                        executionPlan.FailedDataApplierIds,
                        executionPlan.FailureReason));
                    hasPublishedResult = true;
                    return;
                }

                ConcurrentDictionary<string, byte> completedDataApplierIds = new(StringComparer.Ordinal);
                executionTask = ApplyExecutionWavesAsync(
                    executionPlan,
                    applicationCancellationToken,
                    completedDataApplierIds).AsTask();
                UniTask timeoutTask = UniTask.Delay(
                    TimeSpan.FromSeconds(_configuration.TimeoutSeconds),
                    DelayType.UnscaledDeltaTime,
                    PlayerLoopTiming.Update,
                    applicationCancellationToken);
                (bool didApplicationComplete, SceneDataApplicationResult applicationResult) =
                    await UniTask.WhenAny(executionTask.AsUniTask(), timeoutTask);

                applicationCancellationToken.ThrowIfCancellationRequested();
                if (!didApplicationComplete)
                {
                    string[] timedOutIds = dataAppliers
                        .Where(dataApplier => !completedDataApplierIds.ContainsKey(dataApplier.DataApplierId))
                        .Select(dataApplier => dataApplier.DataApplierId)
                        .ToArray();
                    string timeoutReason =
                        $"Loaded-data application for scene '{state.SceneContext.Name}' exceeded {_configuration.TimeoutSeconds} seconds.";
                    Echo.Error(timeoutReason);
                    state.ApplicationCompletionSource.TrySetResult(new SceneDataApplicationResult(
                        false,
                        true,
                        timedOutIds,
                        timeoutReason));
                    hasPublishedResult = true;
                    state.ApplicationCancellationSource.Cancel();
                    await ObserveCancelledExecutionAsync(executionTask, state.SceneContext);
                    return;
                }

                if (applicationResult.Succeeded)
                {
                    state.HasApplicationCompleted = true;
                }
                else
                {
                    Echo.Error(applicationResult.FailureReason);
                }

                state.ApplicationCompletionSource.TrySetResult(applicationResult);
                hasPublishedResult = true;
            }
            catch (OperationCanceledException) when (applicationCancellationToken.IsCancellationRequested)
            {
                Echo.Log($"Loaded-data application for scene '{state.SceneContext.Name}' was cancelled during teardown.");
                state.ApplicationCompletionSource.TrySetCanceled(applicationCancellationToken);
                hasPublishedResult = true;
                if (executionTask != null)
                {
                    await ObserveCancelledExecutionAsync(executionTask, state.SceneContext);
                }
            }
            catch (Exception exception)
            {
                string failureReason = $"Loaded-data application for scene '{state.SceneContext.Name}' failed unexpectedly: {exception}";
                Echo.Error(failureReason);
                state.ApplicationCompletionSource.TrySetResult(CreateFailure(failureReason));
                hasPublishedResult = true;
            }
            finally
            {
                if (!hasPublishedResult)
                {
                    state.ApplicationCompletionSource.TrySetResult(CreateFailure(
                        $"Loaded-data application for scene '{state.SceneContext.Name}' ended without a result."));
                }

                EndApplication(state);
            }
        }

        private static async UniTask<SceneDataApplicationResult> ApplyExecutionWavesAsync(
            SceneDataApplicationExecutionPlan executionPlan,
            CancellationToken cancellationToken,
            ConcurrentDictionary<string, byte> completedDataApplierIds)
        {
            foreach (IReadOnlyList<ILoadedSceneDataApplier> executionWave in executionPlan.ExecutionWaves)
            {
                cancellationToken.ThrowIfCancellationRequested();
                UniTask<DataApplierResult>[] applicationTasks = executionWave
                    .Select(dataApplier => ApplyDataAndRecordCompletionAsync(
                        dataApplier,
                        cancellationToken,
                        completedDataApplierIds))
                    .ToArray();
                DataApplierResult[] results = await UniTask.WhenAll(applicationTasks);
                string[] failedIds = results
                    .Where(result => !result.Succeeded)
                    .Select(result => result.DataApplierId)
                    .ToArray();
                if (failedIds.Length > 0)
                {
                    string failureReason = string.Join(
                        Environment.NewLine,
                        results.Where(result => !result.Succeeded).Select(result => result.FailureReason));
                    return new SceneDataApplicationResult(false, false, failedIds, failureReason);
                }
            }

            return CreateSuccess();
        }

        private static async UniTask<DataApplierResult> ApplyDataAndRecordCompletionAsync(
            ILoadedSceneDataApplier dataApplier,
            CancellationToken cancellationToken,
            ConcurrentDictionary<string, byte> completedDataApplierIds)
        {
            DataApplierResult result = await ApplyDataAsync(dataApplier, cancellationToken);
            completedDataApplierIds.TryAdd(dataApplier.DataApplierId, 0);
            return result;
        }

        private static async UniTask<DataApplierResult> ApplyDataAsync(
            ILoadedSceneDataApplier dataApplier,
            CancellationToken cancellationToken)
        {
            try
            {
                await dataApplier.ApplyLoadedDataAsync(cancellationToken);
                return DataApplierResult.Success(dataApplier.DataApplierId);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return DataApplierResult.Failure(
                    dataApplier.DataApplierId,
                    $"Scene data applier '{dataApplier.DataApplierId}' was cancelled.");
            }
            catch (Exception exception)
            {
                return DataApplierResult.Failure(
                    dataApplier.DataApplierId,
                    $"Scene data applier '{dataApplier.DataApplierId}' failed: {exception}");
            }
        }

        private static async UniTask ObserveCancelledExecutionAsync(
            Task<SceneDataApplicationResult> executionTask,
            LoadedSceneContext sceneContext)
        {
            try
            {
                await executionTask;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Echo.Error($"Cancelled loaded-data execution for scene '{sceneContext.Name}' failed while settling: {exception}");
            }
        }

        private SceneApplicationState GetOrCreateState(LoadedSceneContext sceneContext)
        {
            if (!_statesBySceneId.TryGetValue(sceneContext.Id, out SceneApplicationState state))
            {
                state = new SceneApplicationState(sceneContext);
                _statesBySceneId.Add(sceneContext.Id, state);
            }

            return state;
        }

        private void EndApplication(SceneApplicationState state)
        {
            CancellationTokenSource cancellationSource;
            lock (_stateLock)
            {
                cancellationSource = state.EndApplication();
                if (_statesBySceneId.TryGetValue(state.SceneContext.Id, out SceneApplicationState currentState) &&
                    ReferenceEquals(currentState, state) &&
                    state.DataAppliersById.Count == 0 &&
                    !state.HasApplicationCompleted)
                {
                    _statesBySceneId.Remove(state.SceneContext.Id);
                }
            }

            cancellationSource.Cancel();
            cancellationSource.Dispose();
        }

        private void Unregister(int sceneId, string dataApplierId)
        {
            CancellationTokenSource cancellationSource = null;
            lock (_stateLock)
            {
                if (!_statesBySceneId.TryGetValue(sceneId, out SceneApplicationState state))
                {
                    return;
                }

                if (!state.DataAppliersById.Remove(dataApplierId))
                {
                    Echo.Warning($"Scene data applier '{dataApplierId}' was not registered for scene id {sceneId}.");
                    return;
                }

                if (state.IsApplicationRunning)
                {
                    cancellationSource = state.ApplicationCancellationSource;
                }
                else if (state.DataAppliersById.Count == 0 && !state.HasApplicationCompleted)
                {
                    _statesBySceneId.Remove(sceneId);
                }
            }

            cancellationSource?.Cancel();
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(SceneDataApplicationCoordinator));
            }
        }

        private static SceneDataApplicationResult CreateSuccess()
        {
            return new SceneDataApplicationResult(true, false, Array.Empty<string>(), string.Empty);
        }

        private static SceneDataApplicationResult CreateFailure(string failureReason)
        {
            return new SceneDataApplicationResult(
                false,
                false,
                Array.Empty<string>(),
                failureReason);
        }

        private sealed class SceneApplicationState
        {
            public LoadedSceneContext SceneContext { get; }
            public Dictionary<string, ILoadedSceneDataApplier> DataAppliersById { get; } = new(StringComparer.Ordinal);
            public UniTaskCompletionSource<SceneDataApplicationResult> ApplicationCompletionSource { get; private set; }
            public CancellationTokenSource ApplicationCancellationSource { get; private set; }
            public bool HasApplicationCompleted { get; set; }
            public bool IsApplicationRunning => ApplicationCompletionSource != null;

            public SceneApplicationState(LoadedSceneContext sceneContext)
            {
                SceneContext = sceneContext;
            }

            public void BeginApplication()
            {
                ApplicationCancellationSource = new CancellationTokenSource();
                ApplicationCompletionSource = new UniTaskCompletionSource<SceneDataApplicationResult>();
            }

            public CancellationTokenSource EndApplication()
            {
                CancellationTokenSource cancellationSource = ApplicationCancellationSource;
                ApplicationCancellationSource = null;
                ApplicationCompletionSource = null;
                return cancellationSource;
            }
        }

        private sealed class SceneDataApplierRegistration : IDisposable
        {
            private SceneDataApplicationCoordinator _coordinator;
            private readonly int _sceneId;
            private readonly string _dataApplierId;

            public SceneDataApplierRegistration(
                SceneDataApplicationCoordinator coordinator,
                int sceneId,
                string dataApplierId)
            {
                _coordinator = coordinator;
                _sceneId = sceneId;
                _dataApplierId = dataApplierId;
            }

            public void Dispose()
            {
                SceneDataApplicationCoordinator coordinator = Interlocked.Exchange(ref _coordinator, null);
                coordinator?.Unregister(_sceneId, _dataApplierId);
            }
        }

        private readonly struct DataApplierResult
        {
            public string DataApplierId { get; }
            public bool Succeeded { get; }
            public string FailureReason { get; }

            private DataApplierResult(string dataApplierId, bool succeeded, string failureReason)
            {
                DataApplierId = dataApplierId;
                Succeeded = succeeded;
                FailureReason = failureReason;
            }

            public static DataApplierResult Success(string dataApplierId)
            {
                return new DataApplierResult(dataApplierId, true, string.Empty);
            }

            public static DataApplierResult Failure(string dataApplierId, string failureReason)
            {
                return new DataApplierResult(dataApplierId, false, failureReason);
            }
        }

        #endregion
    }
}
