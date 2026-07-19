using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.TestTools;
using VContainer;
using VContainer.Unity;

namespace FakeMG.GridSystem.Tests.PlayMode
{
    /// <summary>
    /// Locks GridManager's public coordinate, bounds, footprint, and occupancy behavior against production prefabs.
    /// </summary>
    public sealed class GridManagerTests
    {
        private const string CONFIG_RESOURCE_NAME = "GridSystemTestAssetConfig";
        private const string FIRST_INSTANCE_ID = "structure-a";
        private const string SECOND_INSTANCE_ID = "structure-b";

        private IObjectResolver _container;
        private AsyncOperationHandle<GameObject> _gridManagerPrefabHandle;
        private AsyncOperationHandle<GameObject> _structureFootprintPrefabHandle;
        private GridManager _gridManager;
        private GridFootprint _structureFootprint;

        #region Unity Lifecycle

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            GridSystemTestAssetConfigSO testAssetConfig =
                Resources.Load<GridSystemTestAssetConfigSO>(CONFIG_RESOURCE_NAME);
            Assert.IsNotNull(testAssetConfig, $"Missing Resources/{CONFIG_RESOURCE_NAME} asset.");

            _gridManagerPrefabHandle = Addressables.LoadAssetAsync<GameObject>(testAssetConfig.GridManagerPrefab);
            _structureFootprintPrefabHandle =
                Addressables.LoadAssetAsync<GameObject>(testAssetConfig.GridFootprintPrefab);

            yield return _gridManagerPrefabHandle;
            yield return _structureFootprintPrefabHandle;

            Assert.AreEqual(AsyncOperationStatus.Succeeded, _gridManagerPrefabHandle.Status);
            Assert.AreEqual(AsyncOperationStatus.Succeeded, _structureFootprintPrefabHandle.Status);

            GridManager gridManagerPrefab = _gridManagerPrefabHandle.Result.GetComponent<GridManager>();
            GridFootprint structureFootprintPrefab =
                _structureFootprintPrefabHandle.Result.GetComponent<GridFootprint>();
            Assert.IsNotNull(gridManagerPrefab);
            Assert.IsNotNull(structureFootprintPrefab);

            ContainerBuilder builder = new();
            builder.RegisterComponentInNewPrefab(gridManagerPrefab, Lifetime.Scoped);
            builder.RegisterComponentInNewPrefab(structureFootprintPrefab, Lifetime.Scoped);
            _container = builder.Build();

            _gridManager = _container.Resolve<GridManager>();
            _structureFootprint = _container.Resolve<GridFootprint>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _container?.Dispose();

            if (_gridManager)
            {
                Object.Destroy(_gridManager.gameObject);
            }

            if (_structureFootprint)
            {
                Object.Destroy(_structureFootprint.gameObject);
            }

            if (_gridManagerPrefabHandle.IsValid())
            {
                Addressables.Release(_gridManagerPrefabHandle);
            }

            if (_structureFootprintPrefabHandle.IsValid())
            {
                Addressables.Release(_structureFootprintPrefabHandle);
            }

            yield return null;
        }

        #endregion

        #region Public Methods

        [TestCase(3.9f, 0.1f, 2.1f, 3, 0, 2)]
        [TestCase(-0.1f, -0.1f, -0.1f, -1, -1, -1)]
        [TestCase(1f, 1f, 1f, 1, 1, 1)]
        public void WorldToCell_Position_ReturnsContainingCell(
            float worldX,
            float worldY,
            float worldZ,
            int expectedCellX,
            int expectedCellY,
            int expectedCellZ)
        {
            Vector3 worldPosition = new(worldX, worldY, worldZ);

            Vector3Int cellPosition = _gridManager.WorldToCell(worldPosition);

            Assert.AreEqual(new Vector3Int(expectedCellX, expectedCellY, expectedCellZ), cellPosition);
        }

        [Test]
        public void CellToWorld_Cell_ReturnsBottomCenterPosition()
        {
            Vector3 worldPosition = _gridManager.CellToWorld(new Vector3Int(2, 3, -4));

            Assert.AreEqual(new Vector3(2.5f, 3f, -3.5f), worldPosition);
        }

        [Test]
        public void WorldToGridWorld_UnsnappedPosition_ReturnsContainingCellBottomCenter()
        {
            Vector3 gridWorldPosition = _gridManager.WorldToGridWorld(new Vector3(2.9f, 0.8f, -0.1f));

            Assert.AreEqual(new Vector3(2.5f, 0f, -0.5f), gridWorldPosition);
        }

