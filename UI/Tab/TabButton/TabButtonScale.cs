using DG.Tweening;
using UnityEngine;

namespace FakeMG.Framework.UI.Tab.TabButton
{
    public class TabButtonScale : TabButtonBase
    {
        [SerializeField] private RectTransform _tabButton;
        [SerializeField] protected float _animationDuration = 0.3f;

        public override void AnimateSelection()
        {
            _tabButton.DOKill();

            Vector3 targetScale = Vector3.one * 1.2f;
            _tabButton.DOScale(targetScale, _animationDuration).SetEase(Ease.OutBounce).SetLink(_tabButton.gameObject);
        }

        public override void AnimateDeselection()
        {
            _tabButton.DOKill();

            Vector3 targetScale = Vector3.one;
            _tabButton.DOScale(targetScale, _animationDuration).SetEase(Ease.OutQuad).SetLink(_tabButton.gameObject);
        }

        public override void InstantlySelect()
        {
            _tabButton.DOKill();

            _tabButton.localScale = Vector3.one * 1.2f;
        }

        public override void InstantlyDeselect()
        {
            _tabButton.DOKill();

            _tabButton.localScale = Vector3.one;
        }
    }
}