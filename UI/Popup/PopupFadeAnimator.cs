using DG.Tweening;
using UnityEngine;

namespace FakeMG.Framework.UI.Popup
{
    public class PopupFadeAnimator : PopupAnimator
    {
        [Header("Animation Settings")]
        [SerializeField] private float animationDuration = 0.3f;
        [SerializeField] private Ease showEase = Ease.OutBack;
        [SerializeField] private Ease hideEase = Ease.InBack;

        protected override Sequence CreateShowSequence()
        {
            var sequence = DOTween.Sequence();
            sequence.Join(canvasGroup.DOFade(1f, animationDuration).SetLink(canvasGroup.gameObject));

            return sequence;
        }

        protected override Sequence CreateHideSequence()
        {
            var sequence = DOTween.Sequence();
            sequence.Join(canvasGroup.DOFade(0f, animationDuration).SetLink(canvasGroup.gameObject));

            return sequence;
        }

        protected override void ShowImmediate()
        {
            canvasGroup.alpha = 1f;
        }

        protected override void HideImmediate()
        {
            canvasGroup.alpha = 0f;
        }
    }
}