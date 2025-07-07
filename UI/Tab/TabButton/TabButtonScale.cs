using DG.Tweening;
using UnityEngine;

namespace FakeMG.FakeMGFramework.UI.Tab.TabButton
{
    public class TabButtonScale : TabButtonBase
    {
        [SerializeField] private CanvasGroup buttonBackground;
        [SerializeField] private RectTransform buttonIconRect;
        [SerializeField] protected float animationDuration = 0.3f;

        public override void AnimateSelection()
        {
            Vector3 targetScale = Vector3.one * 1.2f;
            buttonIconRect.DOScale(targetScale, animationDuration).SetEase(Ease.OutBounce).SetLink(buttonIconRect.gameObject);
        }

        public override void AnimateDeselection()
        {
            Vector3 targetScale = Vector3.one;
            buttonIconRect.DOScale(targetScale, animationDuration).SetEase(Ease.OutQuad).SetLink(buttonIconRect.gameObject);
        }

        public override void InstantlySelect()
        {
            buttonIconRect.localScale = Vector3.one * 1.2f;
            buttonBackground.alpha = 1f; // Ensure background is fully visible
        }
        
        public override void InstantlyDeselect()
        {
            buttonIconRect.localScale = Vector3.one;
            buttonBackground.alpha = 0.5f; // Ensure background is semi-transparent
        }
    }
}