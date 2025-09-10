using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.Framework.GridBuilding
{
    [Serializable]
    public class PlacementData
    {
        public string instanceID;
        public Vector3Int pivotCell;
        public List<Vector3Int> occupiedCells;

        public PlacementData(Vector3Int pivotCell, List<Vector3Int> occupiedCells, string instanceID)
        {
            this.pivotCell = pivotCell;
            this.occupiedCells = occupiedCells;
            this.instanceID = instanceID;
        }
    }
}