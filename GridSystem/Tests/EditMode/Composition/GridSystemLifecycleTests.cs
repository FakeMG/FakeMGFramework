using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace FakeMG.GridSystem.Tests.EditMode
{
    public sealed class GridSystemLifecycleTests
    {
        private const string INSTANCE_ID = "lifecycle-structure";

        private IGridPlacementGateway _gridPlacementGateway;
        private IGridOccupantPlacementFactory _structurePlacementFactory;
        private GridOccupantRegistry _gridOccupantRegistry;
        private PlacementState _placementState;
        private GridOccupantPlacementService _placementService;
        private GridSystemLifecycle _lifecycle;
        private StructureSO _structureSO;
        private GameObject _runtimeInstance;
        private bool _wasCommittedStateRestored;

        #region Public Methods

        [SetUp]
        public void SetUp()
        {
            _gridPlacementGateway = Substitute.For<IGridPlacementGateway>();
            _structurePlacementFactory = Substitute.For<IGridOccupantPlacementFactory>();
            _gridOccupantRegistry = new GridOccupantRegistry();
            _placementState = new PlacementState();
            _placementService = new GridOccupantPlacementService(
                _gridPlacementGateway,
                _placementState,
                _gridOccupantRegistry,
                _structurePlacementFactory);
            _placementService.OnCommittedStateRestored += RecordCommittedStateRestored;
            _lifecycle = new GridSystemLifecycle(_placementService);
            _structureSO = ScriptableObject.CreateInstance<StructureSO>();
        }

        [TearDown]
        public void TearDown()
        {
            _placementService.OnCommittedStateRestored -= RecordCommittedStateRestored;

            if (_runtimeInstance)
            {
                Object.DestroyImmediate(_runtimeInstance);
            }

            if (_structureSO)
            {
                Object.DestroyImmediate(_structureSO);
            }
        }

        [Test]
        public async Task StartAsync_EmptyCommittedState_RaisesRestoreCompletion()
        {
            await _lifecycle.StartAsync(CancellationToken.None);

            Assert.IsTrue(_wasCommittedStateRestored);
            _gridPlacementGateway.Received(1).RebuildOccupancyIndex(
                Arg.Any<IReadOnlyCollection<GridOccupantPlacement>>());
        }

        [Test]
        public void Dispose_RuntimePlacement_ClearsRuntimeAndPreservesCommittedState()
        {
            GridOccupantPlacement runtimePlacement = SeedRuntimePlacement();

            _lifecycle.Dispose();

            Assert.AreEqual(0, _placementService.GetPlacedStructures().Count);
            Assert.IsTrue(_placementState.TryGetStructure(INSTANCE_ID, out StructureSO committedStructureSO));
            Assert.AreSame(_structureSO, committedStructureSO);
            _structurePlacementFactory.Received(1).DestroyStructure(runtimePlacement);
            _gridPlacementGateway.Received(1).RebuildOccupancyIndex(
                Arg.Any<IReadOnlyCollection<GridOccupantPlacement>>());
        }

        [Test]
        public void Dispose_CalledTwice_DoesNotDestroyRuntimeTwice()
        {
            GridOccupantPlacement runtimePlacement = SeedRuntimePlacement();

            _lifecycle.Dispose();
            _lifecycle.Dispose();

            _structurePlacementFactory.Received(1).DestroyStructure(runtimePlacement);
            _gridPlacementGateway.Received(2).RebuildOccupancyIndex(
                Arg.Any<IReadOnlyCollection<GridOccupantPlacement>>());
        }

        #endregion

        #region Private Methods

        private GridOccupantPlacement SeedRuntimePlacement()
        {
            _runtimeInstance = new GameObject(INSTANCE_ID);
            GridOccupantPlacement runtimePlacement = new(
                INSTANCE_ID,
                _structureSO,
                _runtimeInstance,
                null,
                default,
                Vector3.zero,
                0);
            _gridOccupantRegistry.Upsert(runtimePlacement);
            _placementState.UpsertStructure(
                INSTANCE_ID,
                _structureSO,
                Vector3.zero,
                0,
                new[] { Vector3Int.zero });
            return runtimePlacement;
        }

        private void RecordCommittedStateRestored()
        {
            _wasCommittedStateRestored = true;
        }

        #endregion
    }
}
