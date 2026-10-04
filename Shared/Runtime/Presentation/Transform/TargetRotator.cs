using UnityEngine;

namespace FakeMG.Framework
{
    public class TargetRotator : MonoBehaviour
    {
        [Tooltip("The target transform to match rotation with")]
        [SerializeField] private Transform _target;

        [Header("Rotation Options")]
        [Tooltip("Whether to rotate on the X axis")]
        [SerializeField] private bool _rotateX = true;

        [Tooltip("Whether to rotate on the Y axis")]
        [SerializeField] private bool _rotateY = true;

        [Tooltip("Whether to rotate on the Z axis")]
        [SerializeField] private bool _rotateZ = true;

        [SerializeField] private bool _relativeRotation;

        [Header("Offset Values")]
        [Tooltip("Rotation offset to apply after matching the target rotation")]
        [SerializeField] private Vector3 _rotationOffset;

        [Tooltip("How smoothly to rotate (0 = instant)")]
        [SerializeField] private float _smoothTime = 0.3f;

        private Vector3 _velocity = Vector3.zero;
        private Quaternion _relativeRotationOffset;

        private void Start()
        {
            if (_target)
                _relativeRotationOffset = Quaternion.Inverse(_target.rotation) * transform.rotation;
        }

        private void LateUpdate()
        {
            if (!_target)
                return;

            Quaternion targetRotation = _relativeRotation ? _target.rotation * _relativeRotationOffset : _target.rotation;
            Vector3 targetEuler = targetRotation.eulerAngles + _rotationOffset;
            Vector3 currentEuler = transform.localEulerAngles;

            if (!_rotateX)
                targetEuler.x = currentEuler.x;

            if (!_rotateY)
                targetEuler.y = currentEuler.y;

            if (!_rotateZ)
                targetEuler.z = currentEuler.z;

            transform.localEulerAngles = Vector3.SmoothDamp(currentEuler, targetEuler, ref _velocity, _smoothTime);
        }
    }
}
