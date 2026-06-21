using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.GodCamera
{
    /// <summary>
    /// Applies directional and pointer-drag pan movement to camera motion state.
    /// </summary>
    public sealed class CameraPanMotion
    {
        private readonly CameraInputSubscriber _cameraInputSubscriber;
        private readonly CameraRigView _cameraRigView;
        private readonly CameraPanCalculator _panCalculator;

        private Vector3 _panVelocityMetersPerSecond;
        private Vector3 _panSmoothVelocityMetersPerSecond;
        private Vector3 _pointerDragSmoothVelocityMetersPerSecond;
        private Vector3 _grabbedGroundPointMeters;
        private bool _isPointerDragPanActive;

        public CameraPanMotion(
            CameraInputSubscriber cameraInputSubscriber,
            CameraRigView cameraRigView,
            CameraPanCalculator panCalculator)
        {
            _cameraInputSubscriber = cameraInputSubscriber;
            _cameraRigView = cameraRigView;
            _panCalculator = panCalculator;
        }

        #region Public Methods

        public void ApplyDirectionalPan(
            CameraMotionState motionState,
            CameraProfileSO profile,
            float deltaTimeSeconds)
        {
            Vector2 moveInput = _cameraInputSubscriber.ReadMoveInput();
            Vector3 targetVelocityMetersPerSecond = _panCalculator.CalculateViewRelativeVelocityMetersPerSecond(
                moveInput,
                motionState.CurrentYawDegrees,
                profile.PanSpeedMetersPerSecond);

            _panVelocityMetersPerSecond = Vector3.SmoothDamp(
                _panVelocityMetersPerSecond,
                targetVelocityMetersPerSecond,
                ref _panSmoothVelocityMetersPerSecond,
                profile.PanSmoothingSeconds,
                Mathf.Infinity,
                deltaTimeSeconds);

            motionState.MoveCurrentAndTargetFocusPositionMeters(
                _panVelocityMetersPerSecond * deltaTimeSeconds);
        }

        public void ApplyPointerDragPan(
            CameraMotionState motionState,
            CameraProfileSO profile,
            float deltaTimeSeconds)
        {
            if (!_isPointerDragPanActive)
            {
                return;
            }

            if (!_cameraRigView.TryProjectScreenPointToGround(
                    _cameraInputSubscriber.ReadPointerPositionPixels(),
                    out Vector3 currentGroundPointMeters))
            {
                Echo.Warning("Cannot continue pointer camera drag because the pointer ray does not hit the ground plane.");
                return;
            }

            // Moving the focus by the inverse ground-point displacement keeps the grabbed world point under the cursor.
            Vector3 desiredFocusPositionMeters = motionState.CurrentFocusPositionMeters
                                                 + _panCalculator.CalculateDragCorrectionMeters(
                                                     _grabbedGroundPointMeters,
                                                     currentGroundPointMeters);
            Vector3 smoothedFocusPositionMeters = Vector3.SmoothDamp(
                motionState.CurrentFocusPositionMeters,
                desiredFocusPositionMeters,
                ref _pointerDragSmoothVelocityMetersPerSecond,
                profile.DragPanSmoothingSeconds,
                Mathf.Infinity,
                deltaTimeSeconds);

            motionState.SetCurrentAndTargetFocusPositionMeters(smoothedFocusPositionMeters);
        }

        public void StartPointerDragPan(CameraMotionState motionState)
        {
            motionState.TargetFocusPositionMeters = motionState.CurrentFocusPositionMeters;

            if (!_cameraRigView.TryProjectScreenPointToGround(
                    _cameraInputSubscriber.ReadPointerPositionPixels(),
                    out _grabbedGroundPointMeters))
            {
                Echo.Warning("Cannot start pointer camera drag because the pointer ray does not hit the ground plane.");
                return;
            }

            _isPointerDragPanActive = true;
        }

        public void StopPointerDragPan()
        {
            _isPointerDragPanActive = false;
            _pointerDragSmoothVelocityMetersPerSecond = Vector3.zero;
        }

        #endregion
    }
}
