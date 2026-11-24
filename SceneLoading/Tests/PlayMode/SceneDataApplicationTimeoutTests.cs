using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FakeMG.SceneLoading.Tests.PlayMode
{
    /// <summary>The coordinator's timeout runs on the UniTask Update loop, so these cases need real player-loop frames.</summary>
    public sealed class SceneDataApplicationTimeoutTests
    {
        private const float SHORT_TIMEOUT_SECONDS = 0.05f;
        private const string BLOCKED_ID = "blocked";
        private const string COMPLETED_ID = "completed";
        private const string LATER_DEPENDENT_ID = "later-dependent";
        private const string NON_CANCELLABLE_ID = "non-cancellable";
        private static readonly Regex TIMEOUT_LOG = new("Loaded-data application for scene .* exceeded");

        #region Public Methods

        [UnityTest]
        public IEnumerator ApplyLoadedDataAsync_UnfinishedParticipantTimesOut_ReturnsOnlyUnfinishedId()
        {
            using SceneDataApplicationCoordinator coordinator = CreateCoordinator();
            LoadedSceneContext sceneContext = CreateSceneContext();
            var completedApplier = new CompletedTestDataApplier(COMPLETED_ID);
            var blockedApplier = new GatedTestDataApplier(BLOCKED_ID);
            using IDisposable completedRegistration = coordinator.Register(sceneContext, completedApplier);
            using IDisposable blockedRegistration = coordinator.Register(sceneContext, blockedApplier);
            SceneDataApplicationResult result = default;
            LogAssert.Expect(LogType.Error, TIMEOUT_LOG);

            yield return coordinator.ApplyLoadedDataAsync(sceneContext, CancellationToken.None)
                .ToCoroutine(completedResult => result = completedResult);

            Assert.That(result.DidTimeOut, Is.True);
            Assert.That(result.FailedDataApplierIds, Is.EquivalentTo(new[] { BLOCKED_ID }));
            blockedApplier.Release();
        }

        [UnityTest]
        public IEnumerator ApplyLoadedDataAsync_NonCancellableTimeout_DoesNotOverlapOrMarkCompleted()
        {
            using SceneDataApplicationCoordinator coordinator = CreateCoordinator();
            LoadedSceneContext sceneContext = CreateSceneContext();
            var applier = new NonCancellableTestDataApplier(NON_CANCELLABLE_ID);
            using IDisposable registration = coordinator.Register(sceneContext, applier);
            LogAssert.Expect(LogType.Error, TIMEOUT_LOG);

            SceneDataApplicationResult timedOutResult = default;
            yield return coordinator.ApplyLoadedDataAsync(sceneContext, CancellationToken.None)
                .ToCoroutine(completedResult => timedOutResult = completedResult);
            SceneDataApplicationResult concurrentResult = default;
            yield return coordinator.ApplyLoadedDataAsync(sceneContext, CancellationToken.None)
                .ToCoroutine(completedResult => concurrentResult = completedResult);

            Assert.That(timedOutResult.DidTimeOut, Is.True);
            Assert.That(concurrentResult.DidTimeOut, Is.True);
            Assert.That(applier.ApplicationCount, Is.EqualTo(1));

            applier.Complete();
            yield return null;
            SceneDataApplicationResult retryResult = default;
            yield return coordinator.ApplyLoadedDataAsync(sceneContext, CancellationToken.None)
                .ToCoroutine(completedResult => retryResult = completedResult);

            Assert.That(retryResult.Succeeded, Is.True);
            Assert.That(applier.ApplicationCount, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator ApplyLoadedDataAsync_Timeout_DoesNotStartLaterDependencyWave()
        {
            using SceneDataApplicationCoordinator coordinator = CreateCoordinator();
            LoadedSceneContext sceneContext = CreateSceneContext();
            var blockedApplier = new GatedTestDataApplier(BLOCKED_ID);
            var laterDependentApplier = new DependentTrackingTestDataApplier(LATER_DEPENDENT_ID, BLOCKED_ID);
            using IDisposable blockedRegistration = coordinator.Register(sceneContext, blockedApplier);
            using IDisposable laterDependentRegistration = coordinator.Register(sceneContext, laterDependentApplier);
            LogAssert.Expect(LogType.Error, TIMEOUT_LOG);

            SceneDataApplicationResult result = default;
            yield return coordinator.ApplyLoadedDataAsync(sceneContext, CancellationToken.None)
                .ToCoroutine(completedResult => result = completedResult);

            Assert.That(result.DidTimeOut, Is.True);
            Assert.That(result.FailedDataApplierIds, Is.EquivalentTo(new[] { BLOCKED_ID, LATER_DEPENDENT_ID }));
            Assert.That(laterDependentApplier.ApplicationCount, Is.EqualTo(0));
            Assert.That(blockedApplier.ApplicationToken.IsCancellationRequested, Is.True);
            blockedApplier.Release();
        }

        #endregion

        #region Private Methods

        private static SceneDataApplicationCoordinator CreateCoordinator()
        {
            return new SceneDataApplicationCoordinator(
                new SceneDataApplicationExecutionPlanBuilder(),
                new SceneDataApplicationConfiguration(SHORT_TIMEOUT_SECONDS));
        }

        private static LoadedSceneContext CreateSceneContext()
        {
            return new LoadedSceneContext(1, "test-scene");
        }

        #endregion

        private sealed class CompletedTestDataApplier : ILoadedSceneDataApplier
        {
            public CompletedTestDataApplier(string dataApplierId)
            {
                DataApplierId = dataApplierId;
            }

            public string DataApplierId { get; }

            public UniTask ApplyLoadedDataAsync(CancellationToken cancellationToken)
            {
                return UniTask.CompletedTask;
            }
        }

        private sealed class GatedTestDataApplier : ILoadedSceneDataApplier
        {
            private readonly UniTaskCompletionSource _releaseSource = new();

            public GatedTestDataApplier(string dataApplierId)
            {
                DataApplierId = dataApplierId;
            }

            public string DataApplierId { get; }
            public CancellationToken ApplicationToken { get; private set; }

            public async UniTask ApplyLoadedDataAsync(CancellationToken cancellationToken)
            {
                ApplicationToken = cancellationToken;
                await _releaseSource.Task.AttachExternalCancellation(cancellationToken);
            }

            public void Release()
            {
                _releaseSource.TrySetResult();
            }
        }

        private sealed class NonCancellableTestDataApplier : ILoadedSceneDataApplier
        {
            private readonly UniTaskCompletionSource _completionSource = new();

            public NonCancellableTestDataApplier(string dataApplierId)
            {
                DataApplierId = dataApplierId;
            }

            public string DataApplierId { get; }
            public int ApplicationCount { get; private set; }

            public async UniTask ApplyLoadedDataAsync(CancellationToken cancellationToken)
            {
                ApplicationCount++;
                await _completionSource.Task;
            }

            public void Complete()
            {
                _completionSource.TrySetResult();
            }
        }

        private sealed class DependentTrackingTestDataApplier : ILoadedSceneDataApplier, ILoadedSceneDataApplierDependencies
        {
            public DependentTrackingTestDataApplier(string dataApplierId, params string[] prerequisiteDataApplierIds)
            {
                DataApplierId = dataApplierId;
                PrerequisiteDataApplierIds = prerequisiteDataApplierIds;
            }

            public string DataApplierId { get; }
            public IReadOnlyCollection<string> PrerequisiteDataApplierIds { get; }
            public int ApplicationCount { get; private set; }

            public UniTask ApplyLoadedDataAsync(CancellationToken cancellationToken)
            {
                ApplicationCount++;
                return UniTask.CompletedTask;
            }
        }
    }
}
