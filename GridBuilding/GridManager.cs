using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.Framework.GridBuilding
{
    /// <summary>
    /// The Grid system only cares about which cells are occupied
    /// and keep a reference to the occupying structure via PlacementData.
    /// <br/>
    /// IMPORTANT!: The pivot of a structure is at the bottom center of a cell.
    /// <br/>
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        [SerializeField] private Grid _grid;
        [SerializeField] private Material _gridMaterial;
        [SerializeField] private float _cellSize = 1f;
        [SerializeField] private Vector3Int _gridHalfSize = new(10, 10, 10);

        [Header("Debugging")]
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private bool _drawGridGizmos = true;
        [SerializeField] private bool _drawEmptyCells;
        [SerializeField] private Color _gridBoundsColor = new(0.25f, 0.8f, 1f, 0.9f);
        [SerializeField] private Color _occupiedCellColor = new(1f, 0.45f, 0.2f, 0.45f);
        [SerializeField] private Color _pivotCellColor = new(1f, 0.95f, 0.2f, 0.6f);
        [SerializeField] private Color _emptyCellColor = new(0.2f, 0.35f, 0.6f, 0.12f);

        private readonly Dictionary<Vector3Int, PlacementData> _gridData = new();
        private readonly int _cellPerUnit = Shader.PropertyToID("_CellPerUnit");

        public float CellSize => _cellSize;
        public IReadOnlyDictionary<Vector3Int, PlacementData> GridData => _gridData;

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

        private void OnDrawGizmos()
        {
            if (!_drawGridGizmos || !_grid)
            {
                return;
            }

            DrawGridBounds();
            DrawEmptyCells();
            DrawOccupiedCells();

            void DrawGridBounds()
            {
                Vector3Int minCell = new(-_gridHalfSize.x, -_gridHalfSize.y, -_gridHalfSize.z);
                Vector3Int maxCell = new(_gridHalfSize.x, _gridHalfSize.y, _gridHalfSize.z);

                Vector3 minCenter = GetCellCenter(minCell);
                Vector3 maxCenter = GetCellCenter(maxCell);
                Vector3 boundsCenter = (minCenter + maxCenter) * 0.5f;

                Vector3 boundsSize = new(
                    (_gridHalfSize.x * 2 + 1) * _cellSize,
                    (_gridHalfSize.y * 2 + 1) * _cellSize,
                    (_gridHalfSize.z * 2 + 1) * _cellSize);

                Gizmos.color = _gridBoundsColor;
                Gizmos.DrawWireCube(boundsCenter, boundsSize);
            }

            void DrawEmptyCells()
            {
                if (!_drawEmptyCells)
                {
                    return;
                }

                Vector3Int minCell = new(-_gridHalfSize.x, -_gridHalfSize.y, -_gridHalfSize.z);
                Vector3Int maxCell = new(_gridHalfSize.x, _gridHalfSize.y, _gridHalfSize.z);

                for (int x = minCell.x; x <= maxCell.x; x++)
                {
                    for (int y = minCell.y; y <= maxCell.y; y++)
                    {
                        for (int z = minCell.z; z <= maxCell.z; z++)
                        {
                            Vector3Int cell = new(x, y, z);
                            if (_gridData.ContainsKey(cell))
                            {
                                continue;
                            }

                            Gizmos.color = _emptyCellColor;
                            Gizmos.DrawWireCube(GetCellCenter(cell), Vector3.one * _cellSize);
                        }
                    }
                }
            }

            void DrawOccupiedCells()
            {
                if (_gridData.Count == 0)
                {
                    return;
                }

                foreach (KeyValuePair<Vector3Int, PlacementData> kvp in _gridData)
                {
                    Vector3Int cell = kvp.Key;
                    PlacementData placementData = kvp.Value;
                    bool isPivotCell = cell == placementData.PivotCell;

                    Gizmos.color = isPivotCell ? _pivotCellColor : _occupiedCellColor;
                    Gizmos.DrawCube(GetCellCenter(cell), Vector3.one * _cellSize);
                }
            }

            Vector3 GetCellCenter(Vector3Int cell)
            {
                Vector3 center = CellToWorld(cell);
                center += Vector3.up * (_cellSize * 0.5f);
                return center;
            }
        }

        /// <summary>
        /// Registers a structure in the grid at the specified position.
        /// </summary>
        /// <param name="oldWorldPosition">
        /// The current world position of the structure. If null, assumes the structure's transform is already at <paramref name="newWorldPosition"/>.
        /// Specify this when the Physics system hasn't updated the structure's collider position to <paramref name="newWorldPosition"/> yet,
        /// allowing bounds to be calculated at the target position before the Physics update.
        /// </param>
        public void RegisterStructure(GameObject structureInstance, string instanceID, Vector3 newWorldPosition, Vector3? oldWorldPosition = null)
        {
            Vector3Int pivotCell = _grid.WorldToCell(newWorldPosition);
            List<Vector3Int> occupiedCells = GetOccupiedCells(structureInstance, newWorldPosition, oldWorldPosition);

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

        public bool IsEmptyGridSpace(Vector3 worldPosition)
        {
            Vector3Int cellPosition = _grid.WorldToCell(worldPosition);
            return !_gridData.ContainsKey(cellPosition);
        }

        /// <summary>
        /// Checks if the grid space is empty for the given structure instance at the specified world position override.
        /// </summary>
        /// <param name="oldWorldPosition">
        /// The current world position of the structure. If null, assumes the structure's transform is already at <paramref name="newWorldPosition"/>.
        /// Specify this when the Physics system hasn't updated the structure's collider position to <paramref name="newWorldPosition"/> yet,
        /// allowing bounds to be calculated at the target position before the Physics update.
        /// </param>
        public bool IsEmptyGridSpace(GameObject structureInstance, Vector3 worldPositionOverride, Vector3? oldWorldPosition = null)
        {
            Bounds bounds = GetColliderBounds(structureInstance, worldPositionOverride, oldWorldPosition);

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
                            Echo.Log($"Cell: ({x}, {y}, {z})", _enableLogging, this);
                            Echo.Log("Grid space is occupied", _enableLogging, this);
                            return false;
                        }

                        bool isInsideGrid = x >= -_gridHalfSize.x && x <= _gridHalfSize.x &&
                                            y >= -_gridHalfSize.y && y <= _gridHalfSize.y &&
                                            z >= -_gridHalfSize.z && z <= _gridHalfSize.z;
                        if (!isInsideGrid)
                        {
                            Echo.Log("Grid space is not inside grid", _enableLogging, this);
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Gets all cells that would be occupied by the structure at the specified position.
        /// </summary>
        /// <param name="oldWorldPosition">
        /// The current world position of the structure. If null, assumes the structure's transform is already at <paramref name="newWorldPosition"/>.
        /// Specify this when the Physics system hasn't updated the structure's collider position to <paramref name="newWorldPosition"/> yet,
        /// allowing bounds to be calculated at the target position before the Physics update.
        /// </param>
        private List<Vector3Int> GetOccupiedCells(GameObject structureInstance, Vector3 worldPositionOverride, Vector3? oldWorldPosition = null)
        {
            List<Vector3Int> occupiedCells = new();

            Bounds bounds = GetColliderBounds(structureInstance, worldPositionOverride, oldWorldPosition);

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

        /// <summary>
        /// Calculates collider bounds as if the structure were placed at <paramref name="worldPositionOverride"/>.
        /// </summary>
        /// <param name="oldWorldPosition">
        /// The current world position of the structure. If null, assumes the structure's transform is already at <paramref name="newWorldPosition"/>.
        /// Specify this when the Physics system hasn't updated the structure's collider position to <paramref name="newWorldPosition"/> yet,
        /// allowing bounds to be calculated at the target position before the Physics update.
        /// </param>
        private Bounds GetColliderBounds(GameObject structureInstance, Vector3 worldPositionOverride, Vector3? oldWorldPosition = null)
        {
            // Manually offset the current bounds so placement checks can evaluate target positions without Physics.SyncTransforms.
            Bounds bounds = structureInstance.GetComponentInChildren<Collider>().bounds;
            // Must minus transform position instead of bounds.center cause the actual center may not align with transform position.
            Vector3 currentPosition = oldWorldPosition ?? worldPositionOverride;
            Vector3 positionDelta = worldPositionOverride - currentPosition;

#if UNITY_EDITOR
            if (oldWorldPosition == null || oldWorldPosition == worldPositionOverride)
            {
                float sqrDistanceToCurrent = (bounds.center - worldPositionOverride).sqrMagnitude;
                float sqrThreshold = _cellSize * 2 * _cellSize * 2; // 2 cells distance
                if (sqrDistanceToCurrent > sqrThreshold)
                {
                    Debug.LogWarning($"Bounds center is far from target position. Physics may not have updated yet. Distance: {Mathf.Sqrt(sqrDistanceToCurrent):F2}, Structure: {structureInstance.name}", this);
                }
            }
#endif

            bounds.center += positionDelta;
            return bounds;
        }
    }
}