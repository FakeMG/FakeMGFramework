using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FakeMG.GridSystem.Tests.PlayMode
{
    /// <summary>Loads, places and releases a real prefab through the service that the installer composes.</summary>
    public sealed class InstalledGridPlacementTests
    {
        private const int ROTATION_DEGREES = 90;
        private const float MAX_ROTATION_ERROR_DEGREES = 0.01f;
        private static readonly TimeSpan RELEASE_TIMEOUT = TimeSpan.FromSeconds(1);

        private InstalledGridFixture _grid;

        #region Public Methods

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return CreateFixtureAsync().ToCoroutine();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return _grid.DisposeAsync().ToCoroutine();
        }

        [UnityTest]
        public IEnumerator PlaceStructure_FrameworkTestPrefab_CreatesValidInitializedPlacement()
        {
            return UniTask.ToCoroutine(async () =>
            {
                Vector3 gridWorldPositionMeters = new(3.5f, 0f, -2.5f);
                var placementProcessor = Substitute.For<IGridOccupantPlacementProcessor>();

                bool wasPlaced = await _grid.Placements.PlaceStructureIfEmptyAsync(
                    _grid.StructureSO,
                    gridWorldPositionMeters,
                    ROTATION_DEGREES,
                    placementProcessor,
                    CancellationToken.None);

                Assert.IsTrue(wasPlaced);
                Assert.IsTrue(_grid.Placements.TryGetPlacementAtPosition(gridWorldPositionMeters, out GridOccupantPlacement placement));
                Assert.AreSame(_grid.StructureSO, placement.StructureSO);
                Assert.AreEqual(gridWorldPositionMeters, placement.WorldPosition);
                Assert.AreEqual(ROTATION_DEGREES, placement.RotationDegrees);
                Assert.IsTrue(placement.Footprint.TryValidate());
                Assert.AreEqual(gridWorldPositionMeters, placement.RuntimeInstance.transform.position);
                Assert.Less(
                    Quaternion.Angle(Quaternion.Euler(0f, ROTATION_DEGREES, 0f), placement.RuntimeInstance.transform.rotation),
                    MAX_ROTATION_ERROR_DEGREES);
                Assert.AreEqual(placement.InstanceId, placement.RuntimeInstance.GetComponent<GridOccupantIdentity>().InstanceId);
                placementProcessor.Received(1).Process(placement.RuntimeInstance);
            });
        }

        [UnityTest]
        public IEnumerator DestroyStructure_PlacedStructure_DestroysInstanceAndReleasesItsPrefab()
        {
            return UniTask.ToCoroutine(async () =>
            {
                GridOccupantPlacement placement = await _grid.PlaceAtCellAsync(Vector3Int.zero);
                GameObject runtimeInstance = placement.RuntimeInstance;
                Assert.AreEqual(0, _grid.Provider.ReleaseCount);

                bool wasDestroyed = _grid.Placements.DestroyStructure(placement.WorldPosition);
                await UniTask.NextFrame();
                // Addressables retains a reference until its deferred completion callbacks finish.
                await UniTask.WaitUntil(HasReleasedPrefab, PlayerLoopTiming.LastPostLateUpdate).Timeout(RELEASE_TIMEOUT);

                Assert.IsTrue(wasDestroyed);
                Assert.IsFalse(runtimeInstance);
                Assert.AreEqual(1, _grid.Provider.ReleaseCount);
            });
        }

        [UnityTest]
        public IEnumerator PlaceStructure_PreCanceledToken_ThrowsCancellationWithoutRuntimeInstance()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using CancellationTokenSource cancellationTokenSource = new();
                cancellationTokenSource.Cancel();
                int identityCountBefore = UnityEngine.Object.FindObjectsByType<GridOccupantIdentity>(FindObjectsSortMode.None).Length;
                bool wasCanceled = false;

                try
                {
                    await _grid.Placements.PlaceStructureIfEmptyAsync(_grid.StructureSO, Vector3.zero, null, cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    wasCanceled = true;
                }

                Assert.IsTrue(wasCanceled);
                int identityCountAfter = UnityEngine.Object.FindObjectsByType<GridOccupantIdentity>(FindObjectsSortMode.None).Length;
                Assert.AreEqual(identityCountBefore, identityCountAfter);
                Assert.IsEmpty(_grid.Placements.GetPlacedStructures());
            });
        }

        #endregion

        #region Private Methods

        private async UniTask CreateFixtureAsync()
        {
            _grid = await InstalledGridFixture.CreateAsync();
        }

        private bool HasReleasedPrefab()
        {
            return _grid.Provider.ReleaseCount > 0;
        }

        #endregion
    }
}
