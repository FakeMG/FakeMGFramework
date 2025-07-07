using DG.Tweening;
using UnityEngine;

namespace FakeMG.FakeMGFramework.UI.Tab.TabButton
{
    public class TabButtonSlide : TabButtonBase
    {
        [SerializeField] private RectTransform buttonIconRect;
        [SerializeField] private float buttonScaleMultiplier = 1.3f;
        [SerializeField] private float buttonYOffset = 50f;
        [SerializeField] private float animationDuration = 0.3f;
        [SerializeField] private Ease scaleEase = Ease.OutQuart;
        [SerializeField] private Ease moveYEase = Ease.OutBack;

        public override void AnimateSelection()
        {
            Vector3 targetScale = Vector3.one * buttonScaleMultiplier;
            float targetY = buttonYOffset;

            buttonIconRect.DOScale(targetScale, animationDuration).SetEase(scaleEase).SetLink(buttonIconRect.gameObject);
            buttonIconRect.DOAnchorPosY(targetY, animationDuration).SetEase(moveYEase).SetLink(buttonIconRect.gameObject);
        }

        public override void AnimateDeselection()
        {
            Vector3 targetScale = Vector3.one;
            float targetY = 0f;

            buttonIconRect.DOScale(targetScale, animationDuration).SetEase(scaleEase).SetLink(buttonIconRect.gameObject);
            buttonIconRect.DOAnchorPosY(targetY, animationDuration).SetEase(moveYEase).SetLink(buttonIconRect.gameObject);
        }

        public override void InstantlySelect()
        {
            buttonIconRect.localScale = Vector3.one * buttonScaleMultiplier;
            buttonIconRect.anchoredPosition = new Vector2(buttonIconRect.anchoredPosition.x, buttonYOffset);
        }
        
        public override void InstantlyDeselect()
        {
            buttonIconRect.localScale = Vector3.one;
            buttonIconRect.anchoredPosition = new Vector2(buttonIconRect.anchoredPosition.x, 0f);
        }
    }
}