using UnityEngine;

namespace FakeMG.GodCamera
{
    /// <summary>
    /// Stores the authoritative current and target values shared by camera motion behaviors.
    /// </summary>
    public sealed class CameraMotionState
    {
        private CameraRigState _currentRigState;

        public Vector3 CurrentFocusPositionMeters
        {
            get => _currentRigState.FocusPositionMeters;
            set => _currentRigState.FocusPositionMeters = value;
        }

        public float CurrentYawDegrees
        {
            get => _currentRigState.YawDegrees;
            set => _currentRigState.YawDegrees = value;
        }

        public Vector3 TargetFocusPositionMeters { get; set; }
        public float TargetYawDegrees { get; set; }
        public float TargetZoomMeters { get; set; }

        public CameraMotionState(CameraRigState initialRigState, CameraProfileSO profile)
        {
            _currentRigState = initialRigState;
            TargetFocusPositionMeters = initialRigState.FocusPositionMeters;
            TargetYawDegrees = initialRigState.YawDegrees;
            TargetZoomMeters = profile.GetZoomMeters(initialRigState);
        }

        #region Public Methods

        public CameraRigState CreateRigState()
        {
            return _currentRigState;
        }

        public float GetCurrentZoomMeters(CameraProfileSO profile)
        {
            return profile.GetZoomMeters(_currentRigState);
        }

        public void SetCurrentZoomMeters(CameraProfileSO profile, float zoomMeters)
        {
            profile.SetZoomMeters(ref _currentRigState, zoomMeters);
        }

        public void MoveCurrentAndTargetFocusPositionMeters(Vector3 offsetMeters)
        {
            CurrentFocusPositionMeters += offsetMeters;
            TargetFocusPositionMeters += offsetMeters;
        }

        public void SetCurrentAndTargetFocusPositionMeters(Vector3 focusPositionMeters)
        {
            CurrentFocusPositionMeters = focusPositionMeters;
            TargetFocusPositionMeters = focusPositionMeters;
        }

        #endregion
    }
}