        [Test]
        public void GetWorldBoundsMeters_ConfiguredGrid_ReturnsInclusiveCellBounds()
        {
            Bounds worldBoundsMeters = _gridManager.GetWorldBoundsMeters();
            GetBoundaryCells(worldBoundsMeters, out Vector3Int minCell, out Vector3Int maxCell);
            Vector3 expectedSizeMeters = new(
                (maxCell.x - minCell.x + 1) * _gridManager.CellSizeMeters,
                (maxCell.y - minCell.y + 1) * _gridManager.CellSizeMeters,
                (maxCell.z - minCell.z + 1) * _gridManager.CellSizeMeters);

            Assert.AreEqual(_gridManager.CellToWorld(Vector3Int.zero), worldBoundsMeters.center);
            Assert.AreEqual(expectedSizeMeters, worldBoundsMeters.size);
        }

        [Test]
        public void GetScaleMeters_NinetyDegreeRotation_SwapsHorizontalAxes()
        {
            OverrideFootprintBounds(
                new Vector3Int(-1, 0, -2),
                new Vector3Int(3, 1, 5));

            Vector3 unrotatedScaleMeters = _structureFootprint.GetScaleMeters(_gridManager.CellSizeMeters, 0);
            Vector3 rotatedScaleMeters = _structureFootprint.GetScaleMeters(_gridManager.CellSizeMeters, 90);

            Assert.AreEqual(new Vector3(3f, 1f, 5f), unrotatedScaleMeters);
            Assert.AreEqual(new Vector3(5f, 1f, 3f), rotatedScaleMeters);
        }

        [Test]
        public void GetOccupiedCells_Position_ReturnsCellsAroundSnappedPivot()
        {
            OverrideFootprintBounds(
                new Vector3Int(-1, 0, 0),
                new Vector3Int(3, 1, 1));
            IReadOnlyList<Vector3Int> expectedCells = new[]
            {
                new Vector3Int(1, 0, 4),
                new Vector3Int(2, 0, 4),
                new Vector3Int(3, 0, 4)
            };

            IReadOnlyList<Vector3Int> occupiedCells = _gridManager.GetOccupiedCells(
                _structureFootprint,
                new Vector3(2.4f, 0.1f, 4.2f),
                0);

            CollectionAssert.AreEquivalent(expectedCells, occupiedCells);
        }

        [Test]
        public void RebuildOccupancyIndex_Placement_RegistersEveryFootprintCell()
        {
            OverrideFootprintBounds(
                new Vector3Int(-1, 0, 0),
                new Vector3Int(3, 1, 1));
            GridOccupantPlacement structurePlacement = CreatePlacement(FIRST_INSTANCE_ID, new Vector3Int(2, 0, 4));

            _gridManager.RebuildOccupancyIndex(new[] { structurePlacement });

            Assert.AreEqual(3, _gridManager.OccupiedCells.Count);
            AssertPlacementData(new Vector3Int(1, 0, 4), FIRST_INSTANCE_ID, new Vector3Int(2, 0, 4));
            AssertPlacementData(new Vector3Int(2, 0, 4), FIRST_INSTANCE_ID, new Vector3Int(2, 0, 4));
            AssertPlacementData(new Vector3Int(3, 0, 4), FIRST_INSTANCE_ID, new Vector3Int(2, 0, 4));
        }

        [Test]
        public void RebuildOccupancyIndex_ReplacementPlacements_RemovesStaleCells()
        {
            OverrideFootprintBounds(Vector3Int.zero, Vector3Int.one);
            GridOccupantPlacement firstPlacement = CreatePlacement(FIRST_INSTANCE_ID, Vector3Int.zero);
            GridOccupantPlacement secondPlacement = CreatePlacement(SECOND_INSTANCE_ID, new Vector3Int(5, 0, 0));
            _gridManager.RebuildOccupancyIndex(new[] { firstPlacement });

            _gridManager.RebuildOccupancyIndex(new[] { secondPlacement });

            Assert.IsFalse(_gridManager.OccupiedCells.ContainsKey(Vector3Int.zero));
            AssertPlacementData(new Vector3Int(5, 0, 0), SECOND_INSTANCE_ID, new Vector3Int(5, 0, 0));
        }

        [Test]
        public void TryGetInstanceIdAtPosition_NonPivotOccupiedCell_ReturnsInstanceId()
        {
            OverrideFootprintBounds(
                new Vector3Int(-1, 0, 0),
                new Vector3Int(3, 1, 1));
            _gridManager.RebuildOccupancyIndex(new[]
            {
                CreatePlacement(FIRST_INSTANCE_ID, new Vector3Int(2, 0, 4))
            });

            bool hasInstance = _gridManager.TryGetInstanceIdAtPosition(
                _gridManager.CellToWorld(new Vector3Int(1, 0, 4)),
                out string instanceId);

            Assert.IsTrue(hasInstance);
            Assert.AreEqual(FIRST_INSTANCE_ID, instanceId);
        }

        [Test]
        public void TryGetInstanceIdAtPosition_EmptyCell_ReturnsFalseAndNull()
        {
            bool hasInstance = _gridManager.TryGetInstanceIdAtPosition(Vector3.zero, out string instanceId);

            Assert.IsFalse(hasInstance);
            Assert.IsNull(instanceId);
        }

