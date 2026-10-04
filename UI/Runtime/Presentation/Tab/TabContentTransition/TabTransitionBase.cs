using System;
using DG.Tweening;
using UnityEngine;

namespace FakeMG.Framework.UI.Tab.TabContentTransition
{
    /// <summary>
    /// Base class for tab transition animators providing common functionality
    /// </summary>
    public abstract class TabTransitionBase : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] protected float _animationDuration = 0.3f;
        [SerializeField] protected Ease _animationEase = Ease.OutQuart;

        public float AnimationDuration => _animationDuration;

        public abstract void PlayTabTransitionAnimation(TabData fromTab, TabData toTab, int fromIndex, int toIndex, Action onComplete = null);
        public abstract void SwitchTabInstantly(TabData fromTab, TabData toTab, Action onComplete = null);
        public abstract void ActivateTabContent(TabData tab);
        public abstract void DeactivateTabContent(TabData tab);

        protected void StopTabContentAnimations(TabData tab)
        {
            tab.TabContent.DOKill();
        }
    }
}