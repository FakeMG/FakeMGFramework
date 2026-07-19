using NUnit.Framework;
using UnityEngine;

namespace FakeMG.GridSystem.Tests.EditMode
{
    /// <summary>
    /// Locks the committed placement-state contract used by runtime placement and save/load.
    /// </summary>
    public sealed class PlacementStateTests
    {
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

            placementState.UpsertStructure("instance-a", _structureSO, worldPosition, 90);

            Assert.AreEqual(1, placementState.Structures.Count);
            Assert.AreEqual("instance-a", placementState.Structures[0].InstanceId);
            Assert.AreEqual(_structureSO, placementState.Structures[0].StructureSO);
            Assert.AreEqual(worldPosition, placementState.Structures[0].WorldPosition);
            Assert.AreEqual(90, placementState.Structures[0].RotationDegrees);
        }

        [Test]
        public void UpsertStructure_ExistingInstance_ReplacesPlacementValues()
        {
            PlacementState placementState = new();
            _structureSO = ScriptableObject.CreateInstance<StructureSO>();
            Vector3 replacementWorldPosition = new(4f, 0f, 5f);

            placementState.UpsertStructure("instance-a", null, Vector3.zero, 0);
            placementState.UpsertStructure("instance-a", _structureSO, replacementWorldPosition, 180);

            Assert.AreEqual(1, placementState.Structures.Count);
            Assert.AreEqual(_structureSO, placementState.Structures[0].StructureSO);
            Assert.AreEqual(replacementWorldPosition, placementState.Structures[0].WorldPosition);
            Assert.AreEqual(180, placementState.Structures[0].RotationDegrees);
        }

        [Test]
        public void TryGetStructure_MissingInstance_ReturnsFalseAndNull()
        {
            PlacementState placementState = new();

            bool hasStructure = placementState.TryGetStructure("missing-instance", out StructureSO structureSO);

            Assert.IsFalse(hasStructure);
            Assert.IsNull(structureSO);
        }

        [Test]
        public void RemoveStructure_ExistingInstance_RemovesOnlyMatchingPlacement()
        {
            PlacementState placementState = new();
            _structureSO = ScriptableObject.CreateInstance<StructureSO>();

            placementState.UpsertStructure("instance-a", _structureSO, Vector3.zero, 0);
            placementState.UpsertStructure("instance-b", _structureSO, Vector3.one, 90);

            placementState.RemoveStructure("instance-a");

            Assert.AreEqual(1, placementState.Structures.Count);
            Assert.AreEqual("instance-b", placementState.Structures[0].InstanceId);
        }

        [Test]
        public void Clear_WithPlacements_RemovesAllPlacements()
        {
            PlacementState placementState = new();
            _structureSO = ScriptableObject.CreateInstance<StructureSO>();
            placementState.UpsertStructure("instance-a", _structureSO, Vector3.zero, 0);
            placementState.UpsertStructure("instance-b", _structureSO, Vector3.one, 90);

            placementState.Clear();

            Assert.AreEqual(0, placementState.Structures.Count);
        }

        [Test]
        public void Clone_SourceMutatesAfterClone_CloneKeepsOriginalValues()
        {
            PlacementState sourceState = new();
            _structureSO = ScriptableObject.CreateInstance<StructureSO>();
            Vector3 originalWorldPosition = new(1f, 0f, 1f);
            sourceState.UpsertStructure("instance-a", _structureSO, originalWorldPosition, 90);

            PlacementState clonedState = sourceState.Clone();
            sourceState.UpsertStructure("instance-a", _structureSO, new Vector3(9f, 0f, 9f), 270);

            Assert.AreEqual(1, clonedState.Structures.Count);
            Assert.AreEqual(originalWorldPosition, clonedState.Structures[0].WorldPosition);
            Assert.AreEqual(90, clonedState.Structures[0].RotationDegrees);
        }

        [Test]
        public void ReplaceWith_SourceMutatesAfterReplace_TargetKeepsClonedValues()
        {
            PlacementState sourceState = new();
            PlacementState targetState = new();
            _structureSO = ScriptableObject.CreateInstance<StructureSO>();
            Vector3 originalWorldPosition = new(2f, 0f, 2f);
            sourceState.UpsertStructure("instance-a", _structureSO, originalWorldPosition, 90);
            targetState.UpsertStructure("stale-instance", null, Vector3.zero, 0);

            targetState.ReplaceWith(sourceState);
            sourceState.UpsertStructure("instance-a", _structureSO, new Vector3(8f, 0f, 8f), 180);

            Assert.AreEqual(1, targetState.Structures.Count);
            Assert.AreEqual("instance-a", targetState.Structures[0].InstanceId);
            Assert.AreEqual(originalWorldPosition, targetState.Structures[0].WorldPosition);
            Assert.AreEqual(90, targetState.Structures[0].RotationDegrees);
        }

        #endregion
    }
}
