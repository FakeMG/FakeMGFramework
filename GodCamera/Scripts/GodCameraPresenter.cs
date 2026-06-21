using UnityEngine;
using VContainer.Unity;

namespace FakeMG.GodCamera
{
    /// <summary>
    /// Coordinates camera input commands, motion behaviors, bounds, and rig presentation each frame.
    /// </summary>
    public sealed class GodCameraPresenter : IInitializable, ITickable, System.IDisposable
    {
        private readonly CameraInputSubscriber _cameraInputSubscriber;
        private readonly CameraRigView _cameraRigView;
        private readonly CameraPanMotion _panMotion;
        private readonly CameraZoomMotion _zoomMotion;
        private readonly CameraRotationMotion _rotationMotion;
        private readonly CameraMotionBounds _motionBounds;

        private CameraMotionState _motionState;

        public GodCameraPresenter(
            CameraInputSubscriber cameraInputSubscriber,
            CameraRigView cameraRigView,
            CameraPanMotion panMotion,
            CameraZoomMotion zoomMotion,
            CameraRotationMotion rotationMotion,
            CameraMotionBounds motionBounds)
        {
            _cameraInputSubscriber = cameraInputSubscriber;
            _cameraRigView = cameraRigView;
            _panMotion = panMotion;
            _zoomMotion = zoomMotion;
            _rotationMotion = rotationMotion;
            _motionBounds = motionBounds;
        }

        #region Public Methods

        public void Initialize()
        {
            _motionState = new CameraMotionState(
                _cameraRigView.CreateInitialState(),
                _cameraRigView.CameraProfileSO);

            _cameraInputSubscriber.OnPointerDragPanStarted += StartPointerDragPan;
            _cameraInputSubscriber.OnPointerDragPanStopped += StopPointerDragPan;
            _cameraInputSubscriber.OnRotationStepRequested += QueueRotationStep;

            _motionBounds.ClampInitialState(_motionState);
            _cameraRigView.ApplyState(_motionState.CreateRigState());
        }

        public void Tick()
        {
            float deltaTimeSeconds = Time.deltaTime;
            CameraProfileSO profile = _cameraRigView.CameraProfileSO;
            Bounds allowedBoundsMeters = _cameraRigView.BoundsProvider.GetBoundsMeters();

            _panMotion.ApplyDirectionalPan(_motionState, profile, deltaTimeSeconds);
            _panMotion.ApplyPointerDragPan(_motionState, profile, deltaTimeSeconds);
            _zoomMotion.ApplyZoom(_motionState, profile, allowedBoundsMeters, deltaTimeSeconds);
            _rotationMotion.ApplyRotation(_motionState, profile, deltaTimeSeconds);
            _motionBounds.ClampFocusPositions(_motionState, profile, allowedBoundsMeters);

            _cameraRigView.ApplyState(_motionState.CreateRigState());
        }

        public void Dispose()
        {
            _cameraInputSubscriber.OnPointerDragPanStarted -= StartPointerDragPan;
            _cameraInputSubscriber.OnPointerDragPanStopped -= StopPointerDragPan;
            _cameraInputSubscriber.OnRotationStepRequested -= QueueRotationStep;
        }

        #endregion

        #region Private Methods

        private void StartPointerDragPan()
        {
            _zoomMotion.StopFocusSmoothing(_motionState);
            _panMotion.StartPointerDragPan(_motionState);
        }

        private void StopPointerDragPan()
        {
            _panMotion.StopPointerDragPan();
            _zoomMotion.StopFocusSmoothing(_motionState);
        }

        private void QueueRotationStep(int direction)
        {
            _rotationMotion.QueueRotationStep(
                _motionState,
                _cameraRigView.CameraProfileSO,
                direction);
        }

        #endregion
    }
}
