using UnityEngine;

namespace FakeMG.Framework
{
    public class TargetFollower : MonoBehaviour
    {
        [Tooltip("The target transform to follow")]
        public Transform Target;

        [Header("Follow Options")]
        [Tooltip("Whether to follow the target on the X axis")]
        public bool FollowX = true;
        [Tooltip("Whether to follow the target on the Y axis")]
        public bool FollowY = true;
        [Tooltip("Whether to follow the target on the Z axis")]
        public bool FollowZ = true;

        [Header("Offset Values")]
        [Tooltip("Offset on the X axis")]
        public float OffsetX;
        [Tooltip("Offset on the Y axis")]
        public float OffsetY;
        [Tooltip("Offset on the Z axis")]
        public float OffsetZ;

        [Tooltip("How smoothly to follow the target (0 = instant)")]
        public float SmoothTime = 0.3f;

        private Vector3 _velocity = Vector3.zero;

        private void LateUpdate()
        {
            if (!Target)
                return;

            Vector3 targetPosition = transform.position;

            if (FollowX)
                targetPosition.x = Target.position.x + OffsetX;

            if (FollowY)
                targetPosition.y = Target.position.y + OffsetY;

            if (FollowZ)
                targetPosition.z = Target.position.z + OffsetZ;

            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _velocity, SmoothTime);
        }
    }
}