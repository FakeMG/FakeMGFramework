using UnityEngine;

namespace FakeMG.GodCamera
{
    /// <summary>
    /// Calculates focus and zoom corrections that keep the visible ground area inside allowed bounds.
    /// </summary>
    public class CameraBoundsClamp
    {
        private const int ZOOM_CLAMP_SEARCH_ITERATIONS = 8;

        private readonly CameraVisibleAreaCalculator _visibleAreaCalculator;

        public CameraBoundsClamp(CameraVisibleAreaCalculator visibleAreaCalculator)
        {
            _visibleAreaCalculator = visibleAreaCalculator;
        }

        #region Public Methods

        public bool TryClampFocusPositionMeters(
            Vector3 desiredFocusPositionMeters,
            float yawDegrees,
            CameraProfileSO profile,
            float zoomMeters,
            float aspect,
            Bounds allowedBoundsMeters,
            out Vector3 clampedFocusPositionMeters)
        {
            Bounds effectiveAllowedBoundsMeters = ExpandAllowedBoundsMeters(profile, allowedBoundsMeters);
            if (!_visibleAreaCalculator.TryCalculateGroundBoundsMeters(
                    desiredFocusPositionMeters,
                    yawDegrees,
                    profile,
                    zoomMeters,
                    aspect,
                    out Bounds visibleBoundsMeters))
            {
                clampedFocusPositionMeters = desiredFocusPositionMeters;
                return false;
            }

            Vector3 correctionMeters = Vector3.zero;
            correctionMeters.x = CalculateAxisCorrectionMeters(
                visibleBoundsMeters.min.x,
                visibleBoundsMeters.max.x,
                effectiveAllowedBoundsMeters.min.x,
                effectiveAllowedBoundsMeters.max.x);
            correctionMeters.z = CalculateAxisCorrectionMeters(
                visibleBoundsMeters.min.z,
                visibleBoundsMeters.max.z,
                effectiveAllowedBoundsMeters.min.z,
                effectiveAllowedBoundsMeters.max.z);

            clampedFocusPositionMeters = desiredFocusPositionMeters + correctionMeters;
            clampedFocusPositionMeters.y = allowedBoundsMeters.center.y;
            return true;
        }

        public bool TryClampZoomMeters(
            Vector3 focusPositionMeters,
            float yawDegrees,
            CameraProfileSO profile,
            float desiredZoomMeters,
            float aspect,
            Bounds allowedBoundsMeters,
            out float clampedZoomMeters)
        {
            Bounds effectiveAllowedBoundsMeters = ExpandAllowedBoundsMeters(profile, allowedBoundsMeters);
            clampedZoomMeters = Mathf.Clamp(
                desiredZoomMeters,
                profile.MinimumZoomMeters,
                profile.MaximumZoomMeters);

            if (!TryVisibleAreaFitsWithinBoundsSize(
                    focusPositionMeters,
                    yawDegrees,
                    profile,
                    clampedZoomMeters,
                    aspect,
                    effectiveAllowedBoundsMeters,
                    out bool doesFitWithinBounds))
            {
                return false;
            }

            if (doesFitWithinBounds)
            {
                return true;
            }

            float lowZoomMeters = profile.MinimumZoomMeters;
            float highZoomMeters = clampedZoomMeters;

            // Visible ground area grows monotonically with zoom, so binary search finds the largest fitting value.
            for (int i = 0; i < ZOOM_CLAMP_SEARCH_ITERATIONS; i++)
            {
                float middleZoomMeters = (lowZoomMeters + highZoomMeters) * 0.5f;
                if (!TryVisibleAreaFitsWithinBoundsSize(
                        focusPositionMeters,
                        yawDegrees,
                        profile,
                        middleZoomMeters,
                        aspect,
                        effectiveAllowedBoundsMeters,
                        out doesFitWithinBounds))
                {
                    return false;
                }

                if (doesFitWithinBounds)
                {
                    lowZoomMeters = middleZoomMeters;
                    continue;
                }

                highZoomMeters = middleZoomMeters;
            }

            clampedZoomMeters = lowZoomMeters;
            return true;
        }

        #endregion

        #region Private Methods

        private Bounds ExpandAllowedBoundsMeters(CameraProfileSO profile, Bounds allowedBoundsMeters)
        {
            Vector2 boundsOffsetMeters = profile.BoundsOffsetMeters;
            allowedBoundsMeters.Expand(new Vector3(
                boundsOffsetMeters.x * 2f,
                0f,
                boundsOffsetMeters.y * 2f));
            return allowedBoundsMeters;
        }

        private bool TryVisibleAreaFitsWithinBoundsSize(
            Vector3 focusPositionMeters,
            float yawDegrees,
            CameraProfileSO profile,
            float zoomMeters,
            float aspect,
            Bounds allowedBoundsMeters,
            out bool doesFitWithinBounds)
        {
            if (!_visibleAreaCalculator.TryCalculateGroundBoundsMeters(
                    focusPositionMeters,
                    yawDegrees,
                    profile,
                    zoomMeters,
                    aspect,
                    out Bounds visibleBoundsMeters))
            {
                doesFitWithinBounds = false;
                return false;
            }

            // Zoom validity depends on whether the footprint can fit anywhere inside the map.
            // Focus clamping performs the separate translation needed to place it there.
            doesFitWithinBounds = visibleBoundsMeters.size.x <= allowedBoundsMeters.size.x
                                  && visibleBoundsMeters.size.z <= allowedBoundsMeters.size.z;
            return true;
        }

        private float CalculateAxisCorrectionMeters(
            float visibleMinMeters,
            float visibleMaxMeters,
            float allowedMinMeters,
            float allowedMaxMeters)
        {
            float visibleSizeMeters = visibleMaxMeters - visibleMinMeters;
            float allowedSizeMeters = allowedMaxMeters - allowedMinMeters;

            if (visibleSizeMeters > allowedSizeMeters)
            {
                // No translation can fit an oversized interval, so centering minimizes overflow on both sides.
                float visibleCenterMeters = (visibleMinMeters + visibleMaxMeters) * 0.5f;
                float allowedCenterMeters = (allowedMinMeters + allowedMaxMeters) * 0.5f;
                return allowedCenterMeters - visibleCenterMeters;
            }

            if (visibleMinMeters < allowedMinMeters)
            {
                // Apply only the displacement required to bring the near edge back inside the boundary.
                return allowedMinMeters - visibleMinMeters;
            }

            if (visibleMaxMeters > allowedMaxMeters)
            {
                return allowedMaxMeters - visibleMaxMeters;
            }

            return 0f;
        }

        #endregion
    }
}
