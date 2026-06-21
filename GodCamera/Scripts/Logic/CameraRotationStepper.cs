using UnityEngine;

namespace FakeMG.GodCamera
{
    /// <summary>
    /// Calculates discrete yaw targets and interpolates correctly across the zero-degree boundary.
    /// </summary>
    public class CameraRotationStepper
    {
        #region Public Methods

        public float CalculateNextTargetYawDegrees(float currentTargetYawDegrees, int direction, float stepDegrees)
        {
            return currentTargetYawDegrees + direction * stepDegrees;
        }

        public float SmoothYawDegrees(
            float currentYawDegrees,
            float targetYawDegrees,
            ref float yawVelocityDegreesPerSecond,
            float smoothingSeconds,
            float deltaTimeSeconds)
        {
            // SmoothDampAngle follows the shortest wrapped arc, avoiding a long rotation across 0/360 degrees.
            return Mathf.SmoothDampAngle(
                currentYawDegrees,
                targetYawDegrees,
                ref yawVelocityDegreesPerSecond,
                smoothingSeconds,
                Mathf.Infinity,
                deltaTimeSeconds);
        }

        #endregion
    }
}
