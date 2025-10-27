using UnityEngine;

namespace FakeMG.Framework
{
    public class TargetScaler : MonoBehaviour
    {
        [Tooltip("The target transform to match scale with")]
        public Transform Target;

        [Header("Scale Options")]
        [Tooltip("Whether to scale on the X axis")]
        public bool ScaleX = true;
        [Tooltip("Whether to scale on the Y axis")]
        public bool ScaleY = true;
        [Tooltip("Whether to scale on the Z axis")]
        public bool ScaleZ = true;

        public bool RelativeScale;

        [Tooltip("How smoothly to scale (0 = instant)")]
        public float SmoothTime = 0.3f;

        private Vector3 _velocity = Vector3.zero;

        private Vector3 _originalScale;
        private Vector3 _originalTargetScale;

        private void Start()
        {
            _originalScale = transform.localScale;
            _originalTargetScale = Target.localScale;
        }

        private void LateUpdate()
        {
            if (!Target)
                return;

            Vector3 targetScale = transform.localScale;

            if (ScaleX)
            {
                targetScale.x = Target.localScale.x;
                if (RelativeScale)
                    targetScale.x = _originalScale.x * Target.transform.localScale.x / _originalTargetScale.x;
            }

            if (ScaleY)
            {
                targetScale.y = Target.localScale.y;
                if (RelativeScale)
                    targetScale.y = _originalScale.y * Target.transform.localScale.y / _originalTargetScale.y;
            }

            if (ScaleZ)
            {
                targetScale.z = Target.localScale.z;
                if (RelativeScale)
                    targetScale.z = _originalScale.z * Target.transform.localScale.z / _originalTargetScale.z;
            }

            transform.localScale = Vector3.SmoothDamp(transform.localScale, targetScale, ref _velocity, SmoothTime);
        }
    }
}