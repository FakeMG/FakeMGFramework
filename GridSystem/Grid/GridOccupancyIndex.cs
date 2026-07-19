using System.Collections.Generic;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.GridSystem
{
    /// <summary>
    /// Maintains a lookup from grid cells to the objects occupying them.
    /// </summary>
    internal sealed class GridOccupancyIndex
    {
        private readonly Dictionary<Vector3Int, GridOccupantData> _occupiedCells = new();

        public IReadOnlyDictionary<Vector3Int, GridOccupantData> OccupiedCells => _occupiedCells;

        #region Public Methods

        public void Rebuild(
            IReadOnlyCollection<GridOccupantPlacement> structurePlacements,
            GridManager gridManager)
        {
            _occupiedCells.Clear();

            foreach (GridOccupantPlacement structurePlacement in structurePlacements)
            {
                IReadOnlyList<Vector3Int> occupiedCells = gridManager.GetOccupiedCells(
                    structurePlacement.Footprint,
                    structurePlacement.WorldPosition,
                    structurePlacement.RotationDegrees);

                RegisterOccupiedCells(
                    structurePlacement.InstanceId,
                    gridManager.WorldToCell(structurePlacement.WorldPosition),
                    occupiedCells);
            }
        }

        public bool TryGetInstanceIdAtCell(Vector3Int cellPosition, out string instanceId)
        {
            if (_occupiedCells.TryGetValue(cellPosition, out GridOccupantData occupantData))
            {
                instanceId = occupantData.InstanceId;
                return true;
            }

            instanceId = null;
            return false;
        }

        public bool CanOccupy(
            IReadOnlyList<Vector3Int> occupiedCells,
            Vector3Int gridHalfSize,
            string ignoredInstanceId,
            bool enableLogging,
            Object logContext)
        {
            foreach (Vector3Int occupiedCell in occupiedCells)
            {
                if (!IsInsideGrid(occupiedCell, gridHalfSize))
                {
                    Echo.Log($"Grid space is outside grid at cell '{occupiedCell}'.", enableLogging, logContext);
                    return false;
                }

                if (_occupiedCells.TryGetValue(occupiedCell, out GridOccupantData occupantData) &&
                    occupantData.InstanceId != ignoredInstanceId)
                {
                    Echo.Log($"Grid space is occupied by '{occupantData.InstanceId}' at cell '{occupiedCell}'.", enableLogging, logContext);
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Private Methods

        private void RegisterOccupiedCells(
            string instanceId,
            Vector3Int pivotCell,
            IReadOnlyList<Vector3Int> occupiedCells)
        {
            GridOccupantData occupantData = new(pivotCell, occupiedCells, instanceId);
            foreach (Vector3Int occupiedCell in occupiedCells)
            {
                _occupiedCells[occupiedCell] = occupantData;
            }
        }

        private static bool IsInsideGrid(
            Vector3Int cellPosition,
            Vector3Int gridHalfSize)
        {
            return cellPosition.x >= -gridHalfSize.x && cellPosition.x <= gridHalfSize.x &&
                   cellPosition.y >= -gridHalfSize.y && cellPosition.y <= gridHalfSize.y &&
                   cellPosition.z >= -gridHalfSize.z && cellPosition.z <= gridHalfSize.z;
        }

        #endregion
    }
}
