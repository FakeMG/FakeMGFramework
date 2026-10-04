using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.GridSystem
{
    /// <summary>
    /// Draws editor gizmos for grid bounds and occupancy without owning grid state.
    /// </summary>
    internal sealed class GridGizmoDrawer
    {
        private readonly GridManager _gridManager;

        public GridGizmoDrawer(GridManager gridManager)
        {
            _gridManager = gridManager;
        }

        #region Public Methods

        public void Draw(
            bool shouldDrawEmptyCells,
            Vector3Int gridHalfSize,
            IReadOnlyDictionary<Vector3Int, GridOccupantData> occupiedCells,
            Color gridBoundsColor,
            Color occupiedCellColor,
            Color pivotCellColor,
            Color emptyCellColor)
        {
            DrawGridBounds(gridHalfSize, gridBoundsColor);
            DrawEmptyCells(shouldDrawEmptyCells, gridHalfSize, occupiedCells, emptyCellColor);
            DrawOccupiedCells(occupiedCells, occupiedCellColor, pivotCellColor);
        }

        #endregion

        #region Private Methods

        private void DrawGridBounds(Vector3Int gridHalfSize, Color gridBoundsColor)
        {
            Vector3Int minCell = new(-gridHalfSize.x, -gridHalfSize.y, -gridHalfSize.z);
            Vector3Int maxCell = new(gridHalfSize.x, gridHalfSize.y, gridHalfSize.z);

            Vector3 minCenter = GetCellCenter(minCell);
            Vector3 maxCenter = GetCellCenter(maxCell);
            Vector3 boundsCenter = (minCenter + maxCenter) * 0.5f;

            Vector3 boundsSize = new(
                (gridHalfSize.x * 2 + 1) * _gridManager.CellSizeMeters,
                (gridHalfSize.y * 2 + 1) * _gridManager.CellSizeMeters,
                (gridHalfSize.z * 2 + 1) * _gridManager.CellSizeMeters);

            Gizmos.color = gridBoundsColor;
            Gizmos.DrawWireCube(boundsCenter, boundsSize);
        }

        private void DrawEmptyCells(
            bool shouldDrawEmptyCells,
            Vector3Int gridHalfSize,
            IReadOnlyDictionary<Vector3Int, GridOccupantData> occupiedCells,
            Color emptyCellColor)
        {
            if (!shouldDrawEmptyCells)
            {
                return;
            }

            Vector3Int minCell = new(-gridHalfSize.x, -gridHalfSize.y, -gridHalfSize.z);
            Vector3Int maxCell = new(gridHalfSize.x, gridHalfSize.y, gridHalfSize.z);

            for (int x = minCell.x; x <= maxCell.x; x++)
            {
                for (int y = minCell.y; y <= maxCell.y; y++)
                {
                    for (int z = minCell.z; z <= maxCell.z; z++)
                    {
                        Vector3Int cell = new(x, y, z);
                        if (occupiedCells.ContainsKey(cell))
                        {
                            continue;
                        }

                        Gizmos.color = emptyCellColor;
                        Gizmos.DrawWireCube(GetCellCenter(cell), Vector3.one * _gridManager.CellSizeMeters);
                    }
                }
            }
        }

        private void DrawOccupiedCells(
            IReadOnlyDictionary<Vector3Int, GridOccupantData> occupiedCells,
            Color occupiedCellColor,
            Color pivotCellColor)
        {
            foreach (KeyValuePair<Vector3Int, GridOccupantData> kvp in occupiedCells)
            {
                Vector3Int cell = kvp.Key;
                GridOccupantData occupantData = kvp.Value;
                bool isPivotCell = cell == occupantData.PivotCell;

                Gizmos.color = isPivotCell ? pivotCellColor : occupiedCellColor;
                Gizmos.DrawCube(GetCellCenter(cell), Vector3.one * _gridManager.CellSizeMeters);
            }
        }

        private Vector3 GetCellCenter(Vector3Int cell)
        {
            Vector3 center = _gridManager.CellToWorld(cell);
            center += Vector3.up * (_gridManager.CellSizeMeters * 0.5f);
            return center;
        }

        #endregion
    }
}
