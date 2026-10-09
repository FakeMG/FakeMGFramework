using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

namespace FakeMG.Framework
{
    public sealed class ContinuousRotation : MonoBehaviour
    {
        [FormerlySerializedAs("_raysRectTransform")]
        [SerializeField] private Transform _targetTransform;
        [SerializeField] private Vector3 _localRotationDegrees = new(0f, 0f, 360f);
        [Min(0.1f)]
        [SerializeField] private float _rotationDurationSeconds = 20f;

        private Tween _rotationTween;

        #region Unity Lifecycle

        private void OnEnable()
        {
            _rotationTween = _targetTransform.DOLocalRotate(_localRotationDegrees, _rotationDurationSeconds, RotateMode.LocalAxisAdd)
                .SetEase(Ease.Linear).SetLoops(-1).SetUpdate(true).SetLink(gameObject).OnKill(ClearRotationTween);
        }

        private void OnDisable()
        {
            _rotationTween?.Kill();
        }

        #endregion
        #region Private Methods

        private void ClearRotationTween()
        {
            _rotationTween = null;
        }

        #endregion
    }
}
