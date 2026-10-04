using DG.Tweening;
using UnityEngine;

namespace FakeMG.Framework.UI.Popup
{
    public class PopupScaleAnimator : PopupAnimator
    {
        [Header("Animation Settings")]
        [SerializeField] private float _animationDuration = 0.3f;
        [SerializeField] private Ease _showEase = Ease.OutBack;
        [SerializeField] private Ease _hideEase = Ease.InBack;
        [SerializeField] private Vector3 _targetScale = Vector3.one;

        private readonly Vector3 _initialScale = Vector3.zero;

        protected override Sequence CreateShowSequence()
        {
            var sequence = DOTween.Sequence();
            sequence.Join(_canvasGroup.transform.DOScale(_targetScale, _animationDuration)
                .SetEase(_showEase)
                .SetLink(_canvasGroup.gameObject));
            sequence.Join(_canvasGroup.DOFade(1f, _animationDuration).SetLink(_canvasGroup.gameObject));

            return sequence;
        }

        protected override Sequence CreateHideSequence()
        {
            var sequence = DOTween.Sequence();
            sequence.Join(_canvasGroup.transform.DOScale(_initialScale, _animationDuration)
                .SetEase(_hideEase)
                .SetLink(_canvasGroup.gameObject));
            sequence.Join(_canvasGroup.DOFade(0f, _animationDuration)
                .SetLink(_canvasGroup.gameObject)
                .SetDelay(_animationDuration * 0.5f));

            return sequence;
        }

        protected override void ShowImmediate()
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.transform.localScale = _targetScale;
        }

        protected override void HideImmediate()
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.transform.localScale = _initialScale;
        }
    }
}