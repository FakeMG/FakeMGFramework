using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.FakeMGFramework.GridBuilding
{
    /// <summary>
    /// The Grid system only cares about which cells are occupied
    /// and keep a reference to the occupying block via PlacementData.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        [SerializeField] private Grid grid;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Vector3Int gridHalfSize = new(10, 10, 10);
        private Dictionary<Vector3Int, PlacementData> _gridData = new();

        public float CellSize => cellSize;
        public IReadOnlyDictionary<Vector3Int, PlacementData> GridData => _gridData;

        private void OnValidate()
        {
            if (grid && cellSize > 0)
            {
                grid.cellSize = new Vector3(cellSize, cellSize, cellSize);
            }
        }

        public void SetGridData(Dictionary<Vector3Int, PlacementData> data)
        {
            _gridData = data;
        }

        public void RegisterBlock(Vector3 worldPosition, GameObject blockInstance, string instanceID)
        {
            Vector3Int cellPosition = grid.WorldToCell(worldPosition);
            List<Vector3Int> occupiedCells = GetOccupiedCells(blockInstance);
            var placementData = new PlacementData(cellPosition, occupiedCells, instanceID);

            // Add to grid data dictionary
            foreach (Vector3Int cell in occupiedCells)
            {
                _gridData[cell] = placementData;
            }
        }

        public bool TryRemoveBlock(Vector3 worldPosition, out string instanceID)
        {
            Vector3Int cellPosition = grid.WorldToCell(worldPosition);
            if (_gridData.TryGetValue(cellPosition, out var placementData))
            {
                // Remove from grid data dictionary
                foreach (Vector3Int cell in placementData.OccupiedCells)
                {
                    _gridData.Remove(cell);
                    instanceID = placementData.InstanceID;
                    return true;
                }
            }

            instanceID = null;
            return false;
        }

        public Vector3 WorldToGridWorld(Vector3 worldPosition)
        {
            Vector3Int cellPosition = grid.WorldToCell(worldPosition);
            return grid.CellToWorld(cellPosition) + grid.cellSize * 0.5f;
        }

        public Vector3 CellToWorld(Vector3Int cellPosition)
        {
            return grid.CellToWorld(cellPosition) + grid.cellSize * 0.5f;
        }

        public bool IsEmptyGridSpace(GameObject blockInstance)
        {
            Bounds bounds = blockInstance.GetComponentInChildren<Collider>().bounds;

            // Offset 0.01 to avoid edge cases where bounds.min/max is exactly on the cell edge
            Vector3Int minCell = grid.WorldToCell(bounds.min + 0.01f * Vector3.one);
            Vector3Int maxCell = grid.WorldToCell(bounds.max - 0.01f * Vector3.one);

            for (int x = minCell.x; x <= maxCell.x; x++)
            {
                for (int y = minCell.y; y <= maxCell.y; y++)
                {
                    for (int z = minCell.z; z <= maxCell.z; z++)
                    {
                        if (_gridData.ContainsKey(new Vector3Int(x, y, z)))
                        {
                            Debug.Log($"Cell: ({x}, {y}, {z})");
                            Debug.Log("Grid space is occupied");
                            return false;
                        }

                        bool isInsideGrid = x >= -gridHalfSize.x && x <= gridHalfSize.x &&
                                            y >= -gridHalfSize.y && y <= gridHalfSize.y &&
                                            z >= -gridHalfSize.z && z <= gridHalfSize.z;
                        if (!isInsideGrid)
                        {
                            Debug.Log("Grid space is not inside grid");
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private List<Vector3Int> GetOccupiedCells(GameObject blockInstance)
        {
            List<Vector3Int> occupiedCells = new List<Vector3Int>();

            Bounds bounds = blockInstance.GetComponentInChildren<Collider>().bounds;

            // Offset 0.01 to avoid edge cases where bounds.min/max is exactly on the cell edge
            Vector3Int minCell = grid.WorldToCell(bounds.min + 0.01f * Vector3.one);
            Vector3Int maxCell = grid.WorldToCell(bounds.max - 0.01f * Vector3.one);

            for (int x = minCell.x; x <= maxCell.x; x++)
            {
                for (int y = minCell.y; y <= maxCell.y; y++)
                {
                    for (int z = minCell.z; z <= maxCell.z; z++)
                    {
                        Vector3Int cell = new Vector3Int(x, y, z);
                        occupiedCells.Add(cell);
                    }
                }
            }

            return occupiedCells;
        }
    }
}