using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.GodCamera
{
    /// <summary>
    /// Constrains current and target camera motion state to the configured world bounds.
    /// </summary>
    public sealed class CameraMotionBounds
    {
        private readonly CameraRigView _cameraRigView;
        private readonly CameraBoundsClamp _boundsClamp;

        private bool _hasReportedGroundProjectionFailure;

        public CameraMotionBounds(CameraRigView cameraRigView, CameraBoundsClamp boundsClamp)
        {
            _cameraRigView = cameraRigView;
            _boundsClamp = boundsClamp;
        }

        #region Public Methods

        public void ClampInitialState(CameraMotionState motionState)
        {
            CameraProfileSO profile = _cameraRigView.CameraProfileSO;
            Bounds allowedBoundsMeters = _cameraRigView.BoundsProvider.GetBoundsMeters();

            if (TryClampZoomMeters(
                    motionState,
                    profile,
                    motionState.GetCurrentZoomMeters(profile),
                    allowedBoundsMeters,
                    out float clampedZoomMeters))
            {
                motionState.SetCurrentZoomMeters(profile, clampedZoomMeters);
            }

            motionState.TargetZoomMeters = motionState.GetCurrentZoomMeters(profile);
            if (TryClampFocusPositionMeters(
                    motionState.CurrentFocusPositionMeters,
                    motionState.CurrentYawDegrees,
                    profile,
                    motionState.TargetZoomMeters,
                    allowedBoundsMeters,
                    out Vector3 clampedFocusPositionMeters))
            {
                motionState.SetCurrentAndTargetFocusPositionMeters(clampedFocusPositionMeters);
            }
        }

        public void ClampFocusPositions(
            CameraMotionState motionState,
            CameraProfileSO profile,
            Bounds allowedBoundsMeters)
        {
            if (TryClampFocusPositionMeters(
                    motionState.CurrentFocusPositionMeters,
                    motionState.CurrentYawDegrees,
                    profile,
                    motionState.GetCurrentZoomMeters(profile),
                    allowedBoundsMeters,
                    out Vector3 clampedCurrentFocusPositionMeters))
            {
                motionState.CurrentFocusPositionMeters = clampedCurrentFocusPositionMeters;
            }

            ClampTargetFocusPosition(motionState, profile, allowedBoundsMeters);
        }

        public bool TryClampZoomMeters(
            CameraMotionState motionState,
            CameraProfileSO profile,
            float desiredZoomMeters,
            Bounds allowedBoundsMeters,
            out float clampedZoomMeters)
        {
            bool hasProjectedGroundBounds = _boundsClamp.TryClampZoomMeters(
                motionState.CurrentFocusPositionMeters,
                motionState.CurrentYawDegrees,
                profile,
                desiredZoomMeters,
                _cameraRigView.Aspect,
                allowedBoundsMeters,
                out clampedZoomMeters);
            UpdateProjectionFailureState(hasProjectedGroundBounds);
            return hasProjectedGroundBounds;
        }

        public void ClampTargetFocusPosition(
            CameraMotionState motionState,
            CameraProfileSO profile,
            Bounds allowedBoundsMeters)
        {
            if (TryClampFocusPositionMeters(
                    motionState.TargetFocusPositionMeters,
                    motionState.CurrentYawDegrees,
                    profile,
                    motionState.TargetZoomMeters,
                    allowedBoundsMeters,
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
            CameraProfileSO profile,
            float zoomMeters,
            Bounds allowedBoundsMeters,
            out Vector3 clampedFocusPositionMeters)
        {
            bool hasProjectedGroundBounds = _boundsClamp.TryClampFocusPositionMeters(
                desiredFocusPositionMeters,
                yawDegrees,
                profile,
                zoomMeters,
                _cameraRigView.Aspect,
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
