using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.GridSystem
{
    /// <summary>
    /// Converts rectangular grid bounds to deterministic cell offsets and validates saved offsets
    /// before reconstructing their canonical bounds.
    /// </summary>
    public static class RectangularGridFootprintCellOffsets
    {
        #region Public Methods

        public static IReadOnlyList<Vector3Int> Create(BoundsInt cellBounds)
        {
            List<Vector3Int> occupiedCellOffsets = new(cellBounds.size.x * cellBounds.size.y * cellBounds.size.z);

            for (int x = cellBounds.min.x; x < cellBounds.max.x; x++)
            {
                for (int y = cellBounds.min.y; y < cellBounds.max.y; y++)
                {
                    for (int z = cellBounds.min.z; z < cellBounds.max.z; z++)
                    {
                        occupiedCellOffsets.Add(new Vector3Int(x, y, z));
                    }
                }
            }

            return occupiedCellOffsets;
        }

        public static bool TryCreateBounds(
            IReadOnlyCollection<Vector3Int> occupiedCellOffsets,
            out BoundsInt cellBounds,
            out string failureReason)
        {
            cellBounds = default;
            if (occupiedCellOffsets == null || occupiedCellOffsets.Count == 0)
            {
                failureReason = "Occupied cell offsets are missing.";
                return false;
            }

            HashSet<Vector3Int> uniqueCellOffsets = new();
            Vector3Int minimumCellOffset = default;
            Vector3Int maximumCellOffset = default;
            bool hasFirstCellOffset = false;

            foreach (Vector3Int occupiedCellOffset in occupiedCellOffsets)
            {
                if (!uniqueCellOffsets.Add(occupiedCellOffset))
                {
                    failureReason = $"Occupied cell offset '{occupiedCellOffset}' is duplicated.";
                    return false;
                }

                if (!hasFirstCellOffset)
                {
                    minimumCellOffset = occupiedCellOffset;
                    maximumCellOffset = occupiedCellOffset;
                    hasFirstCellOffset = true;
                    continue;
                }

                minimumCellOffset = Vector3Int.Min(minimumCellOffset, occupiedCellOffset);
                maximumCellOffset = Vector3Int.Max(maximumCellOffset, occupiedCellOffset);
            }

            long sizeX = (long)maximumCellOffset.x - minimumCellOffset.x + 1L;
            long sizeY = (long)maximumCellOffset.y - minimumCellOffset.y + 1L;
            long sizeZ = (long)maximumCellOffset.z - minimumCellOffset.z + 1L;
            if (sizeX > int.MaxValue || sizeY > int.MaxValue || sizeZ > int.MaxValue)
            {
                failureReason = "Occupied cell offsets exceed supported grid bounds.";
                return false;
            }

            if (sizeX > long.MaxValue / sizeY || sizeX * sizeY > long.MaxValue / sizeZ)
            {
                failureReason = "Occupied cell offsets exceed supported footprint capacity.";
                return false;
            }

            long expectedCellCount = sizeX * sizeY * sizeZ;
            if (expectedCellCount != uniqueCellOffsets.Count)
            {
                failureReason = "Occupied cell offsets do not form a filled rectangular footprint.";
                return false;
            }

            cellBounds = new BoundsInt(minimumCellOffset, new Vector3Int((int)sizeX, (int)sizeY, (int)sizeZ));
            failureReason = string.Empty;
            return true;
        }

        #endregion
    }
}
