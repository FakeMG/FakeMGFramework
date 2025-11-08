using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.Framework.GridBuilding
{
    /// <summary>
    /// The Grid system only cares about which cells are occupied
    /// and keep a reference to the occupying block via PlacementData.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        [SerializeField] private Grid _grid;
        [SerializeField] private Material _gridMaterial;
        [SerializeField] private float _cellSize = 1f;
        [SerializeField] private Vector3Int _gridHalfSize = new(10, 10, 10);
        private Dictionary<Vector3Int, PlacementData> _gridData = new();

        public float CellSize => _cellSize;
        public IReadOnlyDictionary<Vector3Int, PlacementData> GridData => _gridData;
        private readonly int _cellPerUnit = Shader.PropertyToID("_CellPerUnit");

        private void OnValidate()
        {
            if (_grid && _cellSize > 0)
            {
                _grid.cellSize = new Vector3(_cellSize, _cellSize, _cellSize);
            }

            if (_gridMaterial && _cellSize > 0)
            {
                _gridMaterial.SetVector(_cellPerUnit, new Vector2(1 / _cellSize, 1 / _cellSize));
            }
        }

        public void RegisterStructure(Vector3 worldPosition, GameObject blockInstance, string instanceID)
        {
            Vector3Int pivotCell = _grid.WorldToCell(worldPosition);
            List<Vector3Int> occupiedCells = GetOccupiedCells(blockInstance);

            PlacementData placementData = new(pivotCell, occupiedCells, instanceID);
            _gridData[pivotCell] = placementData;

            // Remove unnecessary data to optimize memory
            PlacementData optimizedPlacementData = new(pivotCell, new List<Vector3Int>(), instanceID);
            // Add to grid data dictionary
            foreach (Vector3Int cell in occupiedCells)
            {
                if (cell != pivotCell)
                {
                    _gridData[cell] = optimizedPlacementData;
                }
            }
        }

        public bool TryRemoveStructure(Vector3 worldPosition, out string instanceID)
        {
            Vector3Int cellPosition = _grid.WorldToCell(worldPosition);
            if (_gridData.TryGetValue(cellPosition, out PlacementData placementData))
            {
                var occupiedCells = _gridData[placementData.PivotCell].OccupiedCells;
                instanceID = placementData.InstanceID;

                // Remove from grid data dictionary
                foreach (Vector3Int cell in occupiedCells)
                {
                    _gridData.Remove(cell);
                }

                return true;
            }

            instanceID = null;
            return false;
        }

        public Vector3 WorldToGridWorld(Vector3 worldPosition)
        {
            Vector3Int cellPosition = WorldToCell(worldPosition);
            return CellToWorld(cellPosition);
        }

        public Vector3 CellToWorld(Vector3Int cellPosition)
        {
            // The world position is at the bottom center of the cell
            Vector3 offset = new Vector3(_cellSize, 0, _cellSize) * 0.5f;
            return _grid.CellToWorld(cellPosition) + offset;
        }

        public Vector3Int WorldToCell(Vector3 worldPosition)
        {
            return _grid.WorldToCell(worldPosition);
        }

        public bool IsEmptyGridSpace(GameObject blockInstance)
        {
            Bounds bounds = blockInstance.GetComponentInChildren<Collider>().bounds;

            // Offset 0.01 to avoid edge cases where bounds.min/max is exactly on the cell edge
            Vector3Int minCell = _grid.WorldToCell(bounds.min + 0.01f * Vector3.one);
            Vector3Int maxCell = _grid.WorldToCell(bounds.max - 0.01f * Vector3.one);

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

                        bool isInsideGrid = x >= -_gridHalfSize.x && x <= _gridHalfSize.x &&
                                            y >= -_gridHalfSize.y && y <= _gridHalfSize.y &&
                                            z >= -_gridHalfSize.z && z <= _gridHalfSize.z;
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
            List<Vector3Int> occupiedCells = new();

            Bounds bounds = blockInstance.GetComponentInChildren<Collider>().bounds;

            // Offset 0.01 to avoid edge cases where bounds.min/max is exactly on the cell edge
            Vector3Int minCell = _grid.WorldToCell(bounds.min + 0.01f * Vector3.one);
            Vector3Int maxCell = _grid.WorldToCell(bounds.max - 0.01f * Vector3.one);

            for (int x = minCell.x; x <= maxCell.x; x++)
            {
                for (int y = minCell.y; y <= maxCell.y; y++)
                {
                    for (int z = minCell.z; z <= maxCell.z; z++)
                    {
                        Vector3Int cell = new(x, y, z);
                        occupiedCells.Add(cell);
                    }
                }
            }

            return occupiedCells;
        }
    }
}