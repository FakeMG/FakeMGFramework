using System.Collections.Generic;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.GridSystem
{
    /// <summary>
    /// Derives a structure's grid footprint from its combined mesh bounds so every touched cell
    /// is occupied, with an optional runtime override for construction placeholders.
    /// The prefab origin sits at the bottom center of its pivot cell, which may be anywhere inside
    /// the footprint, so occupied cells may extend in either direction on every axis.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GridFootprint : MonoBehaviour
    {
        // Guards against floating point noise pushing an exact cell fit into an extra cell.
        private const float CELL_FIT_TOLERANCE_METERS = 0.001f;

        private BoundsInt? _overriddenCellBounds;
        private BoundsInt? _cachedMeshCellBounds;
        private float _cachedMeshBoundsCellSizeMeters;

        #region Public Methods

        public BoundsInt GetCellBounds(float cellSizeMeters)
        {
            if (_overriddenCellBounds.HasValue)
            {
                return _overriddenCellBounds.Value;
            }

            if (_cachedMeshCellBounds.HasValue &&
                Mathf.Approximately(_cachedMeshBoundsCellSizeMeters, cellSizeMeters))
            {
                return _cachedMeshCellBounds.Value;
            }

            _cachedMeshCellBounds = CalculateCellBoundsFromMeshBounds(cellSizeMeters);
            _cachedMeshBoundsCellSizeMeters = cellSizeMeters;
            return _cachedMeshCellBounds.Value;
        }

        public void OverrideCellBounds(BoundsInt cellBounds)
        {
            if (cellBounds.size.x <= 0 || cellBounds.size.y <= 0 || cellBounds.size.z <= 0)
            {
                Echo.Error($"{name} cannot override structure footprint with invalid cell bounds '{cellBounds}'. All size axes must be greater than zero.", context: this);
                return;
            }

            _overriddenCellBounds = cellBounds;
        }

        public IReadOnlyList<Vector3Int> GetOccupiedCells(Vector3Int pivotCell, int rotationDegrees, float cellSizeMeters)
        {
            BoundsInt rotatedCellBounds = GetRotatedCellBounds(cellSizeMeters, rotationDegrees);
            List<Vector3Int> occupiedCells = new(rotatedCellBounds.size.x * rotatedCellBounds.size.y * rotatedCellBounds.size.z);

            for (int x = rotatedCellBounds.min.x; x < rotatedCellBounds.max.x; x++)
            {
                for (int y = rotatedCellBounds.min.y; y < rotatedCellBounds.max.y; y++)
                {
                    for (int z = rotatedCellBounds.min.z; z < rotatedCellBounds.max.z; z++)
                    {
                        occupiedCells.Add(new Vector3Int(
                            pivotCell.x + x,
                            pivotCell.y + y,
                            pivotCell.z + z));
                    }
                }
            }

            return occupiedCells;
        }

        public Vector3 GetScaleMeters(float cellSizeMeters, int rotationDegrees)
        {
            Vector3Int rotatedCellSize = GetRotatedCellBounds(cellSizeMeters, rotationDegrees).size;
            return new Vector3(
                rotatedCellSize.x * cellSizeMeters,
                1f,
                rotatedCellSize.z * cellSizeMeters);
        }

        public Vector3 GetHorizontalCenterOffsetMeters(float cellSizeMeters, int rotationDegrees)
        {
            BoundsInt rotatedCellBounds = GetRotatedCellBounds(cellSizeMeters, rotationDegrees);
            Vector3Int maximumCellOffset = rotatedCellBounds.max - Vector3Int.one;
            return new Vector3(
                (rotatedCellBounds.min.x + maximumCellOffset.x) * 0.5f * cellSizeMeters,
                0f,
                (rotatedCellBounds.min.z + maximumCellOffset.z) * 0.5f * cellSizeMeters);
        }

        public bool TryValidate(Object logContext = null)
        {
            if (_overriddenCellBounds.HasValue || TryCalculateLocalMeshBoundsMeters(out _))
            {
                return true;
            }

            Echo.Error($"{name} has no mesh bounds to derive a structure footprint from and no cell bounds override.", context: logContext);
            return false;
        }

        #endregion

        #region Private Methods

        private BoundsInt GetRotatedCellBounds(float cellSizeMeters, int rotationDegrees)
        {
            BoundsInt cellBounds = GetCellBounds(cellSizeMeters);
            int normalizedRotationDegrees = ((rotationDegrees % 360) + 360) % 360;
            if (normalizedRotationDegrees == 0)
            {
                return cellBounds;
            }

            Vector3Int maximumCellOffset = cellBounds.max - Vector3Int.one;
            Vector3Int firstCorner = RotateCellOffset(
                new Vector3Int(cellBounds.min.x, cellBounds.min.y, cellBounds.min.z),
                normalizedRotationDegrees);
            Vector3Int minimumRotatedCellOffset = firstCorner;
            Vector3Int maximumRotatedCellOffset = firstCorner;

            EncapsulateRotatedHorizontalCorner(
                new Vector3Int(maximumCellOffset.x, cellBounds.min.y, cellBounds.min.z),
                normalizedRotationDegrees,
                ref minimumRotatedCellOffset,
                ref maximumRotatedCellOffset);
            EncapsulateRotatedHorizontalCorner(
                new Vector3Int(cellBounds.min.x, cellBounds.min.y, maximumCellOffset.z),
                normalizedRotationDegrees,
                ref minimumRotatedCellOffset,
                ref maximumRotatedCellOffset);
            EncapsulateRotatedHorizontalCorner(
                new Vector3Int(maximumCellOffset.x, maximumCellOffset.y, maximumCellOffset.z),
                normalizedRotationDegrees,
                ref minimumRotatedCellOffset,
                ref maximumRotatedCellOffset);

            Vector3Int rotatedCellSize = maximumRotatedCellOffset - minimumRotatedCellOffset + Vector3Int.one;
            return new BoundsInt(minimumRotatedCellOffset, rotatedCellSize);
        }

        private static void EncapsulateRotatedHorizontalCorner(
            Vector3Int cellOffset,
            int rotationDegrees,
            ref Vector3Int minimumCellOffset,
            ref Vector3Int maximumCellOffset)
        {
            Vector3Int rotatedCellOffset = RotateCellOffset(cellOffset, rotationDegrees);
            minimumCellOffset = Vector3Int.Min(minimumCellOffset, rotatedCellOffset);
            maximumCellOffset = Vector3Int.Max(maximumCellOffset, rotatedCellOffset);
        }

        private static Vector3Int RotateCellOffset(Vector3Int cellOffset, int rotationDegrees)
        {
            return rotationDegrees switch
            {
                90 => new Vector3Int(cellOffset.z, cellOffset.y, -cellOffset.x),
                180 => new Vector3Int(-cellOffset.x, cellOffset.y, -cellOffset.z),
                270 => new Vector3Int(-cellOffset.z, cellOffset.y, cellOffset.x),
                _ => cellOffset
            };
        }

        private BoundsInt CalculateCellBoundsFromMeshBounds(float cellSizeMeters)
        {
            if (cellSizeMeters <= 0f)
            {
                Echo.Error($"{name} cannot derive a structure footprint from a non-positive grid cell size '{cellSizeMeters}'.", context: this);
                return new BoundsInt(Vector3Int.zero, Vector3Int.one);
            }

            if (!TryCalculateLocalMeshBoundsMeters(out Bounds localMeshBoundsMeters))
            {
                Echo.Error($"{name} has no mesh bounds to derive a structure footprint from. Falling back to a single cell.", context: this);
                return new BoundsInt(Vector3Int.zero, Vector3Int.one);
            }

            Vector3Int minimumCellOffset = new(
                ToHorizontalMinimumCellOffset(localMeshBoundsMeters.min.x, cellSizeMeters),
                ToVerticalMinimumCellOffset(localMeshBoundsMeters.min.y, cellSizeMeters),
                ToHorizontalMinimumCellOffset(localMeshBoundsMeters.min.z, cellSizeMeters));
            Vector3Int maximumCellOffset = new(
                ToHorizontalMaximumCellOffset(localMeshBoundsMeters.max.x, cellSizeMeters),
                ToVerticalMaximumCellOffset(localMeshBoundsMeters.max.y, cellSizeMeters),
                ToHorizontalMaximumCellOffset(localMeshBoundsMeters.max.z, cellSizeMeters));

            maximumCellOffset = Vector3Int.Max(minimumCellOffset, maximumCellOffset);
            Vector3Int cellSize = maximumCellOffset - minimumCellOffset + Vector3Int.one;
            return new BoundsInt(minimumCellOffset, cellSize);
        }

        private static int ToHorizontalMinimumCellOffset(float minimumMeters, float cellSizeMeters)
        {
            float cellShiftMeters = cellSizeMeters * 0.5f + CELL_FIT_TOLERANCE_METERS;
            return Mathf.FloorToInt((minimumMeters + cellShiftMeters) / cellSizeMeters);
        }

        private static int ToHorizontalMaximumCellOffset(float maximumMeters, float cellSizeMeters)
        {
            float cellShiftMeters = cellSizeMeters * 0.5f + CELL_FIT_TOLERANCE_METERS;
            return Mathf.CeilToInt((maximumMeters - cellShiftMeters) / cellSizeMeters);
        }

        private static int ToVerticalMinimumCellOffset(float minimumMeters, float cellSizeMeters)
        {
            return Mathf.FloorToInt(
                (minimumMeters + CELL_FIT_TOLERANCE_METERS) / cellSizeMeters);
        }

        private static int ToVerticalMaximumCellOffset(float maximumMeters, float cellSizeMeters)
        {
            return Mathf.CeilToInt(
                (maximumMeters - CELL_FIT_TOLERANCE_METERS) / cellSizeMeters) - 1;
        }

        // Bounds are combined from shared mesh data in root-local space so this also works on
        // prefab assets, where renderer and collider bounds are not available.
        private bool TryCalculateLocalMeshBoundsMeters(out Bounds localMeshBoundsMeters)
        {
            localMeshBoundsMeters = default;
            bool hasBounds = false;

            MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>(true);
            foreach (MeshFilter meshFilter in meshFilters)
            {
                if (!meshFilter.sharedMesh)
                {
                    continue;
                }

                Matrix4x4 meshToRootLocal = transform.worldToLocalMatrix * meshFilter.transform.localToWorldMatrix;
                Bounds meshBounds = meshFilter.sharedMesh.bounds;

                for (int cornerIndex = 0; cornerIndex < 8; cornerIndex++)
                {
                    Vector3 corner = new(
                        (cornerIndex & 1) == 0 ? meshBounds.min.x : meshBounds.max.x,
                        (cornerIndex & 2) == 0 ? meshBounds.min.y : meshBounds.max.y,
                        (cornerIndex & 4) == 0 ? meshBounds.min.z : meshBounds.max.z);
                    Vector3 cornerInRootLocal = meshToRootLocal.MultiplyPoint3x4(corner);

                    if (!hasBounds)
                    {
                        localMeshBoundsMeters = new Bounds(cornerInRootLocal, Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        localMeshBoundsMeters.Encapsulate(cornerInRootLocal);
                    }
                }
            }

            return hasBounds;
        }

        #endregion
    }
}
