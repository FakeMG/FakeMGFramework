using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.GridSystem
{
    /// <summary>
    /// Converts grid coordinates to cell bottom-center anchors and maintains a derived occupancy index
    /// for placed structure footprints.
    /// </summary>
    public class GridManager : MonoBehaviour, IGridPlacementGateway
    {
        [SerializeField] private Grid _grid;
        [SerializeField] private Material _gridMaterial;
        [SerializeField] private float _cellSizeMeters = 1f;
        [SerializeField] private Vector3Int _gridHalfSize = new(10, 10, 10);

        [Header("Debugging")]
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private bool _drawGridGizmos = true;
        [SerializeField] private bool _drawEmptyCells;
        [SerializeField] private Color _gridBoundsColor = new(0.25f, 0.8f, 1f, 0.9f);
        [SerializeField] private Color _occupiedCellColor = new(1f, 0.45f, 0.2f, 0.45f);
        [SerializeField] private Color _pivotCellColor = new(1f, 0.95f, 0.2f, 0.6f);
        [SerializeField] private Color _emptyCellColor = new(0.2f, 0.35f, 0.6f, 0.12f);

        private readonly GridOccupancyIndex _occupancyIndex = new();
        private readonly int _cellPerUnit = Shader.PropertyToID("_CellPerUnit");
        private GridGizmoDrawer _gizmoDrawer;

        public float CellSizeMeters => _cellSizeMeters;
        public IReadOnlyDictionary<Vector3Int, GridOccupantData> OccupiedCells => _occupancyIndex.OccupiedCells;

        #region Unity Lifecycle

        private void Awake()
        {
            _gizmoDrawer = new GridGizmoDrawer(this);
            _grid.cellSize = new Vector3(_cellSizeMeters, _cellSizeMeters, _cellSizeMeters);
        }

        private void OnValidate()
        {
            if (_gridMaterial && _cellSizeMeters > 0)
            {
                _gridMaterial.SetVector(_cellPerUnit, new Vector2(1 / _cellSizeMeters, 1 / _cellSizeMeters));
            }
        }

        private void OnDrawGizmos()
        {
            if (!_drawGridGizmos || !_grid)
            {
                return;
            }

            _gizmoDrawer ??= new GridGizmoDrawer(this);
            _gizmoDrawer.Draw(
                _drawEmptyCells,
                _gridHalfSize,
                _occupancyIndex.OccupiedCells,
                _gridBoundsColor,
                _occupiedCellColor,
                _pivotCellColor,
                _emptyCellColor);
        }

        #endregion

        #region Public Methods

        public void RebuildOccupancyIndex(IReadOnlyCollection<GridOccupantPlacement> structurePlacements)
        {
            _occupancyIndex.Rebuild(structurePlacements, this);
        }

        public bool TryGetInstanceIdAtPosition(Vector3 worldPosition, out string instanceId)
        {
            Vector3Int cellPosition = WorldToCell(worldPosition);
            return _occupancyIndex.TryGetInstanceIdAtCell(cellPosition, out instanceId);
        }

        public Vector3 WorldToGridWorld(Vector3 worldPosition)
        {
            Vector3Int cellPosition = WorldToCell(worldPosition);
            return CellToWorld(cellPosition);
        }

        public Vector3 CellToWorld(Vector3Int cellPosition)
        {
            Vector3 bottomCenterOffsetMeters = new(_cellSizeMeters * 0.5f, 0f, _cellSizeMeters * 0.5f);
            return _grid.CellToWorld(cellPosition) + bottomCenterOffsetMeters;
        }

        public Vector3Int WorldToCell(Vector3 worldPosition)
        {
            return _grid.WorldToCell(worldPosition);
        }

        public Bounds GetWorldBoundsMeters()
        {
            Vector3Int minCell = new(-_gridHalfSize.x, -_gridHalfSize.y, -_gridHalfSize.z);
            Vector3Int maxCell = new(_gridHalfSize.x, _gridHalfSize.y, _gridHalfSize.z);

            Vector3 minCenter = CellToWorld(minCell);
            Vector3 maxCenter = CellToWorld(maxCell);
            Vector3 center = (minCenter + maxCenter) * 0.5f;
            Vector3 size = new(
                (_gridHalfSize.x * 2 + 1) * _cellSizeMeters,
                (_gridHalfSize.y * 2 + 1) * _cellSizeMeters,
                (_gridHalfSize.z * 2 + 1) * _cellSizeMeters);

            return new Bounds(center, size);
        }

        public bool CanOccupy(
            GridFootprint footprint,
            Vector3 worldPosition,
            int rotationDegrees,
            string ignoredInstanceId = null)
        {
            IReadOnlyList<Vector3Int> occupiedCells = GetOccupiedCells(footprint, worldPosition, rotationDegrees);
            return _occupancyIndex.CanOccupy(
                occupiedCells,
                _gridHalfSize,
                ignoredInstanceId,
                _enableLogging,
                this);
        }

        public IReadOnlyList<Vector3Int> GetOccupiedCells(
            GridFootprint footprint,
            Vector3 worldPosition,
            int rotationDegrees)
        {
            Vector3Int pivotCell = WorldToCell(worldPosition);
            return footprint.GetOccupiedCells(pivotCell, rotationDegrees, _cellSizeMeters);
        }

        #endregion
    }
}
