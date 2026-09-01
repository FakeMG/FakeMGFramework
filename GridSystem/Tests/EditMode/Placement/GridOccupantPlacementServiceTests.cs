using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace FakeMG.GridSystem.Tests.EditMode
{
    /// <summary>
    /// Locks GridOccupantPlacementService behavior through substituted grid and factory collaborators.
    /// </summary>
    public sealed class GridOccupantPlacementServiceTests
    {
        private const int ROTATION_DEGREES = 90;
        private const string INSTANCE_ID = "structure-instance";

        private static readonly IReadOnlyList<Vector3Int> OCCUPIED_CELL_OFFSETS =
            new List<Vector3Int>
            {
                new(-1, 0, 0),
                new(0, 0, 0),
            };

        private readonly List<Object> _createdUnityObjects = new();
        private readonly List<PlacementChange> _placementChanges = new();
        private readonly List<string> _eventOrder = new();

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
            _gridPlacementGateway.CellSizeMeters.Returns(1f);
            _gridPlacementGateway.GetCanonicalOccupiedCellOffsets(Arg.Any<GridFootprint>())
                .Returns(OCCUPIED_CELL_OFFSETS);
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
            _eventOrder.Clear();
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
            CollectionAssert.AreEqual(
                OCCUPIED_CELL_OFFSETS,
                _placementState.Structures[0].OccupiedCellOffsets);
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
            _placementState.UpsertStructure(
                INSTANCE_ID,
                _structureSO,
                savedWorldPosition,
                ROTATION_DEGREES,
                OCCUPIED_CELL_OFFSETS);
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
            await _structurePlacementFactory.Received(1).CreateStructureAsync(
                INSTANCE_ID,
                _structureSO,
                savedGridWorldPosition,
                ROTATION_DEGREES,
                Arg.Any<CancellationToken>(),
                Arg.Any<string>(),
                Arg.Is<IGridOccupantPlacementProcessor>(processor =>
                    processor is GridFootprintScalePlacementProcessor));
        }

        [Test]
        public async Task PlaceStructureIfEmptyAsync_DefaultRotation_ForwardsZeroDegreesAndProcessor()
        {
            IGridOccupantPlacementProcessor placementProcessor = Substitute.For<IGridOccupantPlacementProcessor>();
            GridOccupantPlacement runtimePlacement = CreateRuntimePlacement(INSTANCE_ID, _gridWorldPosition, 0);
            SetFactoryCreatedPlacement(runtimePlacement);

            bool wasPlaced = await _service.PlaceStructureIfEmptyAsync(
                _structureSO,
                _worldPosition,
                placementProcessor,
                CancellationToken.None);

            Assert.IsTrue(wasPlaced);
            await _structurePlacementFactory.Received(1).CreateStructureAsync(
                Arg.Any<string>(),
                _structureSO,
                _gridWorldPosition,
                0,
                Arg.Any<CancellationToken>(),
                Arg.Any<string>(),
                placementProcessor);
        }

        [Test]
        public async Task ReplaceStructureAsync_MissingInstanceId_ReturnsFalseWithoutFactoryCall()
        {
            StructureSO replacementStructureSO = CreateStructureSO();

            bool wasReplaced = await _service.ReplaceStructureAsync(
                string.Empty,
                replacementStructureSO,
                CancellationToken.None);

            Assert.IsFalse(wasReplaced);
            Assert.AreEqual(0, _placementChanges.Count);
            await _structurePlacementFactory.DidNotReceiveWithAnyArgs().CreateStructureAsync(
                default,
                default,
                default,
                default,
                default,
                default,
                default);
        }

        [Test]
        public async Task ReplaceStructureAsync_MissingReplacement_ReturnsFalseWithoutStateChanges()
        {
            SeedRuntimePlacement(INSTANCE_ID, _gridWorldPosition, ROTATION_DEGREES);

            bool wasReplaced = await _service.ReplaceStructureAsync(
                INSTANCE_ID,
                null,
                CancellationToken.None);

            Assert.IsFalse(wasReplaced);
            Assert.IsTrue(_service.TryGetPlacement(INSTANCE_ID, out _));
            Assert.AreEqual(0, _placementChanges.Count);
        }

        [Test]
        public async Task ReplaceStructureAsync_MissingRuntimePlacement_ReturnsFalseWithoutStateChanges()
        {
            StructureSO replacementStructureSO = CreateStructureSO();

            bool wasReplaced = await _service.ReplaceStructureAsync(
                INSTANCE_ID,
                replacementStructureSO,
                CancellationToken.None);

            Assert.IsFalse(wasReplaced);
            Assert.AreEqual(0, _placementState.Structures.Count);
            Assert.AreEqual(0, _placementChanges.Count);
        }

        [Test]
        public async Task ReplaceStructureAsync_FactoryReturnsNull_PreservesCurrentPlacement()
        {
            GridOccupantPlacement currentPlacement = SeedRuntimePlacement(
                INSTANCE_ID,
                _gridWorldPosition,
                ROTATION_DEGREES);
            StructureSO replacementStructureSO = CreateStructureSO();
            SetFactoryCreatedPlacement(null);

            bool wasReplaced = await _service.ReplaceStructureAsync(
                INSTANCE_ID,
                replacementStructureSO,
                CancellationToken.None);

            Assert.IsFalse(wasReplaced);
            Assert.IsTrue(_service.TryGetPlacement(INSTANCE_ID, out GridOccupantPlacement storedPlacement));
            Assert.AreSame(currentPlacement, storedPlacement);
            Assert.IsTrue(_placementState.TryGetStructure(INSTANCE_ID, out StructureSO committedStructureSO));
            Assert.AreSame(_structureSO, committedStructureSO);
            Assert.AreEqual(0, _placementChanges.Count);
            _structurePlacementFactory.DidNotReceive().DestroyStructure(Arg.Any<GridOccupantPlacement>());
        }

        [Test]
        public async Task ReplaceStructureAsync_BlockedReplacement_DestroysReplacementAndPreservesCurrentPlacement()
        {
            GridOccupantPlacement currentPlacement = SeedRuntimePlacement(
                INSTANCE_ID,
                _gridWorldPosition,
                ROTATION_DEGREES);
            StructureSO replacementStructureSO = CreateStructureSO();
            GridOccupantPlacement replacementPlacement = CreateRuntimePlacement(
                INSTANCE_ID,
                _gridWorldPosition,
                ROTATION_DEGREES,
                replacementStructureSO);
            SetFactoryCreatedPlacement(replacementPlacement);
            _gridPlacementGateway.CanOccupy(
                    null,
                    _gridWorldPosition,
                    ROTATION_DEGREES,
                    INSTANCE_ID)
                .Returns(false);

            bool wasReplaced = await _service.ReplaceStructureAsync(
                INSTANCE_ID,
                replacementStructureSO,
                CancellationToken.None);

            Assert.IsFalse(wasReplaced);
            Assert.IsTrue(_service.TryGetPlacement(INSTANCE_ID, out GridOccupantPlacement storedPlacement));
            Assert.AreSame(currentPlacement, storedPlacement);
            Assert.IsTrue(_placementState.TryGetStructure(INSTANCE_ID, out StructureSO committedStructureSO));
            Assert.AreSame(_structureSO, committedStructureSO);
            Assert.AreEqual(0, _placementChanges.Count);
            _structurePlacementFactory.Received(1).DestroyStructure(replacementPlacement);
            _structurePlacementFactory.DidNotReceive().DestroyStructure(currentPlacement);
            _gridPlacementGateway.DidNotReceive().RebuildOccupancyIndex(Arg.Any<IReadOnlyCollection<GridOccupantPlacement>>());
        }

        [Test]
        public async Task ReplaceStructureAsync_ValidReplacement_CommitsAndRaisesReplaced()
        {
            GridOccupantPlacement currentPlacement = SeedRuntimePlacement(
                INSTANCE_ID,
                _gridWorldPosition,
                ROTATION_DEGREES);
            StructureSO replacementStructureSO = CreateStructureSO();
            GridOccupantPlacement replacementPlacement = CreateRuntimePlacement(
                INSTANCE_ID,
                _gridWorldPosition,
                ROTATION_DEGREES,
                replacementStructureSO);
            SetFactoryCreatedPlacement(replacementPlacement);

            bool wasReplaced = await _service.ReplaceStructureAsync(
                INSTANCE_ID,
                replacementStructureSO,
                CancellationToken.None);

            Assert.IsTrue(wasReplaced);
            Assert.IsTrue(_service.TryGetPlacement(INSTANCE_ID, out GridOccupantPlacement storedPlacement));
            Assert.AreSame(replacementPlacement, storedPlacement);
            Assert.IsTrue(_placementState.TryGetStructure(INSTANCE_ID, out StructureSO committedStructureSO));
            Assert.AreSame(replacementStructureSO, committedStructureSO);
            Assert.AreEqual(_gridWorldPosition, replacementPlacement.WorldPosition);
            Assert.AreEqual(ROTATION_DEGREES, replacementPlacement.RotationDegrees);
            Assert.AreEqual(1, _placementChanges.Count);
            Assert.AreEqual(PlacementChangeKind.Replaced, _placementChanges[0].Kind);
            Assert.AreEqual(INSTANCE_ID, _placementChanges[0].InstanceId);
            _structurePlacementFactory.Received(1).DestroyStructure(currentPlacement);
            _structurePlacementFactory.DidNotReceive().DestroyStructure(replacementPlacement);
            _gridPlacementGateway.Received(1).RebuildOccupancyIndex(Arg.Any<IReadOnlyCollection<GridOccupantPlacement>>());
        }

        [Test]
        public void TryPickUpStructure_EmptyCell_ReturnsFalseWithoutStateChanges()
        {
            GridOccupantPlacement runtimePlacement = SeedRuntimePlacement(
                INSTANCE_ID,
                _gridWorldPosition,
                ROTATION_DEGREES);

            bool wasPickedUp = _service.TryPickUpStructure(
                _worldPosition,
                out GridOccupantPlacement heldStructurePlacement);

            Assert.IsFalse(wasPickedUp);
            Assert.IsNull(heldStructurePlacement);
            Assert.IsTrue(_service.TryGetPlacement(INSTANCE_ID, out _));
            Assert.IsTrue(runtimePlacement.RuntimeInstance.activeSelf);
            Assert.AreEqual(0, _placementChanges.Count);
        }

        [Test]
        public void TryPickUpStructure_OccupancyWithoutRuntimeRecord_ReturnsFalseWithoutStateChanges()
        {
            _placementState.UpsertStructure(
                INSTANCE_ID,
                _structureSO,
                _gridWorldPosition,
                ROTATION_DEGREES,
                OCCUPIED_CELL_OFFSETS);
            SetInstanceLookup(_worldPosition, INSTANCE_ID);
#if LOGGER_ENABLED
            LogAssert.Expect(
                LogType.Error,
                "<color=red>[GridOccupantPlacementService]</color> Cannot pick up structure 'structure-instance' because its runtime records are incomplete.");
#endif

            bool wasPickedUp = _service.TryPickUpStructure(
                _worldPosition,
                out GridOccupantPlacement heldStructurePlacement);

            Assert.IsFalse(wasPickedUp);
            Assert.IsNull(heldStructurePlacement);
            Assert.IsTrue(_placementState.TryGetStructure(INSTANCE_ID, out _));
            Assert.AreEqual(0, _placementChanges.Count);
        }

        [Test]
        public void TryPlaceHeldStructure_BlockedTarget_KeepsStructureHeldAndCommittedAtOriginalPosition()
        {
            GridOccupantPlacement runtimePlacement = SeedRuntimePlacement(
                INSTANCE_ID,
                _gridWorldPosition,
                ROTATION_DEGREES);
            SetInstanceLookup(_worldPosition, INSTANCE_ID);
            Assert.IsTrue(_service.TryPickUpStructure(_worldPosition, out GridOccupantPlacement heldStructurePlacement));
            _placementChanges.Clear();
            Vector3 blockedWorldPosition = new(9f, 0f, 9f);
            _gridPlacementGateway.WorldToGridWorld(blockedWorldPosition).Returns(blockedWorldPosition);
            _gridPlacementGateway.CanOccupy(
                    null,
                    blockedWorldPosition,
                    ROTATION_DEGREES,
                    INSTANCE_ID)
                .Returns(false);

            bool wasPlaced = _service.TryPlaceHeldStructure(
                heldStructurePlacement,
                blockedWorldPosition);

            Assert.IsFalse(wasPlaced);
            Assert.IsFalse(runtimePlacement.RuntimeInstance.activeSelf);
            Assert.IsFalse(_service.TryGetPlacement(INSTANCE_ID, out _));
            Assert.AreEqual(_gridWorldPosition, _placementState.Structures[0].WorldPosition);
            Assert.AreEqual(0, _placementChanges.Count);
        }

        [Test]
        public void RestoreHeldStructure_ValidOriginalPosition_ReactivatesAndRaisesRestored()
        {
            GridOccupantPlacement runtimePlacement = SeedRuntimePlacement(
                INSTANCE_ID,
                _gridWorldPosition,
                ROTATION_DEGREES);
            SetInstanceLookup(_worldPosition, INSTANCE_ID);
            Assert.IsTrue(_service.TryPickUpStructure(_worldPosition, out GridOccupantPlacement heldStructurePlacement));
            _placementChanges.Clear();
            _gridPlacementGateway.WorldToGridWorld(_gridWorldPosition).Returns(_gridWorldPosition);

            bool wasRestored = _service.RestoreHeldStructure(heldStructurePlacement);

            Assert.IsTrue(wasRestored);
            Assert.IsTrue(runtimePlacement.RuntimeInstance.activeSelf);
            Assert.IsTrue(_service.TryGetPlacement(INSTANCE_ID, out GridOccupantPlacement storedPlacement));
            Assert.AreSame(runtimePlacement, storedPlacement);
            Assert.AreEqual(_gridWorldPosition, storedPlacement.WorldPosition);
            Assert.AreEqual(1, _placementChanges.Count);
            Assert.AreEqual(PlacementChangeKind.Restored, _placementChanges[0].Kind);
        }

        [Test]
        public void RestoreHeldStructure_BlockedOriginalPosition_KeepsStructureHeldWithoutEvents()
        {
            GridOccupantPlacement runtimePlacement = SeedRuntimePlacement(
                INSTANCE_ID,
                _gridWorldPosition,
                ROTATION_DEGREES);
            SetInstanceLookup(_worldPosition, INSTANCE_ID);
            Assert.IsTrue(_service.TryPickUpStructure(_worldPosition, out GridOccupantPlacement heldStructurePlacement));
            _placementChanges.Clear();
            _eventOrder.Clear();
            _gridPlacementGateway.WorldToGridWorld(_gridWorldPosition).Returns(_gridWorldPosition);
            _gridPlacementGateway.CanOccupy(
                    null,
                    _gridWorldPosition,
                    ROTATION_DEGREES,
                    INSTANCE_ID)
                .Returns(false);
#if LOGGER_ENABLED
            LogAssert.Expect(
                LogType.Log,
                "<color=white>[GridOccupantPlacementService]</color> Held structure placement was rejected because its grid footprint is occupied or outside the grid.");
            LogAssert.Expect(
                LogType.Error,
                "<color=red>[GridOccupantPlacementService]</color> Failed to restore held structure 'structure-instance' to its original position.");
#endif

            bool wasRestored = _service.RestoreHeldStructure(heldStructurePlacement);

            Assert.IsFalse(wasRestored);
            Assert.IsFalse(runtimePlacement.RuntimeInstance.activeSelf);
            Assert.IsFalse(_service.TryGetPlacement(INSTANCE_ID, out _));
            Assert.AreEqual(0, _placementChanges.Count);
            Assert.AreEqual(0, _eventOrder.Count);
        }

        [Test]
        public void RemoveStructure_ExistingPlacement_ReturnsStructureAndRemovesRuntime()
        {
            SeedRuntimePlacement(INSTANCE_ID, _gridWorldPosition, ROTATION_DEGREES);
            SetInstanceLookup(_worldPosition, INSTANCE_ID);

            StructureSO removedStructureSO = _service.RemoveStructure(_worldPosition);

            Assert.AreSame(_structureSO, removedStructureSO);
            Assert.IsFalse(_service.TryGetPlacement(INSTANCE_ID, out _));
            Assert.IsFalse(_placementState.TryGetStructure(INSTANCE_ID, out _));
        }

        [Test]
        public void TryGetStructurePosition_ExistingPlacement_ReturnsRuntimePosition()
        {
            SeedRuntimePlacement(INSTANCE_ID, _gridWorldPosition, ROTATION_DEGREES);
            SetInstanceLookup(_worldPosition, INSTANCE_ID);

            bool wasFound = _service.TryGetStructurePosition(
                _worldPosition,
                out Vector3 structureWorldPosition);

            Assert.IsTrue(wasFound);
            Assert.AreEqual(_gridWorldPosition, structureWorldPosition);
            Assert.AreEqual(_gridWorldPosition, _service.GetStructurePosition(_worldPosition));
        }

        [Test]
        public void TryGetPlacementAtPosition_ExistingPlacement_ReturnsRuntimePlacement()
        {
            GridOccupantPlacement runtimePlacement = SeedRuntimePlacement(
                INSTANCE_ID,
                _gridWorldPosition,
                ROTATION_DEGREES);
            SetInstanceLookup(_worldPosition, INSTANCE_ID);

            bool wasFound = _service.TryGetPlacementAtPosition(
                _worldPosition,
                out GridOccupantPlacement storedPlacement);

            Assert.IsTrue(wasFound);
            Assert.AreSame(runtimePlacement, storedPlacement);
            Assert.AreEqual(INSTANCE_ID, _service.GetPlacedStructures().Single().InstanceId);
        }

        [Test]
        public void TryGetPlacement_MissingInstanceId_ReturnsFalseAndNull()
        {
            bool wasFound = _service.TryGetPlacement(
                string.Empty,
                out GridOccupantPlacement structurePlacement);

            Assert.IsFalse(wasFound);
            Assert.IsNull(structurePlacement);
        }

        [Test]
        public void TryGetInstanceIdAtPosition_OccupiedCell_ForwardsGatewayResult()
        {
            SetInstanceLookup(_worldPosition, INSTANCE_ID);

            bool wasFound = _service.TryGetInstanceIdAtPosition(
                _worldPosition,
                out string instanceId);

            Assert.IsTrue(wasFound);
            Assert.AreEqual(INSTANCE_ID, instanceId);
            _gridPlacementGateway.Received(1).TryGetInstanceIdAtPosition(
                _worldPosition,
                out Arg.Any<string>());
        }

        [Test]
        public void GetStructureSOAtPosition_EmptyCell_ReturnsNull()
        {
            StructureSO structureSO = _service.GetStructureSOAtPosition(_worldPosition);

            Assert.IsNull(structureSO);
        }

        [Test]
        public void RemoveStructure_EmptyCell_ReturnsNullWithoutDestroyingRuntime()
        {
            StructureSO structureSO = _service.RemoveStructure(_worldPosition);

            Assert.IsNull(structureSO);
            Assert.AreEqual(0, _placementChanges.Count);
            _structurePlacementFactory.DidNotReceive().DestroyStructure(
                Arg.Any<GridOccupantPlacement>());
        }

        [Test]
        public void TryGetStructurePosition_EmptyCell_ReturnsFalseAndZero()
        {
#if LOGGER_ENABLED
            LogAssert.Expect(
                LogType.Error,
                "<color=red>[GridOccupantPlacementService]</color> Cannot get structure position because no structure occupies the selected grid cell.");
#endif

            bool wasFound = _service.TryGetStructurePosition(
                _worldPosition,
                out Vector3 structureWorldPosition);

            Assert.IsFalse(wasFound);
            Assert.AreEqual(Vector3.zero, structureWorldPosition);
        }

        [Test]
        public void GetStructurePosition_EmptyCell_ReturnsZero()
        {
#if LOGGER_ENABLED
            LogAssert.Expect(
                LogType.Error,
                "<color=red>[GridOccupantPlacementService]</color> Cannot get structure position because no structure occupies the selected grid cell.");
#endif

            Vector3 structureWorldPosition = _service.GetStructurePosition(_worldPosition);

            Assert.AreEqual(Vector3.zero, structureWorldPosition);
        }

        [Test]
        public async Task RestoreCommittedStateAsync_MissingStructure_SkipsRecordAndRaisesCompletion()
        {
            _placementState.UpsertStructure(
                INSTANCE_ID,
                null,
                _gridWorldPosition,
                ROTATION_DEGREES,
                OCCUPIED_CELL_OFFSETS);

            await _service.RestoreCommittedStateAsync(CancellationToken.None);

            Assert.IsTrue(_wasCommittedStateRestored);
            Assert.AreEqual(0, _service.GetPlacedStructures().Count);
            Assert.AreEqual(0, _placementChanges.Count);
            await _structurePlacementFactory.DidNotReceiveWithAnyArgs().CreateStructureAsync(
                default,
                default,
                default,
                default,
                default,
                default,
                default);
        }

        [Test]
        public async Task RestoreCommittedStateAsync_InvalidFootprint_SkipsRecordAndRaisesCompletion()
        {
            _placementState.UpsertStructure(
                INSTANCE_ID,
                _structureSO,
                _gridWorldPosition,
                ROTATION_DEGREES,
                new[] { Vector3Int.zero, new Vector3Int(1, 0, 1) });
#if LOGGER_ENABLED
            LogAssert.Expect(
                LogType.Error,
                "<color=red>[GridOccupantPlacementService]</color> Cannot restore saved structure 'structure-instance' because its footprint is invalid: Occupied cell offsets do not form a filled rectangular footprint.");
#endif

            await _service.RestoreCommittedStateAsync(CancellationToken.None);

            Assert.IsTrue(_wasCommittedStateRestored);
            Assert.AreEqual(0, _service.GetPlacedStructures().Count);
            Assert.AreEqual(0, _placementChanges.Count);
        }

        [Test]
        public async Task RestoreCommittedStateAsync_EmptyFootprint_SkipsRecordAndRaisesCompletion()
        {
            _placementState.UpsertStructure(
                INSTANCE_ID,
                _structureSO,
                _gridWorldPosition,
                ROTATION_DEGREES,
                new Vector3Int[0]);
#if LOGGER_ENABLED
            LogAssert.Expect(
                LogType.Error,
                "<color=red>[GridOccupantPlacementService]</color> Cannot restore saved structure 'structure-instance' because its footprint is invalid: Occupied cell offsets are missing.");
#endif

            await _service.RestoreCommittedStateAsync(CancellationToken.None);

            Assert.IsTrue(_wasCommittedStateRestored);
            Assert.AreEqual(0, _service.GetPlacedStructures().Count);
            Assert.AreEqual(0, _placementChanges.Count);
        }

        [Test]
        public async Task RestoreCommittedStateAsync_FactoryReturnsNull_LeavesCommittedStateWithoutRuntime()
        {
            _placementState.UpsertStructure(
                INSTANCE_ID,
                _structureSO,
                _gridWorldPosition,
                ROTATION_DEGREES,
                OCCUPIED_CELL_OFFSETS);
            SetFactoryCreatedPlacement(null);

            await _service.RestoreCommittedStateAsync(CancellationToken.None);

            Assert.IsTrue(_wasCommittedStateRestored);
            Assert.IsTrue(_placementState.TryGetStructure(INSTANCE_ID, out _));
            Assert.AreEqual(0, _service.GetPlacedStructures().Count);
            Assert.AreEqual(0, _placementChanges.Count);
        }

        [Test]
        public async Task RestoreCommittedStateAsync_OccupiedPlacement_DestroysRuntimeAndContinuesCompletion()
        {
            _placementState.UpsertStructure(
                INSTANCE_ID,
                _structureSO,
                _gridWorldPosition,
                ROTATION_DEGREES,
                OCCUPIED_CELL_OFFSETS);
            GridOccupantPlacement runtimePlacement = CreateRuntimePlacement(
                INSTANCE_ID,
                _gridWorldPosition,
                ROTATION_DEGREES);
            SetFactoryCreatedPlacement(runtimePlacement);
            _gridPlacementGateway.CanOccupy(
                    null,
                    _gridWorldPosition,
                    ROTATION_DEGREES,
                    null)
                .Returns(false);
#if LOGGER_ENABLED
            LogAssert.Expect(
                LogType.Error,
                "<color=red>[GridOccupantPlacementService]</color> Cannot restore saved structure 'structure-instance' because its grid space is occupied or outside the grid.");
#endif

            await _service.RestoreCommittedStateAsync(CancellationToken.None);

            Assert.IsTrue(_wasCommittedStateRestored);
            Assert.AreEqual(0, _service.GetPlacedStructures().Count);
            Assert.AreEqual(0, _placementChanges.Count);
            _structurePlacementFactory.Received(1).DestroyStructure(runtimePlacement);
        }

        [Test]
        public async Task RestoreCommittedStateAsync_CanceledFactory_DoesNotRaiseCompletion()
        {
            _placementState.UpsertStructure(
                INSTANCE_ID,
                _structureSO,
                _gridWorldPosition,
                ROTATION_DEGREES,
                OCCUPIED_CELL_OFFSETS);
            _structurePlacementFactory.CreateStructureAsync(
                    Arg.Any<string>(),
                    Arg.Any<StructureSO>(),
                    Arg.Any<Vector3>(),
                    Arg.Any<int>(),
                    Arg.Any<CancellationToken>(),
                    Arg.Any<string>(),
                    Arg.Any<IGridOccupantPlacementProcessor>())
                .Returns(UniTask.FromException<GridOccupantPlacement>(new OperationCanceledException()));

            await _service.RestoreCommittedStateAsync(CancellationToken.None);

            Assert.IsFalse(_wasCommittedStateRestored);
            Assert.AreEqual(0, _service.GetPlacedStructures().Count);
            Assert.AreEqual(0, _placementChanges.Count);
        }

        [Test]
        public async Task RestoreCommittedStateAsync_InvalidThenValidRecord_ContinuesAndCompletesAfterPlacementEvent()
        {
            const string VALID_INSTANCE_ID = "valid-structure";
            _placementState.UpsertStructure(
                "missing-structure",
                null,
                Vector3.zero,
                0,
                OCCUPIED_CELL_OFFSETS);
            _placementState.UpsertStructure(
                VALID_INSTANCE_ID,
                _structureSO,
                _gridWorldPosition,
                ROTATION_DEGREES,
                OCCUPIED_CELL_OFFSETS);
            GridOccupantPlacement runtimePlacement = CreateRuntimePlacement(
                VALID_INSTANCE_ID,
                _gridWorldPosition,
                ROTATION_DEGREES);
            SetFactoryCreatedPlacement(runtimePlacement);

            await _service.RestoreCommittedStateAsync(CancellationToken.None);

            Assert.IsTrue(_wasCommittedStateRestored);
            Assert.IsTrue(_service.TryGetPlacement(VALID_INSTANCE_ID, out _));
            CollectionAssert.AreEqual(
                new[] { "placement:Restored", "restore-completed" },
                _eventOrder);
        }

        [Test]
        public void ClearRuntimeStructures_WithRuntimePlacement_PreservesCommittedState()
        {
            GridOccupantPlacement runtimePlacement = SeedRuntimePlacement(
                INSTANCE_ID,
                _gridWorldPosition,
                ROTATION_DEGREES);

            _service.ClearRuntimeStructures(false);

            Assert.AreEqual(0, _service.GetPlacedStructures().Count);
            Assert.IsTrue(_placementState.TryGetStructure(INSTANCE_ID, out StructureSO committedStructureSO));
            Assert.AreSame(_structureSO, committedStructureSO);
            Assert.AreEqual(0, _placementChanges.Count);
            _structurePlacementFactory.Received(1).DestroyStructure(runtimePlacement);
            _gridPlacementGateway.Received(1).RebuildOccupancyIndex(Arg.Any<IReadOnlyCollection<GridOccupantPlacement>>());
        }

        [Test]
        public void ClearRuntimeStructures_WithEventEnabled_RaisesSingleClearedEvent()
        {
            SeedRuntimePlacement(INSTANCE_ID, _gridWorldPosition, ROTATION_DEGREES);

            _service.ClearRuntimeStructures(true);
            _service.ClearRuntimeStructures(true);

            Assert.AreEqual(1, _placementChanges.Count);
            Assert.AreEqual(PlacementChangeKind.Cleared, _placementChanges[0].Kind);
            Assert.IsNull(_placementChanges[0].InstanceId);
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
            _placementState.UpsertStructure(
                instanceId,
                _structureSO,
                gridWorldPosition,
                rotationDegrees,
                OCCUPIED_CELL_OFFSETS);
            return structurePlacement;
        }

        private GridOccupantPlacement CreateRuntimePlacement(
            string instanceId,
            Vector3 gridWorldPosition,
            int rotationDegrees,
            StructureSO structureSO = null)
        {
            GameObject runtimeInstance = new(instanceId);
            _createdUnityObjects.Add(runtimeInstance);
            return new GridOccupantPlacement(
                instanceId,
                structureSO ? structureSO : _structureSO,
                runtimeInstance,
                null,
                default,
                gridWorldPosition,
                rotationDegrees);
        }

        private StructureSO CreateStructureSO()
        {
            StructureSO structureSO = ScriptableObject.CreateInstance<StructureSO>();
            _createdUnityObjects.Add(structureSO);
            return structureSO;
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
            _eventOrder.Add($"placement:{placementChange.Kind}");
        }

        private void RecordCommittedStateRestored()
        {
            _wasCommittedStateRestored = true;
            _eventOrder.Add("restore-completed");
        }

        #endregion
    }
}
