using UnityEngine;

namespace FakeMG.GodCamera
{
    /// <summary>
    /// Converts input vectors and pointer displacement into ground-plane pan movement.
    /// </summary>
    public class CameraPanCalculator
    {
        #region Public Methods

        public Vector3 CalculateViewRelativeVelocityMetersPerSecond(
            Vector2 input,
            float yawDegrees,
            float panSpeedMetersPerSecond)
        {
            Vector2 clampedInput = Vector2.ClampMagnitude(input, 1f);
            // Rotating the world basis by camera yaw makes input directions follow the current view orientation.
            Quaternion yawRotation = Quaternion.Euler(0f, yawDegrees, 0f);
            Vector3 right = yawRotation * Vector3.right;
            Vector3 forward = yawRotation * Vector3.forward;
            Vector3 direction = right * clampedInput.x + forward * clampedInput.y;

            return direction * panSpeedMetersPerSecond;
        }

        public Vector3 CalculateDragCorrectionMeters(Vector3 grabbedGroundPointMeters, Vector3 currentGroundPointMeters)
        {
            // The inverse displacement moves the camera until the current ray hit returns to the grabbed point.
            Vector3 correctionMeters = grabbedGroundPointMeters - currentGroundPointMeters;
            correctionMeters.y = 0f;
            return correctionMeters;
        }

        #endregion
    }
}
