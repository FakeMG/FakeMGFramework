using DG.Tweening;
using UnityEngine;

namespace FakeMG.FakeMGFramework.UI.Tab.TabButton
{
    public class TabButtonScale : TabButtonBase
    {
        [SerializeField] private RectTransform tabButton;
        [SerializeField] protected float animationDuration = 0.3f;

        public override void AnimateSelection()
        {
            tabButton.DOKill();
            
            Vector3 targetScale = Vector3.one * 1.2f;
            tabButton.DOScale(targetScale, animationDuration).SetEase(Ease.OutBounce).SetLink(tabButton.gameObject);
        }

        public override void AnimateDeselection()
        {
            tabButton.DOKill();
            
            Vector3 targetScale = Vector3.one;
            tabButton.DOScale(targetScale, animationDuration).SetEase(Ease.OutQuad).SetLink(tabButton.gameObject);
        }

        public override void InstantlySelect()
        {
            tabButton.DOKill();
            
            tabButton.localScale = Vector3.one * 1.2f;
        }
        
        public override void InstantlyDeselect()
        {
            tabButton.DOKill();
            
            tabButton.localScale = Vector3.one;
        }
    }
}