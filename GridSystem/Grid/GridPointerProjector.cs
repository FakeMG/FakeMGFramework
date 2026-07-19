using UnityEngine;

namespace FakeMG.GridSystem
{
    /// <summary>
    /// Converts screen-space pointer positions into grid-aligned placement positions.
    /// </summary>
    public sealed class GridPointerProjector
    {
        private const float MAX_POINTER_RAYCAST_DISTANCE_METERS = 100f;

        private readonly GridManager _gridManager;
        private readonly LayerMask _placementLayerMask;
        private readonly Camera _mainCamera;

        public GridPointerProjector(
            GridManager gridManager,
            LayerMask placementLayerMask,
            Camera mainCamera)
        {
            _gridManager = gridManager;
            _placementLayerMask = placementLayerMask;
            _mainCamera = mainCamera;
        }

        #region Public Methods

        public bool TryGetGridWorldPosition(
            Vector2 pointerPositionPixels,
            out Vector3 gridWorldPosition,
            out RaycastHit hitInfo)
        {
            Ray pointerRay = _mainCamera.ScreenPointToRay(pointerPositionPixels);
            if (Physics.Raycast(
                    pointerRay,
                    out hitInfo,
                    MAX_POINTER_RAYCAST_DISTANCE_METERS,
                    _placementLayerMask))
            {
                Vector3 selectedWorldPosition = hitInfo.point - hitInfo.normal * 0.01f;
                gridWorldPosition = _gridManager.WorldToGridWorld(selectedWorldPosition + hitInfo.normal);
                return true;
            }

            gridWorldPosition = Vector3.zero;
            return false;
        }

        #endregion
    }
}
