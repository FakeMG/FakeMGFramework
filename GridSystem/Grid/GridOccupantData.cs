using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.GridSystem
{
    /// <summary>
    /// Describes an object occupying one or more grid cells.
    /// </summary>
    [Serializable]
    public sealed class GridOccupantData
    {
        private readonly List<Vector3Int> _occupiedCells;

        public GridOccupantData(
            Vector3Int pivotCell,
            IReadOnlyList<Vector3Int> occupiedCells,
            string instanceId)
        {
            PivotCell = pivotCell;
            _occupiedCells = new List<Vector3Int>(occupiedCells);
            InstanceId = instanceId;
        }

        public string InstanceId { get; }
        public Vector3Int PivotCell { get; }
        public IReadOnlyList<Vector3Int> OccupiedCells => _occupiedCells;
    }
}
