using FakeMG.Framework;

namespace FakeMG.GodCamera
{
    /// <summary>
    /// Owns stepped yaw requests and smooths the camera toward the requested orientation.
    /// </summary>
    public sealed class CameraRotationMotion
    {
        private readonly CameraRotationStepper _rotationStepper;

        private float _yawVelocityDegreesPerSecond;

        public CameraRotationMotion(CameraRotationStepper rotationStepper)
        {
            _rotationStepper = rotationStepper;
        }

        #region Public Methods

        public void ApplyRotation(
            CameraMotionState motionState,
            CameraProfileSO profile,
            float deltaTimeSeconds)
        {
            motionState.CurrentYawDegrees = _rotationStepper.SmoothYawDegrees(
                motionState.CurrentYawDegrees,
                motionState.TargetYawDegrees,
                ref _yawVelocityDegreesPerSecond,
                profile.RotationSmoothingSeconds,
                deltaTimeSeconds);
        }

        public void QueueRotationStep(CameraMotionState motionState, CameraProfileSO profile, int direction)
        {
            if (profile.RotationMode == CameraRotationMode.Disabled)
            {
                Echo.Log("Camera rotation input ignored because the active profile disables rotation.");
                return;
            }

            motionState.TargetYawDegrees = _rotationStepper.CalculateNextTargetYawDegrees(
                motionState.TargetYawDegrees,
                direction,
                profile.RotationStepDegrees);
        }

        #endregion
    }
}
