using DG.Tweening;
using UnityEngine;

namespace FakeMG.FakeMGFramework.UI.Popup
{
    public class PopupScaleAnimator : PopupAnimator
    {
        [Header("Animation Settings")]
        [SerializeField] private float animationDuration = 0.3f;
        [SerializeField] private Ease showEase = Ease.OutBack;
        [SerializeField] private Ease hideEase = Ease.InBack;
        [SerializeField] private Vector3 targetScale = Vector3.one;

        private readonly Vector3 _initialScale = Vector3.zero;

        protected override Sequence CreateShowSequence()
        {
            var sequence = DOTween.Sequence();
            sequence.Join(canvasGroup.transform.DOScale(targetScale, animationDuration)
                .SetEase(showEase)
                .SetLink(canvasGroup.gameObject));
            sequence.Join(canvasGroup.DOFade(1f, animationDuration).SetLink(canvasGroup.gameObject));

            return sequence;
        }

        protected override Sequence CreateHideSequence()
        {
            var sequence = DOTween.Sequence();
            sequence.Join(canvasGroup.transform.DOScale(_initialScale, animationDuration)
                .SetEase(hideEase)
                .SetLink(canvasGroup.gameObject));
            sequence.Join(canvasGroup.DOFade(0f, animationDuration)
                .SetLink(canvasGroup.gameObject)
                .SetDelay(animationDuration * 0.5f));

            return sequence;
        }

        protected override void ShowImmediate()
        {
            canvasGroup.gameObject.SetActive(true);
            canvasGroup.alpha = 1f;
            canvasGroup.transform.localScale = targetScale;
        }

        protected override void HideImmediate()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.transform.localScale = _initialScale;
            canvasGroup.gameObject.SetActive(false);
        }
    }
}