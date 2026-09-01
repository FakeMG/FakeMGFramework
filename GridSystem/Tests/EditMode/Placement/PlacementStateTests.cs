using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FakeMG.GridSystem.Tests.EditMode
{
    /// <summary>
    /// Locks the committed placement-state contract used by runtime placement and save/load.
    /// </summary>
    public sealed class PlacementStateTests
    {
        private const string FIRST_INSTANCE_ID = "instance-a";
        private const string SECOND_INSTANCE_ID = "instance-b";
        private const string MISSING_INSTANCE_ID = "missing-instance";
        private const int ROTATION_DEGREES = 90;
        private const int REPLACEMENT_ROTATION_DEGREES = 180;

        private static readonly IReadOnlyList<Vector3Int> OCCUPIED_CELL_OFFSETS =
            new List<Vector3Int>
            {
                new(-1, 0, 0),
                new(0, 0, 0),
            };

        private StructureSO _structureSO;

        #region Public Methods

        [TearDown]
        public void TearDown()
        {
            if (_structureSO)
            {
                Object.DestroyImmediate(_structureSO);
            }
        }

        [Test]
        public void UpsertStructure_NewInstance_AddsPlacement()
        {
            PlacementState placementState = new();
            _structureSO = ScriptableObject.CreateInstance<StructureSO>();
            Vector3 worldPosition = new(1f, 0f, 2f);

            placementState.UpsertStructure(
                FIRST_INSTANCE_ID,
                _structureSO,
                worldPosition,
                ROTATION_DEGREES,
                OCCUPIED_CELL_OFFSETS);

            Assert.AreEqual(1, placementState.Structures.Count);
            Assert.AreEqual(FIRST_INSTANCE_ID, placementState.Structures[0].InstanceId);
            Assert.AreEqual(_structureSO, placementState.Structures[0].StructureSO);
            Assert.AreEqual(worldPosition, placementState.Structures[0].WorldPosition);
            Assert.AreEqual(ROTATION_DEGREES, placementState.Structures[0].RotationDegrees);
            CollectionAssert.AreEqual(
                OCCUPIED_CELL_OFFSETS,
                placementState.Structures[0].OccupiedCellOffsets);
        }

        [Test]
        public void UpsertStructure_ExistingInstance_ReplacesPlacementValues()
        {
            PlacementState placementState = new();
            _structureSO = ScriptableObject.CreateInstance<StructureSO>();
            Vector3 replacementWorldPosition = new(4f, 0f, 5f);

            placementState.UpsertStructure(
                FIRST_INSTANCE_ID,
                null,
                Vector3.zero,
                0,
                OCCUPIED_CELL_OFFSETS);
            placementState.UpsertStructure(
                FIRST_INSTANCE_ID,
                _structureSO,
                replacementWorldPosition,
                REPLACEMENT_ROTATION_DEGREES,
                OCCUPIED_CELL_OFFSETS);

            Assert.AreEqual(1, placementState.Structures.Count);
            Assert.AreEqual(_structureSO, placementState.Structures[0].StructureSO);
            Assert.AreEqual(replacementWorldPosition, placementState.Structures[0].WorldPosition);
            Assert.AreEqual(REPLACEMENT_ROTATION_DEGREES, placementState.Structures[0].RotationDegrees);
        }

        [Test]
        public void TryGetStructure_MissingInstance_ReturnsFalseAndNull()
        {
            PlacementState placementState = new();

            bool hasStructure = placementState.TryGetStructure(MISSING_INSTANCE_ID, out StructureSO structureSO);

            Assert.IsFalse(hasStructure);
            Assert.IsNull(structureSO);
        }

        [Test]
        public void TryGetStructure_ExistingInstance_ReturnsStoredStructure()
        {
            PlacementState placementState = new();
            _structureSO = ScriptableObject.CreateInstance<StructureSO>();
            placementState.UpsertStructure(
                FIRST_INSTANCE_ID,
                _structureSO,
                Vector3.zero,
                0,
                OCCUPIED_CELL_OFFSETS);

            bool wasFound = placementState.TryGetStructure(FIRST_INSTANCE_ID, out StructureSO structureSO);

            Assert.IsTrue(wasFound);
            Assert.AreSame(_structureSO, structureSO);
        }

        [Test]
        public void RemoveStructure_ExistingInstance_RemovesOnlyMatchingPlacement()
        {
            PlacementState placementState = new();
            _structureSO = ScriptableObject.CreateInstance<StructureSO>();

            placementState.UpsertStructure(FIRST_INSTANCE_ID, _structureSO, Vector3.zero, 0, OCCUPIED_CELL_OFFSETS);
            placementState.UpsertStructure(SECOND_INSTANCE_ID, _structureSO, Vector3.one, ROTATION_DEGREES, OCCUPIED_CELL_OFFSETS);

            placementState.RemoveStructure(FIRST_INSTANCE_ID);

            Assert.AreEqual(1, placementState.Structures.Count);
            Assert.AreEqual(SECOND_INSTANCE_ID, placementState.Structures[0].InstanceId);
        }

        [Test]
        public void RemoveStructure_MissingInstance_PreservesPlacements()
        {
            PlacementState placementState = new();
            _structureSO = ScriptableObject.CreateInstance<StructureSO>();
            placementState.UpsertStructure(
                FIRST_INSTANCE_ID,
                _structureSO,
                Vector3.zero,
                0,
                OCCUPIED_CELL_OFFSETS);

            placementState.RemoveStructure(MISSING_INSTANCE_ID);

            Assert.AreEqual(1, placementState.Structures.Count);
            Assert.AreEqual(FIRST_INSTANCE_ID, placementState.Structures[0].InstanceId);
        }

        [Test]
        public void Clear_WithPlacements_RemovesAllPlacements()
        {
            PlacementState placementState = new();
            _structureSO = ScriptableObject.CreateInstance<StructureSO>();
            placementState.UpsertStructure(FIRST_INSTANCE_ID, _structureSO, Vector3.zero, 0, OCCUPIED_CELL_OFFSETS);
            placementState.UpsertStructure(SECOND_INSTANCE_ID, _structureSO, Vector3.one, ROTATION_DEGREES, OCCUPIED_CELL_OFFSETS);

            placementState.Clear();

            Assert.AreEqual(0, placementState.Structures.Count);
        }

        [Test]
        public void Clone_SourceMutatesAfterClone_CloneKeepsOriginalValues()
        {
            PlacementState sourceState = new();
            _structureSO = ScriptableObject.CreateInstance<StructureSO>();
            Vector3 originalWorldPosition = new(1f, 0f, 1f);
            sourceState.UpsertStructure(
                FIRST_INSTANCE_ID,
                _structureSO,
                originalWorldPosition,
                ROTATION_DEGREES,
                OCCUPIED_CELL_OFFSETS);

            PlacementState clonedState = sourceState.Clone();
            sourceState.UpsertStructure(
                FIRST_INSTANCE_ID,
                _structureSO,
                new Vector3(9f, 0f, 9f),
                270,
                OCCUPIED_CELL_OFFSETS);

            Assert.AreEqual(1, clonedState.Structures.Count);
            Assert.AreEqual(originalWorldPosition, clonedState.Structures[0].WorldPosition);
            Assert.AreEqual(ROTATION_DEGREES, clonedState.Structures[0].RotationDegrees);
            CollectionAssert.AreEqual(
                OCCUPIED_CELL_OFFSETS,
                clonedState.Structures[0].OccupiedCellOffsets);
        }

        [Test]
        public void ReplaceWith_SourceMutatesAfterReplace_TargetKeepsClonedValues()
        {
            PlacementState sourceState = new();
            PlacementState targetState = new();
            _structureSO = ScriptableObject.CreateInstance<StructureSO>();
            Vector3 originalWorldPosition = new(2f, 0f, 2f);
            sourceState.UpsertStructure(
                FIRST_INSTANCE_ID,
                _structureSO,
                originalWorldPosition,
                ROTATION_DEGREES,
                OCCUPIED_CELL_OFFSETS);
            targetState.UpsertStructure(
                "stale-instance",
                null,
                Vector3.zero,
                0,
                OCCUPIED_CELL_OFFSETS);

            targetState.ReplaceWith(sourceState);
            sourceState.UpsertStructure(
                FIRST_INSTANCE_ID,
                _structureSO,
                new Vector3(8f, 0f, 8f),
                REPLACEMENT_ROTATION_DEGREES,
                OCCUPIED_CELL_OFFSETS);

            Assert.AreEqual(1, targetState.Structures.Count);
            Assert.AreEqual(FIRST_INSTANCE_ID, targetState.Structures[0].InstanceId);
            Assert.AreEqual(originalWorldPosition, targetState.Structures[0].WorldPosition);
            Assert.AreEqual(ROTATION_DEGREES, targetState.Structures[0].RotationDegrees);
            CollectionAssert.AreEqual(
                OCCUPIED_CELL_OFFSETS,
                targetState.Structures[0].OccupiedCellOffsets);
        }

        [Test]
        public void UpsertStructure_SourceOffsetsMutate_StateKeepsCopiedValues()
        {
            PlacementState placementState = new();
            _structureSO = ScriptableObject.CreateInstance<StructureSO>();
            List<Vector3Int> sourceCellOffsets = new(OCCUPIED_CELL_OFFSETS);

            placementState.UpsertStructure(
                FIRST_INSTANCE_ID,
                _structureSO,
                Vector3.zero,
                0,
                sourceCellOffsets);
            sourceCellOffsets.Clear();

            CollectionAssert.AreEqual(
                OCCUPIED_CELL_OFFSETS,
                placementState.Structures[0].OccupiedCellOffsets);
        }

        [Test]
        public void UpsertStructure_NullOffsets_StoresEmptyFootprint()
        {
            PlacementState placementState = new();
            _structureSO = ScriptableObject.CreateInstance<StructureSO>();
#if LOGGER_ENABLED
            LogAssert.Expect(
                LogType.Error,
                $"<color=red>[PlacementState]</color> Cannot store footprint for placement '{FIRST_INSTANCE_ID}' because occupied cell offsets are missing.");
#endif

            placementState.UpsertStructure(
                FIRST_INSTANCE_ID,
                _structureSO,
                Vector3.zero,
                0,
                null);

            Assert.AreEqual(1, placementState.Structures.Count);
            Assert.AreEqual(0, placementState.Structures[0].OccupiedCellOffsets.Count);
        }

        [Test]
        public void UpsertStructure_ExistingInstanceSourceOffsetsMutate_StateKeepsCopiedReplacement()
        {
            PlacementState placementState = new();
            _structureSO = ScriptableObject.CreateInstance<StructureSO>();
            List<Vector3Int> replacementCellOffsets = new()
            {
                new(0, 0, 0),
                new(0, 0, 1),
            };
            placementState.UpsertStructure(
                FIRST_INSTANCE_ID,
                _structureSO,
                Vector3.zero,
                0,
                OCCUPIED_CELL_OFFSETS);

            placementState.UpsertStructure(
                FIRST_INSTANCE_ID,
                _structureSO,
                Vector3.one,
                ROTATION_DEGREES,
                replacementCellOffsets);
            replacementCellOffsets.Clear();

            CollectionAssert.AreEqual(
                new[] { new Vector3Int(0, 0, 0), new Vector3Int(0, 0, 1) },
                placementState.Structures[0].OccupiedCellOffsets);
        }

        #endregion
    }
}
