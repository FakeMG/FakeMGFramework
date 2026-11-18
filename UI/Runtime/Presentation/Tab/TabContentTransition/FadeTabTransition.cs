using System;
using DG.Tweening;
using UnityEngine;

namespace FakeMG.Framework.UI.Tab.TabContentTransition
{
    public class FadeTabTransition : TabTransitionBase
    {
        [Tooltip("Exit duration as a fraction of the entrance, so the outgoing tab clears out of the way quickly.")]
        [SerializeField, Range(0.1f, 1f)] private float _exitDurationRatio01 = 0.65f;

        public override void PlayTabTransitionAnimation(TabData fromTab, TabData toTab, int fromIndex, int toIndex, Action onComplete = null)
        {
            StopTabContentAnimations(fromTab);
            StopTabContentAnimations(toTab);

            var fromCanvasGroup = GetOrAddCanvasGroup(fromTab.TabContent);
            var toCanvasGroup = GetOrAddCanvasGroup(toTab.TabContent);
            // The fades target the canvas groups, so they must be killed there: a surviving fade-out would hide a tab the
            // player has just switched back to.
            fromCanvasGroup.DOKill();
            toCanvasGroup.DOKill();

            fromCanvasGroup.blocksRaycasts = false;
            fromCanvasGroup
                .DOFade(0f, _animationDuration * _exitDurationRatio01)
                .SetUpdate(true)
                .SetEase(Ease.InQuad)
                .SetLink(fromTab.TabContent.gameObject)
                .OnComplete(() =>
                {
                    fromTab.TabContent.gameObject.SetActive(false);
                });

            if (!toTab.TabContent.gameObject.activeSelf)
            {
                toCanvasGroup.alpha = 0f;
            }

            toTab.TabContent.gameObject.SetActive(true);
            toCanvasGroup.blocksRaycasts = true;
            toCanvasGroup
                .DOFade(1f, _animationDuration)
                .SetUpdate(true)
                .SetEase(_animationEase)
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
            canvasGroup.DOKill();
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        public override void DeactivateTabContent(TabData tab)
        {
            tab.TabContent.gameObject.SetActive(false);
            var canvasGroup = GetOrAddCanvasGroup(tab.TabContent);
            canvasGroup.DOKill();
            canvasGroup.alpha = 0f;
        }
    }
}
