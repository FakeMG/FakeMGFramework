using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace FakeMG.GridSystem.Tests.EditMode
{
    /// <summary>
    /// Locks GridOccupantPlacementService behavior through substituted grid and factory collaborators.
    /// </summary>
    public sealed class GridOccupantPlacementServiceTests
    {
        private const int ROTATION_DEGREES = 90;
        private const string INSTANCE_ID = "structure-instance";

        private readonly List<Object> _createdUnityObjects = new();
        private readonly List<PlacementChange> _placementChanges = new();

        private IGridPlacementGateway _gridPlacementGateway;
        private IGridOccupantPlacementFactory _structurePlacementFactory;
        private PlacementState _placementState;
        private GridOccupantRegistry _placedStructureRegistry;
        private GridOccupantPlacementService _service;
        private StructureSO _structureSO;
        private Vector3 _worldPosition;
        private Vector3 _gridWorldPosition;
        private bool _wasCommittedStateRestored;

        #region Public Methods

        [SetUp]
        public void SetUp()
        {
            _gridPlacementGateway = Substitute.For<IGridPlacementGateway>();
            _structurePlacementFactory = Substitute.For<IGridOccupantPlacementFactory>();
            _placementState = new PlacementState();
            _placedStructureRegistry = new GridOccupantRegistry();
            _service = new GridOccupantPlacementService(
                _gridPlacementGateway,
                _placementState,
                _placedStructureRegistry,
                _structurePlacementFactory);
            _service.OnPlacementChanged += RecordPlacementChange;
            _service.OnCommittedStateRestored += RecordCommittedStateRestored;

            _structureSO = ScriptableObject.CreateInstance<StructureSO>();
            _createdUnityObjects.Add(_structureSO);

            _worldPosition = new Vector3(2f, 0f, 3f);
            _gridWorldPosition = new Vector3(2.5f, 0f, 3.5f);

            _gridPlacementGateway.WorldToGridWorld(Arg.Any<Vector3>())
                .Returns(callInfo => (Vector3)callInfo[0]);
            _gridPlacementGateway.WorldToGridWorld(_worldPosition).Returns(_gridWorldPosition);
            _gridPlacementGateway.CanOccupy(
                    null,
                    Arg.Any<Vector3>(),
                    Arg.Any<int>(),
                    Arg.Any<string>())
                .Returns(true);
        }

        [TearDown]
        public void TearDown()
        {
            _service.OnPlacementChanged -= RecordPlacementChange;
            _service.OnCommittedStateRestored -= RecordCommittedStateRestored;

            foreach (Object createdUnityObject in _createdUnityObjects)
            {
                if (createdUnityObject)
                {
                    Object.DestroyImmediate(createdUnityObject);
                }
            }

            _createdUnityObjects.Clear();
            _placementChanges.Clear();
            _wasCommittedStateRestored = false;
        }

        [Test]
        public async Task PlaceStructureIfEmptyAsync_Success_CommitsStateAndRaisesCreated()
        {
            GridOccupantPlacement runtimePlacement = CreateRuntimePlacement(INSTANCE_ID, _gridWorldPosition, ROTATION_DEGREES);
            SetFactoryCreatedPlacement(runtimePlacement);

            bool wasPlaced = await _service.PlaceStructureIfEmptyAsync(
                _structureSO,
                _worldPosition,
                ROTATION_DEGREES,
                null,
                CancellationToken.None);

            Assert.IsTrue(wasPlaced);
            Assert.IsTrue(_placementState.TryGetStructure(INSTANCE_ID, out StructureSO committedStructureSO));
            Assert.AreEqual(_structureSO, committedStructureSO);
            Assert.AreEqual(1, _service.GetPlacedStructures().Count);
            Assert.AreEqual(1, _placementChanges.Count);
            Assert.AreEqual(PlacementChangeKind.Created, _placementChanges[0].Kind);
            Assert.AreEqual(runtimePlacement, _placementChanges[0].GridOccupantPlacement);
            Assert.AreEqual(INSTANCE_ID, _placementChanges[0].InstanceId);
            _gridPlacementGateway.Received(1).RebuildOccupancyIndex(Arg.Any<IReadOnlyCollection<GridOccupantPlacement>>());
            await _structurePlacementFactory.Received(1).CreateStructureAsync(
                Arg.Any<string>(),
                _structureSO,
                _gridWorldPosition,
                ROTATION_DEGREES,
                Arg.Any<CancellationToken>(),
                Arg.Any<string>(),
                null);
        }

        [Test]
        public async Task PlaceStructureIfEmptyAsync_FactoryReturnsNull_DoesNotCommit()
        {
            SetFactoryCreatedPlacement(null);

            bool wasPlaced = await _service.PlaceStructureIfEmptyAsync(
                _structureSO,
                _worldPosition,
                ROTATION_DEGREES,
                null,
                CancellationToken.None);

            Assert.IsFalse(wasPlaced);
            Assert.AreEqual(0, _placementState.Structures.Count);
            Assert.AreEqual(0, _service.GetPlacedStructures().Count);
            Assert.AreEqual(0, _placementChanges.Count);
            _gridPlacementGateway.DidNotReceive().RebuildOccupancyIndex(Arg.Any<IReadOnlyCollection<GridOccupantPlacement>>());
        }

        [Test]
        public async Task PlaceStructureIfEmptyAsync_OccupiedPlacement_DestroysRejectedRuntimeInstance()
        {
            GridOccupantPlacement runtimePlacement = CreateRuntimePlacement(INSTANCE_ID, _gridWorldPosition, ROTATION_DEGREES);
            SetFactoryCreatedPlacement(runtimePlacement);
            _gridPlacementGateway.CanOccupy(null, _gridWorldPosition, ROTATION_DEGREES, null).Returns(false);

            bool wasPlaced = await _service.PlaceStructureIfEmptyAsync(
                _structureSO,
                _worldPosition,
                ROTATION_DEGREES,
                null,
                CancellationToken.None);

            Assert.IsFalse(wasPlaced);
            Assert.AreEqual(0, _placementState.Structures.Count);
            Assert.AreEqual(0, _service.GetPlacedStructures().Count);
            Assert.AreEqual(0, _placementChanges.Count);
            _structurePlacementFactory.Received(1).DestroyStructure(runtimePlacement);
            _gridPlacementGateway.DidNotReceive().RebuildOccupancyIndex(Arg.Any<IReadOnlyCollection<GridOccupantPlacement>>());
        }

        [Test]
        public void DestroyStructure_ExistingPlacement_RemovesStateDestroysRuntimeAndRaisesRemoved()
        {
            GridOccupantPlacement runtimePlacement = SeedRuntimePlacement(INSTANCE_ID, _gridWorldPosition, ROTATION_DEGREES);
            SetInstanceLookup(_worldPosition, INSTANCE_ID);

            bool wasDestroyed = _service.DestroyStructure(_worldPosition);

            Assert.IsTrue(wasDestroyed);
            Assert.IsFalse(_placementState.TryGetStructure(INSTANCE_ID, out _));
            Assert.IsFalse(_service.TryGetPlacement(INSTANCE_ID, out _));
            Assert.AreEqual(1, _placementChanges.Count);
            Assert.AreEqual(PlacementChangeKind.Removed, _placementChanges[0].Kind);
            Assert.IsNull(_placementChanges[0].GridOccupantPlacement);
            Assert.AreEqual(INSTANCE_ID, _placementChanges[0].InstanceId);
            _structurePlacementFactory.Received(1).DestroyStructure(runtimePlacement);
            _gridPlacementGateway.Received(1).RebuildOccupancyIndex(Arg.Any<IReadOnlyCollection<GridOccupantPlacement>>());
        }

        [Test]
        public void TryPickUpStructure_ExistingPlacement_DetachesRuntimePlacementAndRaisesRemoved()
        {
            GridOccupantPlacement runtimePlacement = SeedRuntimePlacement(INSTANCE_ID, _gridWorldPosition, ROTATION_DEGREES);
            SetInstanceLookup(_worldPosition, INSTANCE_ID);

            bool wasPickedUp = _service.TryPickUpStructure(_worldPosition, out GridOccupantPlacement heldStructurePlacement);

            Assert.IsTrue(wasPickedUp);
            Assert.AreEqual(runtimePlacement, heldStructurePlacement);
            Assert.IsFalse(runtimePlacement.RuntimeInstance.activeSelf);
            Assert.IsFalse(_service.TryGetPlacement(INSTANCE_ID, out _));
            Assert.AreEqual(1, _placementChanges.Count);
            Assert.AreEqual(PlacementChangeKind.Removed, _placementChanges[0].Kind);
            Assert.AreEqual(runtimePlacement, _placementChanges[0].GridOccupantPlacement);
            Assert.AreEqual(INSTANCE_ID, _placementChanges[0].InstanceId);
            _structurePlacementFactory.DidNotReceive().DestroyStructure(Arg.Any<GridOccupantPlacement>());
            _gridPlacementGateway.Received(1).RebuildOccupancyIndex(Arg.Any<IReadOnlyCollection<GridOccupantPlacement>>());
        }

        [Test]
        public void TryPlaceHeldStructure_ValidTarget_CommitsNewPositionAndRaisesMoved()
        {
            GridOccupantPlacement runtimePlacement = SeedRuntimePlacement(INSTANCE_ID, _gridWorldPosition, ROTATION_DEGREES);
            Vector3 newWorldPosition = new(6f, 0f, 7f);
            Vector3 newGridWorldPosition = new(6.5f, 0f, 7.5f);
            SetInstanceLookup(_worldPosition, INSTANCE_ID);
            _service.TryPickUpStructure(_worldPosition, out GridOccupantPlacement heldStructurePlacement);
            _placementChanges.Clear();
            _gridPlacementGateway.ClearReceivedCalls();
            _gridPlacementGateway.WorldToGridWorld(newWorldPosition).Returns(newGridWorldPosition);
            _gridPlacementGateway.CanOccupy(null, newGridWorldPosition, ROTATION_DEGREES, INSTANCE_ID).Returns(true);

            bool wasPlaced = _service.TryPlaceHeldStructure(heldStructurePlacement, newWorldPosition);

            Assert.IsTrue(wasPlaced);
            Assert.IsTrue(runtimePlacement.RuntimeInstance.activeSelf);
            Assert.AreEqual(newGridWorldPosition, runtimePlacement.WorldPosition);
            Assert.IsTrue(_service.TryGetPlacement(INSTANCE_ID, out GridOccupantPlacement committedPlacement));
            Assert.AreEqual(runtimePlacement, committedPlacement);
            Assert.AreEqual(1, _placementChanges.Count);
            Assert.AreEqual(PlacementChangeKind.Moved, _placementChanges[0].Kind);
            Assert.AreEqual(runtimePlacement, _placementChanges[0].GridOccupantPlacement);
            _gridPlacementGateway.Received(1).RebuildOccupancyIndex(Arg.Any<IReadOnlyCollection<GridOccupantPlacement>>());
        }

        [Test]
        public void RestoreHeldStructure_NullHeldStructure_ReturnsFalseWithoutStateChanges()
        {
            bool wasRestored = _service.RestoreHeldStructure(null);

            Assert.IsFalse(wasRestored);
            Assert.AreEqual(0, _placementState.Structures.Count);
            Assert.AreEqual(0, _service.GetPlacedStructures().Count);
            Assert.AreEqual(0, _placementChanges.Count);
            _gridPlacementGateway.DidNotReceive().RebuildOccupancyIndex(Arg.Any<IReadOnlyCollection<GridOccupantPlacement>>());
        }

        [Test]
        public void ClearAllStructures_WithRuntimeStructures_ClearsStateDestroysRuntimeAndRaisesCleared()
        {
            GridOccupantPlacement firstPlacement = SeedRuntimePlacement("instance-a", Vector3.zero, 0);
            GridOccupantPlacement secondPlacement = SeedRuntimePlacement("instance-b", Vector3.one, 90);
            _gridPlacementGateway.ClearReceivedCalls();

            _service.ClearAllStructures();

            Assert.AreEqual(0, _placementState.Structures.Count);
            Assert.AreEqual(0, _service.GetPlacedStructures().Count);
            Assert.AreEqual(1, _placementChanges.Count);
            Assert.AreEqual(PlacementChangeKind.Cleared, _placementChanges[0].Kind);
            _structurePlacementFactory.Received(1).DestroyStructure(firstPlacement);
            _structurePlacementFactory.Received(1).DestroyStructure(secondPlacement);
            _gridPlacementGateway.Received(1).RebuildOccupancyIndex(Arg.Any<IReadOnlyCollection<GridOccupantPlacement>>());
        }

        [Test]
        public void ClearAllStructures_WithoutRuntimeStructures_DoesNotRaiseCleared()
        {
            _service.ClearAllStructures();

            Assert.AreEqual(0, _placementState.Structures.Count);
            Assert.AreEqual(0, _service.GetPlacedStructures().Count);
            Assert.AreEqual(0, _placementChanges.Count);
            _structurePlacementFactory.DidNotReceive().DestroyStructure(Arg.Any<GridOccupantPlacement>());
            _gridPlacementGateway.Received(1).RebuildOccupancyIndex(Arg.Any<IReadOnlyCollection<GridOccupantPlacement>>());
        }

        [Test]
        public async Task RestoreCommittedStateAsync_ValidSavedPlacement_RecreatesRuntimePlacementAndRaisesRestored()
        {
            Vector3 savedWorldPosition = new(4f, 0f, 5f);
            Vector3 savedGridWorldPosition = new(4.5f, 0f, 5.5f);
            GridOccupantPlacement restoredPlacement = CreateRuntimePlacement(INSTANCE_ID, savedGridWorldPosition, ROTATION_DEGREES);
            _placementState.UpsertStructure(INSTANCE_ID, _structureSO, savedWorldPosition, ROTATION_DEGREES);
            _gridPlacementGateway.WorldToGridWorld(savedWorldPosition).Returns(savedGridWorldPosition);
            SetFactoryCreatedPlacement(restoredPlacement);

            await _service.RestoreCommittedStateAsync(CancellationToken.None);

            Assert.IsTrue(_wasCommittedStateRestored);
            Assert.IsTrue(_service.TryGetPlacement(INSTANCE_ID, out GridOccupantPlacement committedPlacement));
            Assert.AreEqual(restoredPlacement, committedPlacement);
            Assert.AreEqual(1, _placementChanges.Count);
            Assert.AreEqual(PlacementChangeKind.Restored, _placementChanges[0].Kind);
            Assert.AreEqual(restoredPlacement, _placementChanges[0].GridOccupantPlacement);
            _gridPlacementGateway.Received(2).RebuildOccupancyIndex(Arg.Any<IReadOnlyCollection<GridOccupantPlacement>>());
        }

        #endregion

        #region Private Methods

        private GridOccupantPlacement SeedRuntimePlacement(
            string instanceId,
            Vector3 gridWorldPosition,
            int rotationDegrees)
        {
            GridOccupantPlacement structurePlacement = CreateRuntimePlacement(
                instanceId,
                gridWorldPosition,
                rotationDegrees);
            _placedStructureRegistry.Upsert(structurePlacement);
            _placementState.UpsertStructure(instanceId, _structureSO, gridWorldPosition, rotationDegrees);
            return structurePlacement;
        }

        private GridOccupantPlacement CreateRuntimePlacement(
            string instanceId,
            Vector3 gridWorldPosition,
            int rotationDegrees)
        {
            GameObject runtimeInstance = new(instanceId);
            _createdUnityObjects.Add(runtimeInstance);
            return new GridOccupantPlacement(
                instanceId,
                _structureSO,
                runtimeInstance,
                null,
                default,
                gridWorldPosition,
                rotationDegrees);
        }

        private void SetFactoryCreatedPlacement(GridOccupantPlacement structurePlacement)
        {
            _structurePlacementFactory.CreateStructureAsync(
                    Arg.Any<string>(),
                    Arg.Any<StructureSO>(),
                    Arg.Any<Vector3>(),
                    Arg.Any<int>(),
                    Arg.Any<CancellationToken>(),
                    Arg.Any<string>(),
                    Arg.Any<IGridOccupantPlacementProcessor>())
                .Returns(UniTask.FromResult(structurePlacement));
        }

        private void SetInstanceLookup(Vector3 worldPosition, string instanceId)
        {
            _gridPlacementGateway.TryGetInstanceIdAtPosition(worldPosition, out Arg.Any<string>())
                .Returns(callInfo =>
                {
                    callInfo[1] = instanceId;
                    return true;
                });
        }

        private void RecordPlacementChange(PlacementChange placementChange)
        {
            _placementChanges.Add(placementChange);
        }

        private void RecordCommittedStateRestored()
        {
            _wasCommittedStateRestored = true;
        }

        #endregion
    }
}
