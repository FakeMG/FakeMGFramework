using UnityEngine;

namespace FakeMG.GodCamera
{
    /// <summary>Owns zoom targets, ground-anchor correction, and zoom/focus smoothing.</summary>
    public sealed class CameraZoomMotion
    {
        private readonly CameraZoomCalculator _zoomCalculator;
        private Vector3 _focusSmoothVelocityMetersPerSecond;
        private float _zoomVelocityMetersPerSecond;

        #region Public Methods

        public CameraZoomMotion(CameraZoomCalculator zoomCalculator) => _zoomCalculator = zoomCalculator;

        public float CalculateTargetZoomMeters(
            CameraMotionState motionState, float zoomDeltaMeters, float minimumZoomMeters, float maximumZoomMeters)
        {
            return _zoomCalculator.CalculateTargetZoomMeters(
                motionState.TargetZoomMeters, zoomDeltaMeters, minimumZoomMeters, maximumZoomMeters);
        }

        public void SetZoomTarget(CameraMotionState motionState, float targetZoomMeters, Vector3 anchorCorrectionMeters)
        {
            motionState.TargetZoomMeters = targetZoomMeters;
            motionState.TargetFocusPositionMeters += anchorCorrectionMeters;
        }

        public void SmoothCurrentZoomState(
            CameraMotionState motionState, CameraProjectionType projectionType, float smoothingSeconds, float deltaTimeSeconds)
        {
            motionState.CurrentFocusPositionMeters = Vector3.SmoothDamp(
                motionState.CurrentFocusPositionMeters,
                motionState.TargetFocusPositionMeters,
                ref _focusSmoothVelocityMetersPerSecond,
                smoothingSeconds,
                Mathf.Infinity,
                deltaTimeSeconds);
            float currentZoomMeters = Mathf.SmoothDamp(
                motionState.GetCurrentZoomMeters(projectionType),
                motionState.TargetZoomMeters,
                ref _zoomVelocityMetersPerSecond,
                smoothingSeconds,
                Mathf.Infinity,
                deltaTimeSeconds);
            motionState.SetCurrentZoomMeters(projectionType, currentZoomMeters);
        }

        public void StopFocusSmoothing(CameraMotionState motionState)
        {
            motionState.TargetFocusPositionMeters = motionState.CurrentFocusPositionMeters;
            _focusSmoothVelocityMetersPerSecond = Vector3.zero;
        }

        #endregion
    }
}
