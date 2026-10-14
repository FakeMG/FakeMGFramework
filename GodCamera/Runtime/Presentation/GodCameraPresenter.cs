using FakeMG.Framework;
using UnityEngine;
using VContainer.Unity;

namespace FakeMG.GodCamera
{
    public interface ICameraFocusRequests
    {
        void RequestFocus(Vector3 focusPositionMeters);
    }

    /// <summary>Reads engine input/projections and coordinates camera motion and rig presentation.</summary>
    public sealed class GodCameraPresenter : IInitializable, ITickable, System.IDisposable, ICameraFocusRequests
    {
        private readonly CameraInputSubscriber _cameraInputSubscriber;
        private readonly CameraRigView _cameraRigView;
        private readonly CameraPanMotion _panMotion;
        private readonly CameraZoomMotion _zoomMotion;
        private readonly CameraRotationMotion _rotationMotion;
        private readonly CameraMotionBounds _motionBounds;
        private readonly CameraZoomCalculator _zoomCalculator;
        private CameraMotionState _motionState;
        private bool _hasRequestedFocus;
        private Vector3 _requestedFocusMeters;

        #region Public Methods

        public GodCameraPresenter(
            CameraInputSubscriber cameraInputSubscriber,
            CameraRigView cameraRigView,
            CameraPanMotion panMotion,
            CameraZoomMotion zoomMotion,
            CameraRotationMotion rotationMotion,
            CameraMotionBounds motionBounds,
            CameraZoomCalculator zoomCalculator)
        {
            _cameraInputSubscriber = cameraInputSubscriber;
            _cameraRigView = cameraRigView;
            _panMotion = panMotion;
            _zoomMotion = zoomMotion;
            _rotationMotion = rotationMotion;
            _motionBounds = motionBounds;
            _zoomCalculator = zoomCalculator;
        }

        public void Initialize()
        {
            CameraProfileSO profileSO = _cameraRigView.CameraProfileSO;
            _motionState = new CameraMotionState(_cameraRigView.CreateInitialState(), profileSO.ProjectionType);
            _cameraInputSubscriber.OnPointerDragPanStarted += StartPointerDragPan;
            _cameraInputSubscriber.OnPointerDragPanStopped += StopPointerDragPan;
            _cameraInputSubscriber.OnRotationStepRequested += QueueRotationStep;
            _motionBounds.ClampInitialState(
                _motionState, profileSO, _cameraRigView.BoundsProvider.GetBoundsMeters(), _cameraRigView.Aspect);
            _cameraRigView.ApplyState(_motionState.CreateRigState());
        }

        public void Tick()
        {
            float deltaTimeSeconds = Time.unscaledDeltaTime;
            CameraProfileSO profileSO = _cameraRigView.CameraProfileSO;
            Bounds allowedBoundsMeters = _cameraRigView.BoundsProvider.GetBoundsMeters();
            float aspect = _cameraRigView.Aspect;
            if (_cameraInputSubscriber.ReadMoveInput().sqrMagnitude > 0.001f
                || _panMotion.IsPointerDragPanActive
                || Mathf.Abs(_cameraInputSubscriber.ReadControllerZoomInput()) > 0.001f)
            {
                _hasRequestedFocus = false;
            }

            _panMotion.ApplyDirectionalPan(
                _motionState,
                _cameraInputSubscriber.ReadMoveInput(),
                profileSO.PanSpeedMetersPerSecond,
                profileSO.PanSmoothingSeconds,
                deltaTimeSeconds);
            ApplyPointerDragPan(profileSO.DragPanSmoothingSeconds, deltaTimeSeconds);
            ApplyZoom(profileSO, allowedBoundsMeters, aspect, deltaTimeSeconds);
            _rotationMotion.ApplyRotation(_motionState, profileSO, deltaTimeSeconds);

            if (_hasRequestedFocus)
            {
                _motionState.TargetFocusPositionMeters = _requestedFocusMeters;
                _motionBounds.ClampTargetFocusPosition(_motionState, profileSO, allowedBoundsMeters, aspect);
                _motionState.CurrentFocusPositionMeters = Vector3.Lerp(_motionState.CurrentFocusPositionMeters,
                    _motionState.TargetFocusPositionMeters, 1f - Mathf.Exp(-deltaTimeSeconds / 0.25f));
                if ((_motionState.CurrentFocusPositionMeters - _motionState.TargetFocusPositionMeters).sqrMagnitude < 0.0025f)
                {
                    _hasRequestedFocus = false;
                }
            }
            _motionBounds.ClampFocusPositions(_motionState, profileSO, allowedBoundsMeters, aspect);
            _cameraRigView.ApplyState(_motionState.CreateRigState());
        }

        public void Dispose()
        {
            _cameraInputSubscriber.OnPointerDragPanStarted -= StartPointerDragPan;
            _cameraInputSubscriber.OnPointerDragPanStopped -= StopPointerDragPan;
            _cameraInputSubscriber.OnRotationStepRequested -= QueueRotationStep;
        }

