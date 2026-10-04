using UnityEngine;

namespace FakeMG.Framework
{
    public class TargetScaler : MonoBehaviour
    {
        [Tooltip("The target transform to match scale with")]
        [SerializeField] private Transform _target;

        [Header("Scale Options")]
        [Tooltip("Whether to scale on the X axis")]
        [SerializeField] private bool _scaleX = true;

        [Tooltip("Whether to scale on the Y axis")]
        [SerializeField] private bool _scaleY = true;

        [Tooltip("Whether to scale on the Z axis")]
        [SerializeField] private bool _scaleZ = true;

        [SerializeField] private bool _relativeScale;

        [Tooltip("How smoothly to scale (0 = instant)")]
        [SerializeField] private float _smoothTime = 0.3f;

        private Vector3 _velocity = Vector3.zero;

        private Vector3 _originalScale;
        private Vector3 _originalTargetScale;
        private Vector3 _relativeScaleRatio;

        private void Start()
        {
            _originalScale = transform.localScale;
            _originalTargetScale = _target.localScale;

            _relativeScaleRatio = new Vector3(
                _originalTargetScale.x == 0f ? 1f : _originalScale.x / _originalTargetScale.x,
                _originalTargetScale.y == 0f ? 1f : _originalScale.y / _originalTargetScale.y,
                _originalTargetScale.z == 0f ? 1f : _originalScale.z / _originalTargetScale.z);
        }

        private void LateUpdate()
        {
            if (!_target)
                return;

            Vector3 targetScale = transform.localScale;

            if (_scaleX)
            {
                targetScale.x = _target.localScale.x;
                if (_relativeScale)
                    targetScale.x = _target.localScale.x * _relativeScaleRatio.x;
            }

            if (_scaleY)
            {
                targetScale.y = _target.localScale.y;
                if (_relativeScale)
                    targetScale.y = _target.localScale.y * _relativeScaleRatio.y;
            }

            if (_scaleZ)
            {
                targetScale.z = _target.localScale.z;
                if (_relativeScale)
                    targetScale.z = _target.localScale.z * _relativeScaleRatio.z;
            }

            transform.localScale = Vector3.SmoothDamp(transform.localScale, targetScale, ref _velocity, _smoothTime);
        }
    }
}