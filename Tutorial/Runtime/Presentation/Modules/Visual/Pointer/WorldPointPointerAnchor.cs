using System;
using UnityEngine;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Anchors a pointer to a world-space point projected through the main camera. The point
    /// is re-read every frame, and the anchor is hidden while the point is behind the camera
    /// or outside the viewport.
    /// </summary>
    public sealed class WorldPointPointerAnchor : ITutorialPointerAnchor
    {
        private readonly Func<Vector3?> _resolveWorldPositionMeters;

        /// <param name="resolveWorldPositionMeters">Returns null while the point does not exist.</param>
        public WorldPointPointerAnchor(Func<Vector3?> resolveWorldPositionMeters)
        {
            _resolveWorldPositionMeters = resolveWorldPositionMeters;
        }

        #region Public Methods

        public bool TryGetScreenPositionPixels(out Vector2 screenPositionPixels)
        {
            screenPositionPixels = default;
            Camera camera = Camera.main;
            Vector3? worldPositionMeters = _resolveWorldPositionMeters();
            if (camera == null || !worldPositionMeters.HasValue)
            {
                return false;
            }

            Vector3 viewportPosition = camera.WorldToViewportPoint(worldPositionMeters.Value);
            if (viewportPosition.z <= 0f || viewportPosition.x < 0f || viewportPosition.x > 1f ||
                viewportPosition.y < 0f || viewportPosition.y > 1f)
            {
                return false;
            }

            screenPositionPixels = camera.ViewportToScreenPoint(viewportPosition);
            return true;
        }

        #endregion
    }
}
