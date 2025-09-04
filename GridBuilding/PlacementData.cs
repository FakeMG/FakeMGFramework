using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.Framework.GridBuilding
{
    [Serializable]
    public class PlacementData
    {
        public string InstanceID { get; private set; }
        public Vector3Int PivotCell { get; set; }
        public readonly List<Vector3Int> OccupiedCells;

        public PlacementData(Vector3Int pivotCell, List<Vector3Int> occupiedCells, string instanceID)
        {
            PivotCell = pivotCell;
            OccupiedCells = occupiedCells;
            InstanceID = instanceID;
        }
    }
}