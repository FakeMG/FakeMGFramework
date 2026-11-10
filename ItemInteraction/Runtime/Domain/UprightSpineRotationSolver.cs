using UnityEngine;

namespace FakeMG.ItemInteraction
{
    public sealed class UprightSpineRotationSolver
    {
        private const float MIN_HORIZONTAL_SQR_MAGNITUDE = 0.000001f;

        #region Public Methods

        public Quaternion GetUprightParentRotation(Quaternion parentWorldRotation)
        {
            Vector3 horizontalForward = Vector3.ProjectOnPlane(parentWorldRotation * Vector3.forward, Vector3.up);
            if (horizontalForward.sqrMagnitude < MIN_HORIZONTAL_SQR_MAGNITUDE)
            {
                Vector3 horizontalRight = Vector3.ProjectOnPlane(parentWorldRotation * Vector3.right, Vector3.up);
                horizontalForward = Vector3.Cross(horizontalRight.normalized, Vector3.up);
            }

            return Quaternion.LookRotation(horizontalForward.normalized, Vector3.up);
        }

        public Quaternion ResolveTargetWorldRotation(
            Quaternion parentWorldRotation,
            Quaternion animatedSpineLocalRotation,
            Vector3 rotationOffsetDegrees)
        {
            return ResolveTargetWorldRotation(parentWorldRotation, animatedSpineLocalRotation, Quaternion.Euler(rotationOffsetDegrees));
        }

        public Quaternion ResolveTargetWorldRotation(
            Quaternion parentWorldRotation,
            Quaternion animatedSpineLocalRotation,
            Quaternion rotationOffsetQuaternion)
        {
            return GetUprightParentRotation(parentWorldRotation) * rotationOffsetQuaternion * animatedSpineLocalRotation;
        }

        #endregion
    }
}
