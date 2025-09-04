using System;
using DG.Tweening;
using UnityEngine;

namespace FakeMG.Framework.UI.Tab.TabContentTransition
{
    public class ScaleTabTransition : TabTransitionBase
    {
        [Header("Scale Animation Settings")]
        [SerializeField] private float scaleDownMultiplier = 0.8f;
        [SerializeField, Tooltip("Controls the overshoot amount for the OutBack ease. Higher values = more overshoot.")]
        private float overshootScale = 1.1f;

        public override void PlayTabTransitionAnimation(TabData fromTab, TabData toTab, int fromIndex, int toIndex, Action onComplete = null)
        {
            StopTabContentAnimations(fromTab);
            StopTabContentAnimations(toTab);

            fromTab.TabContent.DOScale(scaleDownMultiplier, animationDuration)
                .SetEase(Ease.InQuad)
                .SetLink(fromTab.TabContent.gameObject)
                .OnComplete(() =>
                {
                    fromTab.TabContent.gameObject.SetActive(false);
                    fromTab.TabContent.localScale = Vector3.one; // Reset scale
                });

            toTab.TabContent.gameObject.SetActive(true);
            toTab.TabContent.localScale = Vector3.zero;
            toTab.TabContent.DOScale(Vector3.one, animationDuration)
                .SetEase(Ease.OutBack, overshootScale)
                .SetLink(toTab.TabContent.gameObject)
                .OnComplete(() =>
                {
                    onComplete?.Invoke();
                });
        }

        public override void SwitchTabInstantly(TabData fromTab, TabData toTab, Action onComplete = null)
        {
            StopTabContentAnimations(fromTab);
            StopTabContentAnimations(toTab);

            ActivateTabContent(toTab);
            DeactivateTabContent(fromTab);

            onComplete?.Invoke();
        }

        public override void ActivateTabContent(TabData tab)
        {
            tab.TabContent.gameObject.SetActive(true);
            tab.TabContent.localScale = Vector3.one;
        }

        public override void DeactivateTabContent(TabData tab)
        {
            tab.TabContent.gameObject.SetActive(false);
            tab.TabContent.localScale = Vector3.one * scaleDownMultiplier;
        }
    }
}