using System;
using DG.Tweening;
using UnityEngine;

namespace FakeMG.Framework.UI.Tab.TabContentTransition
{
    public class SlidingTabTransition : TabTransitionBase
    {
        [SerializeField] private RectTransform canvasRectTransform;

        public override void PlayTabTransitionAnimation(TabData fromTab, TabData toTab, int fromIndex, int toIndex, Action onComplete = null)
        {
            // Calculate slide direction (positive = from right, negative = from left)
            int direction = toIndex > fromIndex ? 1 : -1;

            AnimateTabTransitionWithSlide(fromTab, toTab, direction, onComplete);
        }

        private void AnimateTabTransitionWithSlide(TabData currentTab, TabData newTab, int direction, Action onComplete = null)
        {
            // Get canvas width for calculating the slide amount
            float canvasWidth = canvasRectTransform.rect.width;

            StopTabContentAnimations(newTab);
            StopTabContentAnimations(currentTab);

            if (newTab.TabContent)
            {
                // Set up the new panel
                newTab.TabContent.gameObject.SetActive(true);
                var tabPosition = newTab.TabContent.anchoredPosition;
                Vector2 hiddenPosition = new Vector2(direction * canvasWidth, tabPosition.y);
                newTab.TabContent.anchoredPosition = hiddenPosition;

                // Animate new panel in
                newTab.TabContent.DOAnchorPosX(0, animationDuration)
                    .SetEase(animationEase)
                    .SetLink(newTab.TabContent.gameObject)
                    .OnComplete(() =>
                    {
                        onComplete?.Invoke();
                    });
            }

            if (currentTab.TabContent)
            {
                // Animate current panel out
                var tabPosition = currentTab.TabContent.anchoredPosition;
                currentTab.TabContent.DOAnchorPosX(-direction * canvasWidth, animationDuration)
                    .SetEase(animationEase)
                    .SetLink(currentTab.TabContent.gameObject)
                    .OnComplete(() =>
                    {
                        currentTab.TabContent.gameObject.SetActive(false);
                        currentTab.TabContent.anchoredPosition = tabPosition; // Reset position
                    });
            }
            else if (newTab.TabContent == null)
            {
                // If neither tab has content, still call the completion callback
                onComplete?.Invoke();
            }
        }

        public override void SwitchTabInstantly(TabData fromTab, TabData toTab, Action onComplete = null)
        {
            StopTabContentAnimations(toTab);
            StopTabContentAnimations(fromTab);

            ActivateTabContent(toTab);
            DeactivateTabContent(fromTab);

            onComplete?.Invoke();
        }

        public override void ActivateTabContent(TabData tab)
        {
            tab.TabContent.gameObject.SetActive(true);
            var tabPosition = tab.TabContent.anchoredPosition;
            tab.TabContent.anchoredPosition = new Vector2(0, tabPosition.y);
        }

        public override void DeactivateTabContent(TabData tab)
        {
            tab.TabContent.gameObject.SetActive(false);
        }
    }
}