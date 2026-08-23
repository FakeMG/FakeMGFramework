using System;
using System.Collections;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.SceneLoading;
using NUnit.Framework;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace FakeMG.SaveLoad.Tests.PlayMode
{
    public sealed class SceneDataApplicationCoordinatorTests
    {
        [UnityTest]
        public IEnumerator ApplyLoadedDataAsync_NoParticipants_Succeeds()
        {
            using var coordinator = new SceneDataApplicationCoordinator();
            SceneDataApplicationResult result = default;

            yield return coordinator.ApplyLoadedDataAsync(
                    SceneManager.GetActiveScene(),
                    1f,
                    CancellationToken.None)
                .ToCoroutine(completedResult => result = completedResult);

            Assert.That(result.Succeeded, Is.True);
        }

        [UnityTest]
        public IEnumerator ApplyLoadedDataAsync_ParticipantFailure_ReturnsParticipantId()
        {
            using var coordinator = new SceneDataApplicationCoordinator();
            Scene scene = SceneManager.GetActiveScene();
            var applier = new TestSceneDataApplier("failing", true);
            using IDisposable registration = coordinator.Register(scene, applier);
            SceneDataApplicationResult result = default;
            LogAssert.Expect(
                LogType.Error,
                new Regex("Scene data applier 'failing' failed"));

            yield return coordinator.ApplyLoadedDataAsync(scene, 1f, CancellationToken.None)
                .ToCoroutine(completedResult => result = completedResult);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FailedDataApplierIds, Is.EquivalentTo(new[] { "failing" }));
        }

        [UnityTest]
        public IEnumerator ApplyLoadedDataAsync_SameSceneConcurrentCalls_AppliesParticipantOnce()
        {
            using var coordinator = new SceneDataApplicationCoordinator();
            Scene scene = SceneManager.GetActiveScene();
            var applier = new TestSceneDataApplier("shared", false, true);
            using IDisposable registration = coordinator.Register(scene, applier);
            UniTask<SceneDataApplicationResult> firstTask = coordinator
                .ApplyLoadedDataAsync(scene, 2f, CancellationToken.None)
                .Preserve();
            UniTask<SceneDataApplicationResult> secondTask = coordinator
                .ApplyLoadedDataAsync(scene, 2f, CancellationToken.None)
                .Preserve();

            Assert.Throws<InvalidOperationException>(() =>
                coordinator.Register(scene, new TestSceneDataApplier("late", false)));
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
            using var coordinator = new SceneDataApplicationCoordinator();
            Scene scene = SceneManager.GetActiveScene();
            var completedApplier = new TestSceneDataApplier("completed", false);
            var blockedApplier = new TestSceneDataApplier("blocked", false, true);
            using IDisposable completedRegistration = coordinator.Register(scene, completedApplier);
            using IDisposable blockedRegistration = coordinator.Register(scene, blockedApplier);
            SceneDataApplicationResult result = default;
            LogAssert.Expect(
                LogType.Error,
                new Regex("Loaded-data application for scene .* exceeded"));

            yield return coordinator.ApplyLoadedDataAsync(scene, 0.05f, CancellationToken.None)
                .ToCoroutine(completedResult => result = completedResult);

            Assert.That(result.DidTimeOut, Is.True);
            Assert.That(result.FailedDataApplierIds, Is.EquivalentTo(new[] { "blocked" }));
        }

        [UnityTest]
        public IEnumerator ApplyLoadedDataAsync_AfterSuccessfulApplication_DoesNotApplyParticipantAgain()
        {
            using var coordinator = new SceneDataApplicationCoordinator();
            Scene scene = SceneManager.GetActiveScene();
            var applier = new TestSceneDataApplier("single-application", false);
            using IDisposable registration = coordinator.Register(scene, applier);

            yield return coordinator.ApplyLoadedDataAsync(scene, 1f, CancellationToken.None).ToCoroutine();
            yield return coordinator.ApplyLoadedDataAsync(scene, 1f, CancellationToken.None).ToCoroutine();

            Assert.That(applier.ApplicationCount, Is.EqualTo(1));
        }

        private sealed class TestSceneDataApplier : ILoadedSceneDataApplier
        {
            private readonly bool _shouldFail;
            private readonly UniTaskCompletionSource _completionSource;

            public string DataApplierId { get; }
            public int ApplicationCount { get; private set; }

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
    }
}
