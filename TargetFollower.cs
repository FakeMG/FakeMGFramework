using UnityEngine;

namespace FakeMG {
    public class TargetFollower : MonoBehaviour {
        [Tooltip("The target transform to follow")]
        public Transform target;

        [Header("Follow Options")]
        [Tooltip("Whether to follow the target on the X axis")]
        public bool followX = true;
        [Tooltip("Whether to follow the target on the Y axis")]
        public bool followY = true;
        [Tooltip("Whether to follow the target on the Z axis")]
        public bool followZ = true;
        
        [Header("Offset Values")]
        [Tooltip("Offset on the X axis")]
        public float offsetX;
        [Tooltip("Offset on the Y axis")]
        public float offsetY;
        [Tooltip("Offset on the Z axis")]
        public float offsetZ;

        [Tooltip("How smoothly to follow the target (0 = instant)")]
        public float smoothTime = 0.3f;

        private Vector3 _velocity = Vector3.zero;

        private void LateUpdate() {
            if (!target)
                return;

            Vector3 targetPosition = transform.position;

            if (followX)
                targetPosition.x = target.position.x + offsetX;
            
            if (followY)
                targetPosition.y = target.position.y + offsetY;
            
            if (followZ)
                targetPosition.z = target.position.z + offsetZ;

            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _velocity, smoothTime);
        }
    }
}