        [Test]
        public void CanOccupy_EmptyInBoundsCell_ReturnsTrue()
        {
            OverrideFootprintBounds(Vector3Int.zero, Vector3Int.one);

            bool canOccupy = _gridManager.CanOccupy(_structureFootprint, Vector3.zero, 0);

            Assert.IsTrue(canOccupy);
        }

        [Test]
        public void CanOccupy_OverlappingPlacement_ReturnsFalse()
        {
            OverrideFootprintBounds(Vector3Int.zero, Vector3Int.one);
            _gridManager.RebuildOccupancyIndex(new[]
            {
                CreatePlacement(FIRST_INSTANCE_ID, Vector3Int.zero)
            });

            bool canOccupy = _gridManager.CanOccupy(_structureFootprint, Vector3.zero, 0);

            Assert.IsFalse(canOccupy);
        }

        [Test]
        public void CanOccupy_OverlappingIgnoredInstance_ReturnsTrue()
        {
            OverrideFootprintBounds(Vector3Int.zero, Vector3Int.one);
            _gridManager.RebuildOccupancyIndex(new[]
            {
                CreatePlacement(FIRST_INSTANCE_ID, Vector3Int.zero)
            });

            bool canOccupy = _gridManager.CanOccupy(
                _structureFootprint,
                Vector3.zero,
                0,
                FIRST_INSTANCE_ID);

            Assert.IsTrue(canOccupy);
        }

        [Test]
        public void CanOccupy_CellsOnPositiveAndNegativeBounds_ReturnsTrue()
        {
            OverrideFootprintBounds(Vector3Int.zero, Vector3Int.one);
            GetBoundaryCells(_gridManager.GetWorldBoundsMeters(), out Vector3Int minCell, out Vector3Int maxCell);

            bool canOccupyPositiveBoundary = _gridManager.CanOccupy(
                _structureFootprint,
                _gridManager.CellToWorld(maxCell),
                0);
            bool canOccupyNegativeBoundary = _gridManager.CanOccupy(
                _structureFootprint,
                _gridManager.CellToWorld(minCell),
                0);

            Assert.IsTrue(canOccupyPositiveBoundary);
            Assert.IsTrue(canOccupyNegativeBoundary);
        }

        [Test]
        public void CanOccupy_CellsBeyondPositiveAndNegativeBounds_ReturnsFalse()
        {
            OverrideFootprintBounds(Vector3Int.zero, Vector3Int.one);
            GetBoundaryCells(_gridManager.GetWorldBoundsMeters(), out Vector3Int minCell, out Vector3Int maxCell);
            Vector3Int beyondPositiveBoundaryCell = maxCell + Vector3Int.right;
            Vector3Int beyondNegativeBoundaryCell = minCell + Vector3Int.left;

            bool canOccupyBeyondPositiveBoundary = _gridManager.CanOccupy(
                _structureFootprint,
                _gridManager.CellToWorld(beyondPositiveBoundaryCell),
                0);
            bool canOccupyBeyondNegativeBoundary = _gridManager.CanOccupy(
                _structureFootprint,
                _gridManager.CellToWorld(beyondNegativeBoundaryCell),
                0);

            Assert.IsFalse(canOccupyBeyondPositiveBoundary);
            Assert.IsFalse(canOccupyBeyondNegativeBoundary);
        }

        #endregion

        #region Private Methods

        private void OverrideFootprintBounds(
            Vector3Int minimumCellOffset,
            Vector3Int cellSizeCells)
        {
            _structureFootprint.OverrideCellBounds(new BoundsInt(minimumCellOffset, cellSizeCells));
        }

        private GridOccupantPlacement CreatePlacement(string instanceId, Vector3Int pivotCell)
        {
            Vector3 worldPosition = _gridManager.CellToWorld(pivotCell);
            return new GridOccupantPlacement(
                instanceId,
                null,
                _structureFootprint.gameObject,
                _structureFootprint,
                default,
                worldPosition,
                0);
        }

        private void AssertPlacementData(
            Vector3Int occupiedCell,
            string expectedInstanceId,
            Vector3Int expectedPivotCell)
        {
            Assert.IsTrue(_gridManager.OccupiedCells.TryGetValue(occupiedCell, out GridOccupantData occupantData));
            Assert.AreEqual(expectedInstanceId, occupantData.InstanceId);
            Assert.AreEqual(expectedPivotCell, occupantData.PivotCell);
        }

        private void GetBoundaryCells(
            Bounds worldBoundsMeters,
            out Vector3Int minCell,
            out Vector3Int maxCell)
        {
            Vector3 halfCellSizeMeters = Vector3.one * (_gridManager.CellSizeMeters * 0.5f);
            minCell = _gridManager.WorldToCell(worldBoundsMeters.min + halfCellSizeMeters);
            maxCell = _gridManager.WorldToCell(worldBoundsMeters.max - halfCellSizeMeters);
        }

        #endregion
    }
}
