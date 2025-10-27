using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.Framework.GridBuilding
{
    [Serializable]
    public class PlacementData
    {
        public string InstanceID;
        public Vector3Int PivotCell;
        public List<Vector3Int> OccupiedCells;

        public PlacementData(Vector3Int pivotCell, List<Vector3Int> occupiedCells, string instanceID)
        {
            PivotCell = pivotCell;
            OccupiedCells = occupiedCells;
            InstanceID = instanceID;
        }
    }
}