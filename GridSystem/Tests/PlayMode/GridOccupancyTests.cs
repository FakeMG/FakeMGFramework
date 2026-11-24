using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FakeMG.GridSystem.Tests.PlayMode
{
    public sealed class GridOccupancyTests
    {
        private static readonly BoundsInt THREE_CELL_WIDE_BOUNDS = new(new Vector3Int(-1, 0, 0), new Vector3Int(3, 1, 1));
        private static readonly BoundsInt TWO_CELL_WIDE_BOUNDS = new(Vector3Int.zero, new Vector3Int(2, 1, 1));
        private static readonly BoundsInt SINGLE_CELL_BOUNDS = new(Vector3Int.zero, Vector3Int.one);
        private static readonly Vector3Int WIDE_PIVOT_CELL = new(2, 0, 4);
        private static readonly Vector3Int DISTANT_PIVOT_CELL = new(5, 0, 0);

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
        public IEnumerator RebuildOccupancyIndex_Placement_RegistersEveryFootprintCell()
        {
            return UniTask.ToCoroutine(async () =>
            {
                GridOccupantPlacement placement = await _grid.PlaceAtCellAsync(WIDE_PIVOT_CELL);

                _grid.ResizeFootprint(placement, THREE_CELL_WIDE_BOUNDS);

                Assert.AreEqual(3, _grid.GridManager.OccupiedCells.Count);
                AssertOccupant(new Vector3Int(1, 0, 4), placement.InstanceId, WIDE_PIVOT_CELL);
                AssertOccupant(new Vector3Int(2, 0, 4), placement.InstanceId, WIDE_PIVOT_CELL);
                AssertOccupant(new Vector3Int(3, 0, 4), placement.InstanceId, WIDE_PIVOT_CELL);
            });
        }

        [UnityTest]
        public IEnumerator RebuildOccupancyIndex_ReplacementPlacements_RemovesStaleCells()
        {
            return UniTask.ToCoroutine(async () =>
            {
                Vector3Int secondCell = new(5, 0, 0);
                GridOccupantPlacement first = await _grid.PlaceAtCellAsync(Vector3Int.zero);
                GridOccupantPlacement second = await _grid.PlaceAtCellAsync(secondCell);
                _grid.ResizeFootprint(first, SINGLE_CELL_BOUNDS);
                _grid.ResizeFootprint(second, SINGLE_CELL_BOUNDS);

                _grid.GridManager.RebuildOccupancyIndex(new[] { second });

                Assert.IsFalse(_grid.GridManager.OccupiedCells.ContainsKey(Vector3Int.zero));
                AssertOccupant(secondCell, second.InstanceId, secondCell);
            });
        }

        [UnityTest]
        public IEnumerator RebuildOccupancyIndex_EmptyPlacements_ClearsAllOccupiedCells()
        {
            return UniTask.ToCoroutine(async () =>
            {
                await _grid.PlaceAtCellAsync(Vector3Int.zero);

                _grid.GridManager.RebuildOccupancyIndex(Array.Empty<GridOccupantPlacement>());

                Assert.AreEqual(0, _grid.GridManager.OccupiedCells.Count);
            });
        }

        [UnityTest]
        public IEnumerator TryGetInstanceIdAtPosition_NonPivotOccupiedCell_ReturnsInstanceId()
        {
            return UniTask.ToCoroutine(async () =>
            {
                GridOccupantPlacement placement = await _grid.PlaceAtCellAsync(WIDE_PIVOT_CELL);
                _grid.ResizeFootprint(placement, THREE_CELL_WIDE_BOUNDS);

                bool hasInstance = _grid.GridManager.TryGetInstanceIdAtPosition(
                    _grid.GridManager.CellToWorld(new Vector3Int(1, 0, 4)),
                    out string instanceId);

                Assert.IsTrue(hasInstance);
                Assert.AreEqual(placement.InstanceId, instanceId);
            });
        }

        [UnityTest]
        public IEnumerator CanOccupy_OverlappingPlacement_ReturnsFalse()
        {
            return UniTask.ToCoroutine(async () =>
            {
                GridOccupantPlacement placement = await _grid.PlaceAtCellAsync(Vector3Int.zero);
                _grid.ResizeFootprint(placement, SINGLE_CELL_BOUNDS);

                bool canOccupy = _grid.GridManager.CanOccupy(placement.Footprint, placement.WorldPosition, 0);

                Assert.IsFalse(canOccupy);
            });
        }

        [UnityTest]
        public IEnumerator CanOccupy_OverlappingIgnoredInstance_ReturnsTrue()
        {
            return UniTask.ToCoroutine(async () =>
            {
                GridOccupantPlacement placement = await _grid.PlaceAtCellAsync(Vector3Int.zero);
                _grid.ResizeFootprint(placement, SINGLE_CELL_BOUNDS);

                bool canOccupy = _grid.GridManager.CanOccupy(placement.Footprint, placement.WorldPosition, 0, placement.InstanceId);

                Assert.IsTrue(canOccupy);
            });
        }

        [UnityTest]
        public IEnumerator CanOccupy_IgnoredInstanceAlsoOverlapsDifferentInstance_ReturnsFalse()
        {
            return UniTask.ToCoroutine(async () =>
            {
                GridOccupantPlacement first = await _grid.PlaceAtCellAsync(Vector3Int.zero);
                _grid.ResizeFootprint(first, SINGLE_CELL_BOUNDS);
                // Placed clear of the first, then its footprint is pointed at the cell right beside it.
                GridOccupantPlacement second = await _grid.PlaceAtCellAsync(DISTANT_PIVOT_CELL);
                _grid.ResizeFootprint(second, new BoundsInt(Vector3Int.right - DISTANT_PIVOT_CELL, Vector3Int.one));
                first.Footprint.OverrideCellBounds(TWO_CELL_WIDE_BOUNDS);

                bool canOccupy = _grid.GridManager.CanOccupy(first.Footprint, first.WorldPosition, 0, first.InstanceId);

                Assert.IsFalse(canOccupy);
            });
        }

        #endregion

        #region Private Methods

        private async UniTask CreateFixtureAsync()
        {
            _grid = await InstalledGridFixture.CreateAsync();
        }

        private void AssertOccupant(Vector3Int occupiedCell, string expectedInstanceId, Vector3Int expectedPivotCell)
        {
            Assert.IsTrue(_grid.GridManager.OccupiedCells.TryGetValue(occupiedCell, out GridOccupantData occupantData));
            Assert.AreEqual(expectedInstanceId, occupantData.InstanceId);
            Assert.AreEqual(expectedPivotCell, occupantData.PivotCell);
        }

        #endregion
    }
}
