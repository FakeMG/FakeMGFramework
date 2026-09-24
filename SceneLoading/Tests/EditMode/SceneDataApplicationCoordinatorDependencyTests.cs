using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FakeMG.SceneLoading.Tests.EditMode
{
    public sealed class SceneDataApplicationCoordinatorDependencyTests
    {
        private const string INDEPENDENT_FIRST_ID = "independent-first";
        private const string INDEPENDENT_SECOND_ID = "independent-second";
        private const string PREREQUISITE_ID = "prerequisite";
        private const string DEPENDENT_ID = "dependent";
        private const string BLOCKED_ID = "blocked";
        private const string LATER_DEPENDENT_ID = "later-dependent";
        private const int MAX_STARTUP_FRAMES = 30;

        #region Public Methods

        [UnityTest]
        public IEnumerator ApplyLoadedDataAsync_IndependentAppliers_StartInParallel()
        {
            using SceneDataApplicationCoordinator coordinator = CreateCoordinator(2f);
            LoadedSceneContext sceneContext = CreateSceneContext();
            var firstApplier = new GatedTestDataApplier(INDEPENDENT_FIRST_ID);
            var secondApplier = new GatedTestDataApplier(INDEPENDENT_SECOND_ID);
            using IDisposable firstRegistration = coordinator.Register(sceneContext, firstApplier);
            using IDisposable secondRegistration = coordinator.Register(sceneContext, secondApplier);
            UniTask<SceneDataApplicationResult> applicationTask = coordinator
                .ApplyLoadedDataAsync(sceneContext, CancellationToken.None)
                .Preserve();

            try
            {
                for (int frame = 0;
                     frame < MAX_STARTUP_FRAMES && (!firstApplier.HasStarted || !secondApplier.HasStarted);
                     frame++)
                {
                    yield return null;
                }

                Assert.That(firstApplier.HasStarted, Is.True, "The first independent applier did not start.");
                Assert.That(secondApplier.HasStarted, Is.True, "The second independent applier did not start in the same wave.");

                firstApplier.Release();
                secondApplier.Release();
                SceneDataApplicationResult result = default;
                yield return applicationTask.ToCoroutine(completedResult => result = completedResult);

                Assert.That(result.Succeeded, Is.True);
                Assert.That(firstApplier.ApplicationCount, Is.EqualTo(1));
                Assert.That(secondApplier.ApplicationCount, Is.EqualTo(1));
            }
            finally
            {
                firstApplier.Release();
                secondApplier.Release();
            }
        }

        [UnityTest]
        public IEnumerator ApplyLoadedDataAsync_FailedPrerequisite_DoesNotStartDependent()
        {
            using SceneDataApplicationCoordinator coordinator = CreateCoordinator(1f);
            LoadedSceneContext sceneContext = CreateSceneContext();
            var prerequisiteApplier = new FailingTestDataApplier(PREREQUISITE_ID);
            var dependentApplier = new DependentTrackingTestDataApplier(DEPENDENT_ID, PREREQUISITE_ID);
            using IDisposable prerequisiteRegistration = coordinator.Register(sceneContext, prerequisiteApplier);
            using IDisposable dependentRegistration = coordinator.Register(sceneContext, dependentApplier);
            LogAssert.Expect(
                LogType.Error,
                new Regex($"Scene data applier '{PREREQUISITE_ID}' failed"));

            SceneDataApplicationResult result = default;
            yield return coordinator.ApplyLoadedDataAsync(sceneContext, CancellationToken.None)
                .ToCoroutine(completedResult => result = completedResult);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FailedDataApplierIds, Is.EquivalentTo(new[] { PREREQUISITE_ID }));
            Assert.That(dependentApplier.ApplicationCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator ApplyLoadedDataAsync_Timeout_DoesNotStartLaterDependencyWave()
        {
            using SceneDataApplicationCoordinator coordinator = CreateCoordinator(0.05f);
            LoadedSceneContext sceneContext = CreateSceneContext();
            var blockedApplier = new GatedTestDataApplier(BLOCKED_ID);
            var laterDependentApplier = new DependentTrackingTestDataApplier(LATER_DEPENDENT_ID, BLOCKED_ID);
            using IDisposable blockedRegistration = coordinator.Register(sceneContext, blockedApplier);
            using IDisposable laterDependentRegistration = coordinator.Register(sceneContext, laterDependentApplier);
            LogAssert.Expect(
                LogType.Error,
                new Regex("Loaded-data application for scene .* exceeded"));

            SceneDataApplicationResult result = default;
            yield return coordinator.ApplyLoadedDataAsync(sceneContext, CancellationToken.None)
                .ToCoroutine(completedResult => result = completedResult);

            Assert.That(result.DidTimeOut, Is.True);
            Assert.That(result.FailedDataApplierIds, Is.EquivalentTo(new[] { BLOCKED_ID, LATER_DEPENDENT_ID }));
            Assert.That(laterDependentApplier.ApplicationCount, Is.EqualTo(0));
            Assert.That(blockedApplier.ApplicationToken.IsCancellationRequested, Is.True);
            blockedApplier.Release();
        }

        [UnityTest]
        public IEnumerator ApplyLoadedDataAsync_UnregisterCancellation_DoesNotStartDependentWave()
        {
            using SceneDataApplicationCoordinator coordinator = CreateCoordinator(2f);
            LoadedSceneContext sceneContext = CreateSceneContext();
            var blockedApplier = new GatedTestDataApplier(BLOCKED_ID);
            var laterDependentApplier = new DependentTrackingTestDataApplier(LATER_DEPENDENT_ID, BLOCKED_ID);
            IDisposable blockedRegistration = coordinator.Register(sceneContext, blockedApplier);
            using IDisposable laterDependentRegistration = coordinator.Register(sceneContext, laterDependentApplier);
            var applicationTask = coordinator
                .ApplyLoadedDataAsync(sceneContext, CancellationToken.None)
                .SuppressCancellationThrow();

            for (int frame = 0;
                 frame < MAX_STARTUP_FRAMES && !blockedApplier.HasStarted;
                 frame++)
            {
                yield return null;
            }

            Assert.That(blockedApplier.HasStarted, Is.True);
            blockedRegistration.Dispose();

            bool wasCancelled = false;
            yield return applicationTask.ToCoroutine(result => wasCancelled = result.IsCanceled);

            Assert.That(wasCancelled, Is.True);
            Assert.That(blockedApplier.ApplicationToken.IsCancellationRequested, Is.True);
            Assert.That(laterDependentApplier.ApplicationCount, Is.EqualTo(0));
            blockedApplier.Release();
        }

        #endregion

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

        private sealed class GatedTestDataApplier : ILoadedSceneDataApplier
        {
            private readonly UniTaskCompletionSource _releaseSource = new();

            public GatedTestDataApplier(string dataApplierId)
            {
                DataApplierId = dataApplierId;
            }

            public string DataApplierId { get; }
            public bool HasStarted { get; private set; }
            public int ApplicationCount { get; private set; }
            public CancellationToken ApplicationToken { get; private set; }

            public async UniTask ApplyLoadedDataAsync(CancellationToken cancellationToken)
            {
                ApplicationCount++;
                ApplicationToken = cancellationToken;
                HasStarted = true;
                await _releaseSource.Task.AttachExternalCancellation(cancellationToken);
            }

            public void Release()
            {
                _releaseSource.TrySetResult();
            }
        }

        private sealed class FailingTestDataApplier : ILoadedSceneDataApplier
        {
            public FailingTestDataApplier(string dataApplierId)
            {
                DataApplierId = dataApplierId;
            }

            public string DataApplierId { get; }

            public UniTask ApplyLoadedDataAsync(CancellationToken cancellationToken)
            {
                return UniTask.FromException(new InvalidOperationException("Expected test failure."));
            }
        }

        private sealed class DependentTrackingTestDataApplier : ILoadedSceneDataApplier, ILoadedSceneDataApplierDependencies
        {
            public DependentTrackingTestDataApplier(
                string dataApplierId,
                params string[] prerequisiteDataApplierIds)
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
