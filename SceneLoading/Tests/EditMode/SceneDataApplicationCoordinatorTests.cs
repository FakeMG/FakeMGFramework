using System;
using System.Collections;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FakeMG.SceneLoading.Tests.EditMode
{
    public sealed class SceneDataApplicationCoordinatorTests
    {
        [UnityTest]
        public IEnumerator ApplyLoadedDataAsync_NoParticipants_Succeeds()
        {
            using SceneDataApplicationCoordinator coordinator = CreateCoordinator(1f);
            SceneDataApplicationResult result = default;

            yield return coordinator.ApplyLoadedDataAsync(
                    CreateSceneContext(),
                    CancellationToken.None)
                .ToCoroutine(completedResult => result = completedResult);

            Assert.That(result.Succeeded, Is.True);
        }

        [UnityTest]
        public IEnumerator ApplyLoadedDataAsync_ParticipantFailure_ReturnsParticipantId()
        {
            using SceneDataApplicationCoordinator coordinator = CreateCoordinator(1f);
            LoadedSceneContext sceneContext = CreateSceneContext();
            var applier = new TestSceneDataApplier("failing", true);
            using IDisposable registration = coordinator.Register(sceneContext, applier);
            SceneDataApplicationResult result = default;
            LogAssert.Expect(
                LogType.Error,
                new Regex("Scene data applier 'failing' failed"));

            yield return coordinator.ApplyLoadedDataAsync(sceneContext, CancellationToken.None)
                .ToCoroutine(completedResult => result = completedResult);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FailedDataApplierIds, Is.EquivalentTo(new[] { "failing" }));
        }

        [UnityTest]
        public IEnumerator ApplyLoadedDataAsync_SameSceneConcurrentCalls_AppliesParticipantOnce()
        {
            using SceneDataApplicationCoordinator coordinator = CreateCoordinator(2f);
            LoadedSceneContext sceneContext = CreateSceneContext();
            var applier = new TestSceneDataApplier("shared", false, true);
            using IDisposable registration = coordinator.Register(sceneContext, applier);
            UniTask<SceneDataApplicationResult> firstTask = coordinator
                .ApplyLoadedDataAsync(sceneContext, CancellationToken.None)
                .Preserve();
            UniTask<SceneDataApplicationResult> secondTask = coordinator
                .ApplyLoadedDataAsync(sceneContext, CancellationToken.None)
                .Preserve();

            Assert.Throws<InvalidOperationException>(() =>
                coordinator.Register(sceneContext, new TestSceneDataApplier("late", false)));
            applier.Complete();
            SceneDataApplicationResult[] results = null;
            yield return UniTask.WhenAll(new[] { firstTask, secondTask })
                .ToCoroutine(completedResults => results = completedResults);

            Assert.That(results[0].Succeeded, Is.True);
            Assert.That(results[1].Succeeded, Is.True);
            Assert.That(applier.ApplicationCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ApplyLoadedDataAsync_UnfinishedParticipantTimesOut_ReturnsOnlyUnfinishedId()
        {
            using SceneDataApplicationCoordinator coordinator = CreateCoordinator(0.05f);
            LoadedSceneContext sceneContext = CreateSceneContext();
            var completedApplier = new TestSceneDataApplier("completed", false);
            var blockedApplier = new TestSceneDataApplier("blocked", false, true);
            using IDisposable completedRegistration = coordinator.Register(sceneContext, completedApplier);
            using IDisposable blockedRegistration = coordinator.Register(sceneContext, blockedApplier);
            SceneDataApplicationResult result = default;
            LogAssert.Expect(
                LogType.Error,
                new Regex("Loaded-data application for scene .* exceeded"));

            yield return coordinator.ApplyLoadedDataAsync(sceneContext, CancellationToken.None)
                .ToCoroutine(completedResult => result = completedResult);

            Assert.That(result.DidTimeOut, Is.True);
            Assert.That(result.FailedDataApplierIds, Is.EquivalentTo(new[] { "blocked" }));
        }

        [UnityTest]
        public IEnumerator ApplyLoadedDataAsync_NonCancellableTimeout_DoesNotOverlapOrMarkCompleted()
        {
            using SceneDataApplicationCoordinator coordinator = CreateCoordinator(0.05f);
            LoadedSceneContext sceneContext = CreateSceneContext();
            var applier = new NonCancellableTestSceneDataApplier("non-cancellable");
            using IDisposable registration = coordinator.Register(sceneContext, applier);
            LogAssert.Expect(
                LogType.Error,
                new Regex("Loaded-data application for scene .* exceeded"));

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
        public IEnumerator ApplyLoadedDataAsync_AfterSuccessfulApplication_DoesNotApplyParticipantAgain()
        {
            using SceneDataApplicationCoordinator coordinator = CreateCoordinator(1f);
            LoadedSceneContext sceneContext = CreateSceneContext();
            var applier = new TestSceneDataApplier("single-application", false);
            using IDisposable registration = coordinator.Register(sceneContext, applier);

            yield return coordinator.ApplyLoadedDataAsync(sceneContext, CancellationToken.None).ToCoroutine();
            yield return coordinator.ApplyLoadedDataAsync(sceneContext, CancellationToken.None).ToCoroutine();

            Assert.That(applier.ApplicationCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ApplyLoadedDataAsync_ParticipantUnregisters_CancelsPendingApplication()
        {
            using SceneDataApplicationCoordinator coordinator = CreateCoordinator(2f);
            LoadedSceneContext sceneContext = CreateSceneContext();
            var applier = new TestSceneDataApplier("removed", false, true);
            IDisposable registration = coordinator.Register(sceneContext, applier);
            var task = coordinator.ApplyLoadedDataAsync(sceneContext, CancellationToken.None).SuppressCancellationThrow();

            registration.Dispose();
            bool wasCancelled = false;
            yield return task.ToCoroutine(result => wasCancelled = result.IsCanceled);

            Assert.That(wasCancelled, Is.True);
            Assert.That(applier.ApplicationToken.IsCancellationRequested, Is.True);
        }

        [UnityTest]
        public IEnumerator ApplyLoadedDataAsync_CoordinatorDisposed_CancelsPendingApplication()
        {
            SceneDataApplicationCoordinator coordinator = CreateCoordinator(2f);
            LoadedSceneContext sceneContext = CreateSceneContext();
            var applier = new TestSceneDataApplier("disposed", false, true);
            coordinator.Register(sceneContext, applier);
            var task = coordinator.ApplyLoadedDataAsync(sceneContext, CancellationToken.None).SuppressCancellationThrow();

            coordinator.Dispose();
            bool wasCancelled = false;
            yield return task.ToCoroutine(result => wasCancelled = result.IsCanceled);

            Assert.That(wasCancelled, Is.True);
            Assert.That(applier.ApplicationToken.IsCancellationRequested, Is.True);
        }

        private static SceneDataApplicationCoordinator CreateCoordinator(float timeoutSeconds)
        {
            return new SceneDataApplicationCoordinator(
                new SceneDataApplicationExecutionPlanBuilder(),
                new SceneDataApplicationConfiguration(timeoutSeconds));
        }

        private static LoadedSceneContext CreateSceneContext()
        {
            return new LoadedSceneContext(1, "test-scene");
        }

        private sealed class TestSceneDataApplier : ILoadedSceneDataApplier
        {
            private readonly bool _shouldFail;
            private readonly UniTaskCompletionSource _completionSource;

            public string DataApplierId { get; }
            public int ApplicationCount { get; private set; }
            public CancellationToken ApplicationToken { get; private set; }

            public TestSceneDataApplier(
                string dataApplierId,
                bool shouldFail,
                bool shouldWait = false)
            {
                DataApplierId = dataApplierId;
                _shouldFail = shouldFail;
                _completionSource = shouldWait ? new UniTaskCompletionSource() : null;
            }

            public async UniTask ApplyLoadedDataAsync(CancellationToken cancellationToken)
            {
                ApplicationCount++;
                ApplicationToken = cancellationToken;
                if (_completionSource != null)
                {
                    await _completionSource.Task.AttachExternalCancellation(cancellationToken);
                }

                if (_shouldFail)
                {
                    throw new InvalidOperationException("Expected test failure.");
                }
            }

            public void Complete()
            {
                _completionSource.TrySetResult();
            }
        }

        private sealed class NonCancellableTestSceneDataApplier : ILoadedSceneDataApplier
        {
            private readonly UniTaskCompletionSource _completionSource = new();

            public string DataApplierId { get; }
            public int ApplicationCount { get; private set; }

            public NonCancellableTestSceneDataApplier(string dataApplierId)
            {
                DataApplierId = dataApplierId;
            }

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
    }
}
