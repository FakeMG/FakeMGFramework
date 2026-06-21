using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.GodCamera
{
    /// <summary>
    /// Applies controller and pointer zoom while preserving the configured screen anchor.
    /// </summary>
    public sealed class CameraZoomMotion
    {
        private readonly CameraInputSubscriber _cameraInputSubscriber;
        private readonly CameraRigView _cameraRigView;
        private readonly CameraZoomCalculator _zoomCalculator;
        private readonly CameraMotionBounds _motionBounds;

        private Vector3 _focusSmoothVelocityMetersPerSecond;
        private float _zoomVelocityMetersPerSecond;

        public CameraZoomMotion(
            CameraInputSubscriber cameraInputSubscriber,
            CameraRigView cameraRigView,
            CameraZoomCalculator zoomCalculator,
            CameraMotionBounds motionBounds)
        {
            _cameraInputSubscriber = cameraInputSubscriber;
            _cameraRigView = cameraRigView;
            _zoomCalculator = zoomCalculator;
            _motionBounds = motionBounds;
        }

        #region Public Methods

        public void ApplyZoom(
            CameraMotionState motionState,
            CameraProfileSO profile,
            Bounds allowedBoundsMeters,
            float deltaTimeSeconds)
        {
            float controllerZoomDeltaMeters = _cameraInputSubscriber.ReadControllerZoomInput()
                                              * profile.ZoomSpeedMetersPerSecond
                                              * deltaTimeSeconds;
            float pointerZoomDeltaMeters = _cameraInputSubscriber.ConsumePointerZoomSteps()
                                           * profile.PointerZoomSizeStepMeters;
            float zoomDeltaMeters = controllerZoomDeltaMeters + pointerZoomDeltaMeters;

            if (Mathf.Approximately(zoomDeltaMeters, 0f))
            {
                SmoothCurrentZoomState(motionState, profile, deltaTimeSeconds);
                return;
            }

            CameraZoomAnchorMode zoomAnchorMode = Mathf.Abs(pointerZoomDeltaMeters) > 0f
                ? profile.MouseZoomAnchorMode
                : profile.ControllerZoomAnchorMode;
            float desiredZoomMeters = _zoomCalculator.CalculateTargetZoomMeters(
                motionState.TargetZoomMeters,
                zoomDeltaMeters,
                profile);

            if (_motionBounds.TryClampZoomMeters(
                    motionState,
                    profile,
                    desiredZoomMeters,
                    allowedBoundsMeters,
                    out float clampedZoomMeters))
            {
                desiredZoomMeters = clampedZoomMeters;
            }

            Vector3 anchorCorrectionMeters = CalculateAnchorCorrectionMeters(
                motionState,
                desiredZoomMeters,
                _cameraInputSubscriber.ReadPointerPositionPixels(),
                zoomAnchorMode);

            motionState.TargetZoomMeters = desiredZoomMeters;
            motionState.TargetFocusPositionMeters += anchorCorrectionMeters;
            _motionBounds.ClampTargetFocusPosition(motionState, profile, allowedBoundsMeters);
            SmoothCurrentZoomState(motionState, profile, deltaTimeSeconds);
        }

        public void StopFocusSmoothing(CameraMotionState motionState)
        {
            motionState.TargetFocusPositionMeters = motionState.CurrentFocusPositionMeters;
            _focusSmoothVelocityMetersPerSecond = Vector3.zero;
        }

        #endregion

        #region Private Methods

        private void SmoothCurrentZoomState(
            CameraMotionState motionState,
            CameraProfileSO profile,
            float deltaTimeSeconds)
        {
            motionState.CurrentFocusPositionMeters = Vector3.SmoothDamp(
                motionState.CurrentFocusPositionMeters,
                motionState.TargetFocusPositionMeters,
                ref _focusSmoothVelocityMetersPerSecond,
                profile.ZoomSmoothingSeconds,
                Mathf.Infinity,
                deltaTimeSeconds);

            float currentZoomMeters = Mathf.SmoothDamp(
                motionState.GetCurrentZoomMeters(profile),
                motionState.TargetZoomMeters,
                ref _zoomVelocityMetersPerSecond,
                profile.ZoomSmoothingSeconds,
                Mathf.Infinity,
                deltaTimeSeconds);
            motionState.SetCurrentZoomMeters(profile, currentZoomMeters);
        }

        private Vector3 CalculateAnchorCorrectionMeters(
            CameraMotionState motionState,
            float targetZoomMeters,
            Vector2 anchorScreenPositionPixels,
            CameraZoomAnchorMode zoomAnchorMode)
        {
            if (zoomAnchorMode == CameraZoomAnchorMode.FocusPoint)
            {
                return Vector3.zero;
            }

            if (zoomAnchorMode == CameraZoomAnchorMode.ScreenCenter)
            {
                anchorScreenPositionPixels = _cameraRigView.GetScreenCenterPixels();
            }

            if (!_cameraRigView.TryProjectScreenPointToGround(
                    anchorScreenPositionPixels,
                    out Vector3 originalAnchorMeters))
            {
                Echo.Warning("Cannot anchor camera zoom because the current screen ray does not hit the ground plane.");
                return Vector3.zero;
            }

            if (!_cameraRigView.TryProjectScreenPointToGround(
                    anchorScreenPositionPixels,
                    motionState.TargetFocusPositionMeters,
                    motionState.CurrentYawDegrees,
                    targetZoomMeters,
                    out Vector3 targetAnchorMeters))
            {
                Echo.Warning("Cannot anchor camera zoom because the target screen ray does not hit the ground plane.");
                return Vector3.zero;
            }

            // The difference between the current and future ray hits is the focus shift required to keep the anchor fixed.
            return _zoomCalculator.CalculateAnchorCorrectionMeters(originalAnchorMeters, targetAnchorMeters);
        }

        #endregion
    }
}