        public void RequestFocus(Vector3 focusPositionMeters)
        {
            _requestedFocusMeters = focusPositionMeters;
            _hasRequestedFocus = true;
        }

        #endregion

        #region Private Methods

        private void ApplyPointerDragPan(float smoothingSeconds, float deltaTimeSeconds)
        {
            if (!_panMotion.IsPointerDragPanActive)
            {
                return;
            }

            if (!_cameraRigView.TryProjectScreenPointToGround(
                    _cameraInputSubscriber.ReadPointerPositionPixels(), out Vector3 groundPointMeters))
            {
                Echo.Warning("Cannot continue pointer camera drag because the pointer ray does not hit the ground plane.");
                return;
            }

            _panMotion.ApplyPointerDragPan(_motionState, groundPointMeters, smoothingSeconds, deltaTimeSeconds);
        }

        private void ApplyZoom(CameraProfileSO profileSO, Bounds allowedBoundsMeters, float aspect, float deltaTimeSeconds)
        {
            float controllerZoomDeltaMeters = _cameraInputSubscriber.ReadControllerZoomInput()
                * profileSO.ZoomSpeedMetersPerSecond * deltaTimeSeconds;
            float pointerZoomDeltaMeters = _cameraInputSubscriber.ConsumePointerZoomSteps() * profileSO.PointerZoomSizeStepMeters;
            float zoomDeltaMeters = controllerZoomDeltaMeters + pointerZoomDeltaMeters;

            if (!Mathf.Approximately(zoomDeltaMeters, 0f))
            {
                _hasRequestedFocus = false;
                float desiredZoomMeters = _zoomMotion.CalculateTargetZoomMeters(
                    _motionState, zoomDeltaMeters, profileSO.MinimumZoomMeters, profileSO.MaximumZoomMeters);
                if (_motionBounds.TryClampZoomMeters(
                        _motionState, profileSO, desiredZoomMeters, allowedBoundsMeters, aspect, out float clampedZoomMeters))
                {
                    desiredZoomMeters = clampedZoomMeters;
                }

                CameraZoomAnchorMode anchorMode = Mathf.Abs(pointerZoomDeltaMeters) > 0f
                    ? profileSO.MouseZoomAnchorMode
                    : profileSO.ControllerZoomAnchorMode;
                Vector3 anchorCorrectionMeters = CalculateAnchorCorrectionMeters(desiredZoomMeters, anchorMode);
                _zoomMotion.SetZoomTarget(_motionState, desiredZoomMeters, anchorCorrectionMeters);
                _motionBounds.ClampTargetFocusPosition(_motionState, profileSO, allowedBoundsMeters, aspect);
            }

            _zoomMotion.SmoothCurrentZoomState(
                _motionState, profileSO.ProjectionType, profileSO.ZoomSmoothingSeconds, deltaTimeSeconds);
        }

        private Vector3 CalculateAnchorCorrectionMeters(float targetZoomMeters, CameraZoomAnchorMode anchorMode)
        {
            if (anchorMode == CameraZoomAnchorMode.FocusPoint)
            {
                return Vector3.zero;
            }

            Vector2 screenPositionPixels = anchorMode == CameraZoomAnchorMode.ScreenCenter
                ? _cameraRigView.GetScreenCenterPixels()
                : _cameraInputSubscriber.ReadPointerPositionPixels();
            if (!_cameraRigView.TryProjectScreenPointToGround(screenPositionPixels, out Vector3 originalAnchorMeters))
            {
                Echo.Warning("Cannot anchor camera zoom because the current screen ray does not hit the ground plane.");
                return Vector3.zero;
            }

            if (!_cameraRigView.TryProjectScreenPointToGround(
                    screenPositionPixels,
                    _motionState.TargetFocusPositionMeters,
                    _motionState.CurrentYawDegrees,
                    targetZoomMeters,
                    out Vector3 targetAnchorMeters))
            {
                Echo.Warning("Cannot anchor camera zoom because the target screen ray does not hit the ground plane.");
                return Vector3.zero;
            }

            return _zoomCalculator.CalculateAnchorCorrectionMeters(originalAnchorMeters, targetAnchorMeters);
        }

        private void StartPointerDragPan()
        {
            _hasRequestedFocus = false;
            _zoomMotion.StopFocusSmoothing(_motionState);
            if (!_cameraRigView.TryProjectScreenPointToGround(
                    _cameraInputSubscriber.ReadPointerPositionPixels(), out Vector3 groundPointMeters))
            {
                Echo.Warning("Cannot start pointer camera drag because the pointer ray does not hit the ground plane.");
                return;
            }

            _panMotion.StartPointerDragPan(_motionState, groundPointMeters);
        }

        private void StopPointerDragPan()
        {
            _panMotion.StopPointerDragPan();
            _zoomMotion.StopFocusSmoothing(_motionState);
        }

        private void QueueRotationStep(int direction)
        {
            _hasRequestedFocus = false;
            _rotationMotion.QueueRotationStep(_motionState, _cameraRigView.CameraProfileSO, direction);
        }

        #endregion
    }
}
