using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.GodCamera
{
    /// <summary>
    /// Constrains current and target camera motion state to the configured world bounds.
    /// </summary>
    public sealed class CameraMotionBounds
    {
        private readonly CameraBoundsClamp _boundsClamp;

        private bool _hasReportedGroundProjectionFailure;

        public CameraMotionBounds(CameraBoundsClamp boundsClamp)
        {
            _boundsClamp = boundsClamp;
        }

        #region Public Methods

        public void ClampInitialState(
            CameraMotionState motionState, CameraProfileSO profileSO, Bounds allowedBoundsMeters, float aspect)
        {
            if (TryClampZoomMeters(
                    motionState,
                    profileSO,
                    motionState.GetCurrentZoomMeters(profileSO.ProjectionType),
                    allowedBoundsMeters,
                    aspect,
                    out float clampedZoomMeters))
            {
                motionState.SetCurrentZoomMeters(profileSO.ProjectionType, clampedZoomMeters);
            }

            motionState.TargetZoomMeters = motionState.GetCurrentZoomMeters(profileSO.ProjectionType);
            if (TryClampFocusPositionMeters(
                    motionState.CurrentFocusPositionMeters,
                    motionState.CurrentYawDegrees,
                    profileSO,
                    motionState.TargetZoomMeters,
                    allowedBoundsMeters,
                    aspect,
                    out Vector3 clampedFocusPositionMeters))
            {
                motionState.SetCurrentAndTargetFocusPositionMeters(clampedFocusPositionMeters);
            }
        }

        public void ClampFocusPositions(
            CameraMotionState motionState,
            CameraProfileSO profileSO,
            Bounds allowedBoundsMeters,
            float aspect)
        {
            if (TryClampFocusPositionMeters(
                    motionState.CurrentFocusPositionMeters,
                    motionState.CurrentYawDegrees,
                    profileSO,
                    motionState.GetCurrentZoomMeters(profileSO.ProjectionType),
                    allowedBoundsMeters,
                    aspect,
                    out Vector3 clampedCurrentFocusPositionMeters))
            {
                motionState.CurrentFocusPositionMeters = clampedCurrentFocusPositionMeters;
            }

            ClampTargetFocusPosition(motionState, profileSO, allowedBoundsMeters, aspect);
        }

        public bool TryClampZoomMeters(
            CameraMotionState motionState,
            CameraProfileSO profileSO,
            float desiredZoomMeters,
            Bounds allowedBoundsMeters,
            float aspect,
            out float clampedZoomMeters)
        {
            bool hasProjectedGroundBounds = _boundsClamp.TryClampZoomMeters(
                motionState.CurrentFocusPositionMeters,
                motionState.CurrentYawDegrees,
                profileSO,
                desiredZoomMeters,
                aspect,
                allowedBoundsMeters,
                out clampedZoomMeters);
            UpdateProjectionFailureState(hasProjectedGroundBounds);
            return hasProjectedGroundBounds;
        }

        public void ClampTargetFocusPosition(
            CameraMotionState motionState,
            CameraProfileSO profileSO,
            Bounds allowedBoundsMeters,
            float aspect)
        {
            if (TryClampFocusPositionMeters(
                    motionState.TargetFocusPositionMeters,
                    motionState.CurrentYawDegrees,
                    profileSO,
                    motionState.TargetZoomMeters,
                    allowedBoundsMeters,
                    aspect,
                    out Vector3 clampedTargetFocusPositionMeters))
            {
                motionState.TargetFocusPositionMeters = clampedTargetFocusPositionMeters;
            }
        }

        #endregion

        #region Private Methods

        private bool TryClampFocusPositionMeters(
            Vector3 desiredFocusPositionMeters,
            float yawDegrees,
            CameraProfileSO profileSO,
            float zoomMeters,
            Bounds allowedBoundsMeters,
            float aspect,
            out Vector3 clampedFocusPositionMeters)
        {
            bool hasProjectedGroundBounds = _boundsClamp.TryClampFocusPositionMeters(
                desiredFocusPositionMeters,
                yawDegrees,
                profileSO,
                zoomMeters,
                aspect,
                allowedBoundsMeters,
                out clampedFocusPositionMeters);
            UpdateProjectionFailureState(hasProjectedGroundBounds);
            return hasProjectedGroundBounds;
        }

        private void UpdateProjectionFailureState(bool hasProjectedGroundBounds)
        {
            if (hasProjectedGroundBounds)
            {
                _hasReportedGroundProjectionFailure = false;
                return;
            }

            if (_hasReportedGroundProjectionFailure)
            {
                return;
            }

            Echo.Warning(
                "Camera bounds cannot be calculated because one or more viewport rays do not hit the ground plane. Check camera pitch, field of view, and zoom limits.");
            _hasReportedGroundProjectionFailure = true;
        }

        #endregion
    }
}
