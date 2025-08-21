using System;
using DG.Tweening;
using UnityEngine;

namespace FakeMG.FakeMGFramework.UI.Tab.TabContentTransition
{
    public class FadeTabTransition : TabTransitionBase
    {
        public override void PlayTabTransitionAnimation(TabData fromTab, TabData toTab, int fromIndex, int toIndex, Action onComplete = null)
        {
            StopTabContentAnimations(fromTab);
            StopTabContentAnimations(toTab);

            // Start fade out of current tab
            var fromCanvasGroup = GetOrAddCanvasGroup(fromTab.TabContent);
            fromCanvasGroup.DOFade(0f, animationDuration)
                .SetEase(animationEase)
                .SetLink(fromTab.TabContent.gameObject)
                .OnComplete(() =>
                {
                    fromTab.TabContent.gameObject.SetActive(false);
                });

            // Start fade in of new tab after a short delay
            toTab.TabContent.gameObject.SetActive(true);
            var toCanvasGroup = GetOrAddCanvasGroup(toTab.TabContent);
            toCanvasGroup.alpha = 0f;
            toCanvasGroup.DOFade(1f, animationDuration)
                .SetEase(animationEase)
                .SetLink(toTab.TabContent.gameObject)
                .OnComplete(() =>
                {
                    onComplete?.Invoke();
                });
        }

        private CanvasGroup GetOrAddCanvasGroup(RectTransform rectTransform)
        {
            var canvasGroup = rectTransform.GetComponent<CanvasGroup>();
            if (!canvasGroup)
            {
                canvasGroup = rectTransform.gameObject.AddComponent<CanvasGroup>();
            }
            return canvasGroup;
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
            var canvasGroup = GetOrAddCanvasGroup(tab.TabContent);
            canvasGroup.alpha = 1f;
        }

        public override void DeactivateTabContent(TabData tab)
        {
            tab.TabContent.gameObject.SetActive(false);
            var canvasGroup = GetOrAddCanvasGroup(tab.TabContent);
            canvasGroup.alpha = 0f;
        }
    }
}
