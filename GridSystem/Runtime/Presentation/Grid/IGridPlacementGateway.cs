using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.GridSystem
{
    /// <summary>
    /// Provides grid snapping, occupancy validation, and occupancy-index rebuilding for placement workflows.
    /// </summary>
    public interface IGridPlacementGateway
    {
        float CellSizeMeters { get; }

        #region Public Methods

        void RebuildOccupancyIndex(IReadOnlyCollection<GridOccupantPlacement> structurePlacements);

        bool TryGetInstanceIdAtPosition(Vector3 worldPosition, out string instanceId);

        Vector3 WorldToGridWorld(Vector3 worldPosition);

        IReadOnlyList<Vector3Int> GetCanonicalOccupiedCellOffsets(GridFootprint footprint);

        bool CanOccupy(
            GridFootprint footprint,
            Vector3 worldPosition,
            int rotationDegrees,
            string ignoredInstanceId = null);

        #endregion
    }
}
