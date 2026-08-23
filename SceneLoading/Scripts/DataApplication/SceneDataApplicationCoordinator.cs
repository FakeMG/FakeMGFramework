using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FakeMG.SceneLoading
{
    public interface ILoadedSceneDataApplier
    {
        string DataApplierId { get; }
        UniTask ApplyLoadedDataAsync(CancellationToken cancellationToken);
    }

    public readonly struct SceneDataApplicationResult
    {
        public bool Succeeded { get; }
        public bool DidTimeOut { get; }
        public IReadOnlyList<string> FailedDataApplierIds { get; }
        public string FailureReason { get; }

        public SceneDataApplicationResult(
            bool succeeded,
            bool didTimeOut,
            IReadOnlyList<string> failedDataApplierIds,
            string failureReason)
        {
            Succeeded = succeeded;
            DidTimeOut = didTimeOut;
            FailedDataApplierIds = failedDataApplierIds ?? Array.Empty<string>();
            FailureReason = failureReason ?? string.Empty;
        }
    }

    public interface ISceneDataApplicationCoordinator
    {
        IDisposable Register(Scene scene, ILoadedSceneDataApplier dataApplier);
        UniTask<SceneDataApplicationResult> ApplyLoadedDataAsync(Scene scene, float timeoutSeconds, CancellationToken cancellationToken);
    }

    public sealed class SceneDataApplicationCoordinator : ISceneDataApplicationCoordinator, IDisposable
    {
        private readonly object _registrationLock = new();
        private readonly Dictionary<int, SceneRegistration> _registrationsBySceneHandle = new();
        private readonly Dictionary<int, UniTask<SceneDataApplicationResult>> _activeTasksBySceneHandle = new();

        public SceneDataApplicationCoordinator()
        {
            SceneManager.sceneUnloaded += RemoveUnloadedSceneRegistration;
        }

        #region Public Methods

        public IDisposable Register(Scene scene, ILoadedSceneDataApplier dataApplier)
        {
            if (!scene.IsValid())
            {
                throw new ArgumentException("A valid scene is required.", nameof(scene));
            }

            if (dataApplier == null || string.IsNullOrWhiteSpace(dataApplier.DataApplierId))
            {
                throw new ArgumentException("A scene data applier with a stable ID is required.", nameof(dataApplier));
            }

            lock (_registrationLock)
            {
                SceneRegistration registration = GetOrCreateRegistration(scene.handle);
                if (registration.IsApplicationRunning || registration.HasApplicationCompleted)
                {
                    throw new InvalidOperationException($"Scene '{scene.name}' no longer accepts data appliers because application has started or completed.");
                }

                if (!registration.DataAppliersById.TryAdd(dataApplier.DataApplierId, dataApplier))
                {
                    throw new InvalidOperationException($"Duplicate scene data applier ID '{dataApplier.DataApplierId}'.");
                }

                return new SceneDataApplierRegistration(
                    this,
                    scene.handle,
                    dataApplier.DataApplierId);
            }
        }

        public UniTask<SceneDataApplicationResult> ApplyLoadedDataAsync(Scene scene, float timeoutSeconds, CancellationToken cancellationToken)
        {
            if (!scene.IsValid())
            {
                return UniTask.FromResult(new SceneDataApplicationResult(
                    false,
                    false,
                    Array.Empty<string>(),
                    "Loaded-data application requires a valid scene."));
            }

            if (timeoutSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(timeoutSeconds));
            }

            lock (_registrationLock)
            {
                if (_activeTasksBySceneHandle.TryGetValue(scene.handle, out UniTask<SceneDataApplicationResult> activeTask))
                {
                    return activeTask.AttachExternalCancellation(cancellationToken);
                }

                SceneRegistration registration = GetOrCreateRegistration(scene.handle);
                if (registration.HasApplicationCompleted)
                {
                    return UniTask.FromResult(new SceneDataApplicationResult(
                        true,
                        false,
                        Array.Empty<string>(),
                        string.Empty));
                }

                registration.IsApplicationRunning = true;
                ILoadedSceneDataApplier[] dataAppliers = registration.DataAppliersById.Values.ToArray();
                UniTask<SceneDataApplicationResult> applicationTask = ApplyRegisteredDataAsync(scene, dataAppliers, timeoutSeconds).Preserve();
                _activeTasksBySceneHandle.Add(scene.handle, applicationTask);
                return applicationTask.AttachExternalCancellation(cancellationToken);
            }
        }

        public void Dispose()
        {
            SceneManager.sceneUnloaded -= RemoveUnloadedSceneRegistration;
            lock (_registrationLock)
            {
                _registrationsBySceneHandle.Clear();
                _activeTasksBySceneHandle.Clear();
            }
        }

        #endregion

        #region Private Methods

        private async UniTask<SceneDataApplicationResult> ApplyRegisteredDataAsync(
            Scene scene,
            IReadOnlyList<ILoadedSceneDataApplier> dataAppliers,
            float timeoutSeconds)
        {
            using var applicationCancellationSource = new CancellationTokenSource();
            try
            {
                if (dataAppliers.Count == 0)
                {
                    MarkApplicationCompleted(scene.handle);
                    return new SceneDataApplicationResult(
                        true,
                        false,
                        Array.Empty<string>(),
                        string.Empty);
                }

                var completedDataApplierIds = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);
                UniTask<DataApplierResult>[] applicationTasks = dataAppliers
                    .Select(dataApplier => ApplyDataAndRecordCompletionAsync(
                        dataApplier,
                        applicationCancellationSource.Token,
                        completedDataApplierIds))
                    .ToArray();
                UniTask<DataApplierResult[]> allApplicationsTask = UniTask.WhenAll(applicationTasks);
                UniTask timeoutTask = UniTask.Delay(
                    TimeSpan.FromSeconds(timeoutSeconds),
                    DelayType.UnscaledDeltaTime,
                    PlayerLoopTiming.Update,
                    applicationCancellationSource.Token);
                (bool didApplicationsComplete, DataApplierResult[] results) = await UniTask.WhenAny(allApplicationsTask, timeoutTask);
                if (!didApplicationsComplete)
                {
                    string[] timedOutIds = dataAppliers
                        .Where(dataApplier => !completedDataApplierIds.ContainsKey(dataApplier.DataApplierId))
                        .Select(dataApplier => dataApplier.DataApplierId)
                        .ToArray();
                    applicationCancellationSource.Cancel();
                    string timeoutReason = $"Loaded-data application for scene '{scene.name}' exceeded {timeoutSeconds} seconds.";
                    Echo.Error(timeoutReason);
                    return new SceneDataApplicationResult(false, true, timedOutIds, timeoutReason);
                }

                string[] failedIds = results
                    .Where(result => !result.Succeeded)
                    .Select(result => result.DataApplierId)
                    .ToArray();
                if (failedIds.Length == 0)
                {
                    MarkApplicationCompleted(scene.handle);
                    return new SceneDataApplicationResult(true, false, failedIds, string.Empty);
                }

                string failureReason = string.Join(Environment.NewLine, results.Where(result => !result.Succeeded).Select(result => result.FailureReason));
                Echo.Error(failureReason);
                return new SceneDataApplicationResult(false, false, failedIds, failureReason);
            }
            finally
            {
                lock (_registrationLock)
                {
                    _activeTasksBySceneHandle.Remove(scene.handle);
                    if (_registrationsBySceneHandle.TryGetValue(scene.handle, out SceneRegistration registration))
                    {
                        registration.IsApplicationRunning = false;
                    }
                }
            }
        }

        private static async UniTask<DataApplierResult> ApplyDataAsync(ILoadedSceneDataApplier dataApplier, CancellationToken cancellationToken)
        {
            try
            {
                await dataApplier.ApplyLoadedDataAsync(cancellationToken);
                return DataApplierResult.Success(dataApplier.DataApplierId);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return DataApplierResult.Failure(dataApplier.DataApplierId, $"Scene data applier '{dataApplier.DataApplierId}' was cancelled.");
            }
            catch (Exception exception)
            {
                return DataApplierResult.Failure(dataApplier.DataApplierId, $"Scene data applier '{dataApplier.DataApplierId}' failed: {exception}");
            }
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

        private SceneRegistration GetOrCreateRegistration(int sceneHandle)
        {
            if (!_registrationsBySceneHandle.TryGetValue(sceneHandle, out SceneRegistration registration))
            {
                registration = new SceneRegistration();
                _registrationsBySceneHandle.Add(sceneHandle, registration);
            }

            return registration;
        }

        private void MarkApplicationCompleted(int sceneHandle)
        {
            lock (_registrationLock)
            {
                if (_registrationsBySceneHandle.TryGetValue(sceneHandle, out SceneRegistration registration))
                {
                    registration.HasApplicationCompleted = true;
                }
            }
        }

        private void Unregister(int sceneHandle, string dataApplierId)
        {
            lock (_registrationLock)
            {
                if (!_registrationsBySceneHandle.TryGetValue(sceneHandle, out SceneRegistration registration) ||
                    !registration.DataAppliersById.Remove(dataApplierId))
                {
                    Echo.Warning($"Scene data applier '{dataApplierId}' was not registered for scene handle {sceneHandle}.");
                    return;
                }

                if (registration.DataAppliersById.Count == 0 &&
                    !registration.IsApplicationRunning &&
                    !registration.HasApplicationCompleted)
                {
                    _registrationsBySceneHandle.Remove(sceneHandle);
                }
            }
        }

        private void RemoveUnloadedSceneRegistration(Scene unloadedScene)
        {
            lock (_registrationLock)
            {
                _registrationsBySceneHandle.Remove(unloadedScene.handle);
                _activeTasksBySceneHandle.Remove(unloadedScene.handle);
            }
        }

        private sealed class SceneRegistration
        {
            public Dictionary<string, ILoadedSceneDataApplier> DataAppliersById { get; } = new(StringComparer.Ordinal);
            public bool IsApplicationRunning { get; set; }
            public bool HasApplicationCompleted { get; set; }
        }

        private sealed class SceneDataApplierRegistration : IDisposable
        {
            private SceneDataApplicationCoordinator _coordinator;
            private readonly int _sceneHandle;
            private readonly string _dataApplierId;

            public SceneDataApplierRegistration(
                SceneDataApplicationCoordinator coordinator,
                int sceneHandle,
                string dataApplierId)
            {
                _coordinator = coordinator;
                _sceneHandle = sceneHandle;
                _dataApplierId = dataApplierId;
            }

            public void Dispose()
            {
                SceneDataApplicationCoordinator coordinator = Interlocked.Exchange(ref _coordinator, null);
                coordinator?.Unregister(_sceneHandle, _dataApplierId);
            }
        }

        private readonly struct DataApplierResult
        {
            public string DataApplierId { get; }
            public bool Succeeded { get; }
            public string FailureReason { get; }

            private DataApplierResult(
                string dataApplierId,
                bool succeeded,
                string failureReason)
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
