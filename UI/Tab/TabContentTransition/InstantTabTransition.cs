using System;

namespace FakeMG.FakeMGFramework.UI.Tab.TabContentTransition
{
    public class InstantTabTransition : TabTransitionBase
    {
        public override void PlayTabTransitionAnimation(
            TabData fromTab, TabData toTab, int fromIndex, int toIndex,
            Action onComplete = null)
        {
            SwitchTabInstantly(fromTab, toTab, onComplete);
        }

        public override void SwitchTabInstantly(TabData fromTab, TabData toTab, Action onComplete = null)
        {
            ActivateTabContent(toTab);
            DeactivateTabContent(fromTab);
            onComplete?.Invoke();
        }

        public override void ActivateTabContent(TabData tab)
        {
            tab.TabContent.gameObject.SetActive(true);
            tab.onTabSelected?.Invoke();
        }

        public override void DeactivateTabContent(TabData tab)
        {
            tab.TabContent.gameObject.SetActive(false);
        }
    }
}