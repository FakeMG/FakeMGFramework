using UnityEngine;

namespace FakeMG.FakeMGFramework
{
    public class TargetScaler : MonoBehaviour
    {
        [Tooltip("The target transform to match scale with")]
        public Transform target;

        [Header("Scale Options")]
        [Tooltip("Whether to scale on the X axis")]
        public bool scaleX = true;
        [Tooltip("Whether to scale on the Y axis")]
        public bool scaleY = true;
        [Tooltip("Whether to scale on the Z axis")]
        public bool scaleZ = true;

        public bool relativeScale;

        [Tooltip("How smoothly to scale (0 = instant)")]
        public float smoothTime = 0.3f;

        private Vector3 _velocity = Vector3.zero;

        private Vector3 _originalScale;
        private Vector3 _originalTargetScale;

        private void Start()
        {
            _originalScale = transform.localScale;
            _originalTargetScale = target.localScale;
        }

        private void LateUpdate()
        {
            if (!target)
                return;

            Vector3 targetScale = transform.localScale;

            if (scaleX)
            {
                targetScale.x = target.localScale.x;
                if (relativeScale)
                    targetScale.x = _originalScale.x * target.transform.localScale.x / _originalTargetScale.x;
            }

            if (scaleY)
            {
                targetScale.y = target.localScale.y;
                if (relativeScale)
                    targetScale.y = _originalScale.y * target.transform.localScale.y / _originalTargetScale.y;
            }

            if (scaleZ)
            {
                targetScale.z = target.localScale.z;
                if (relativeScale)
                    targetScale.z = _originalScale.z * target.transform.localScale.z / _originalTargetScale.z;
            }

            transform.localScale = Vector3.SmoothDamp(transform.localScale, targetScale, ref _velocity, smoothTime);
        }
    }
}