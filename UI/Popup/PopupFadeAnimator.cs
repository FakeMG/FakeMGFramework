using DG.Tweening;
using UnityEngine;

namespace FakeMG.Framework.UI.Popup
{
    public class PopupFadeAnimator : PopupAnimator
    {
        [Header("Animation Settings")]
        [SerializeField] private float _animationDuration = 0.3f;

        protected override Sequence CreateShowSequence()
        {
            var sequence = DOTween.Sequence();
            sequence.Join(_canvasGroup.DOFade(1f, _animationDuration).SetLink(_canvasGroup.gameObject));

            return sequence;
        }

        protected override Sequence CreateHideSequence()
        {
            var sequence = DOTween.Sequence();
            sequence.Join(_canvasGroup.DOFade(0f, _animationDuration).SetLink(_canvasGroup.gameObject));

            return sequence;
        }

        protected override void ShowImmediate()
        {
            _canvasGroup.alpha = 1f;
        }

        protected override void HideImmediate()
        {
            _canvasGroup.alpha = 0f;
        }
    }
}