using UnityEngine;

namespace FakeMG.GodCamera
{
    /// <summary>Owns pan smoothing and the ground point grabbed by a pointer drag.</summary>
    public sealed class CameraPanMotion
    {
        private readonly CameraPanCalculator _panCalculator;
        private Vector3 _panVelocityMetersPerSecond;
        private Vector3 _panSmoothVelocityMetersPerSecond;
        private Vector3 _pointerDragSmoothVelocityMetersPerSecond;
        private Vector3 _grabbedGroundPointMeters;

        public bool IsPointerDragPanActive { get; private set; }

        #region Public Methods

        public CameraPanMotion(CameraPanCalculator panCalculator) => _panCalculator = panCalculator;

        public void ApplyDirectionalPan(
            CameraMotionState motionState,
            Vector2 moveInput,
            float panSpeedMetersPerSecond,
            float smoothingSeconds,
            float deltaTimeSeconds)
        {
            Vector3 targetVelocityMetersPerSecond = _panCalculator.CalculateViewRelativeVelocityMetersPerSecond(
                moveInput, motionState.CurrentYawDegrees, panSpeedMetersPerSecond);
            _panVelocityMetersPerSecond = Vector3.SmoothDamp(
                _panVelocityMetersPerSecond,
                targetVelocityMetersPerSecond,
                ref _panSmoothVelocityMetersPerSecond,
                smoothingSeconds,
                Mathf.Infinity,
                deltaTimeSeconds);
            motionState.MoveCurrentAndTargetFocusPositionMeters(_panVelocityMetersPerSecond * deltaTimeSeconds);
        }

        public void ApplyPointerDragPan(
            CameraMotionState motionState,
            Vector3 currentGroundPointMeters,
            float smoothingSeconds,
            float deltaTimeSeconds)
        {
            if (!IsPointerDragPanActive)
            {
                return;
            }

            // Inverting ground-point displacement retains the grabbed world point under the cursor.
            Vector3 desiredFocusPositionMeters = motionState.CurrentFocusPositionMeters
                + _panCalculator.CalculateDragCorrectionMeters(_grabbedGroundPointMeters, currentGroundPointMeters);
            Vector3 smoothedFocusPositionMeters = Vector3.SmoothDamp(
                motionState.CurrentFocusPositionMeters,
                desiredFocusPositionMeters,
                ref _pointerDragSmoothVelocityMetersPerSecond,
                smoothingSeconds,
                Mathf.Infinity,
                deltaTimeSeconds);
            motionState.SetCurrentAndTargetFocusPositionMeters(smoothedFocusPositionMeters);
        }

        public void StartPointerDragPan(CameraMotionState motionState, Vector3 grabbedGroundPointMeters)
        {
            motionState.TargetFocusPositionMeters = motionState.CurrentFocusPositionMeters;
            _grabbedGroundPointMeters = grabbedGroundPointMeters;
            IsPointerDragPanActive = true;
        }

        public void StopPointerDragPan()
        {
            IsPointerDragPanActive = false;
            _pointerDragSmoothVelocityMetersPerSecond = Vector3.zero;
        }

        #endregion
    }
}
