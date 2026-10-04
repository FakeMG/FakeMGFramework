using UnityEngine;

namespace FakeMG.Framework
{
    public class TargetFollower : MonoBehaviour
    {
        [Tooltip("The target transform to follow")]
        [SerializeField] private Transform _target;

        [Header("Follow Options")]
        [Tooltip("Whether to follow the target on the X axis")]
        [SerializeField] private bool _followX = true;

        [Tooltip("Whether to follow the target on the Y axis")]
        [SerializeField] private bool _followY = true;

        [Tooltip("Whether to follow the target on the Z axis")]
        [SerializeField] private bool _followZ = true;

        [SerializeField] private bool _relativePosition;

        [Header("Offset Values")]
        [Tooltip("Offset on the X axis")]
        [SerializeField] private float _offsetX;

        [Tooltip("Offset on the Y axis")]
        [SerializeField] private float _offsetY;

        [Tooltip("Offset on the Z axis")]
        [SerializeField] private float _offsetZ;

        [Tooltip("How smoothly to follow the target (0 = instant)")]
        [SerializeField] private float _smoothTime = 0.3f;

        private Vector3 _velocity = Vector3.zero;
        private Vector3 _relativePositionOffset;

        private void Start()
        {
            if (_target)
                _relativePositionOffset = transform.position - _target.position;
        }

        private void LateUpdate()
        {
            if (!_target)
                return;

            Vector3 targetPosition = _relativePosition ? _target.position + _relativePositionOffset : _target.position;

            if (_followX)
                targetPosition.x = _target.position.x + _offsetX;

            if (_followY)
                targetPosition.y = _target.position.y + _offsetY;

            if (_followZ)
                targetPosition.z = _target.position.z + _offsetZ;

            if (_relativePosition)
            {
                if (_followX)
                    targetPosition.x = _target.position.x + _relativePositionOffset.x + _offsetX;

                if (_followY)
                    targetPosition.y = _target.position.y + _relativePositionOffset.y + _offsetY;

                if (_followZ)
                    targetPosition.z = _target.position.z + _relativePositionOffset.z + _offsetZ;
            }

            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _velocity, _smoothTime);
        }
    }
}