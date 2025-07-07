using System.Collections.Generic;
using FakeMG.FakeMGFramework.UI.Tab.TabContentTransition;
using UnityEngine;
using UnityEngine.Events;

namespace FakeMG.FakeMGFramework.UI.Tab
{
    public class TabSystem : MonoBehaviour
    {
        [SerializeField] protected List<TabData> tabs = new();
        [SerializeField] protected int defaultActiveTabIndex;
        [SerializeField] protected TabTransitionBase transition;

        public UnityEvent<int> onTabChanged;

        private int _currentActiveTabIndex = -1;

        private void Start()
        {
            InitializeTabs();

            if (IsValidTabIndex(defaultActiveTabIndex))
            {
                ShowDefaultTabContent();
                tabs[defaultActiveTabIndex].onTabSelected?.Invoke();
                tabs[defaultActiveTabIndex].TabButton.InstantlySelect();
                _currentActiveTabIndex = defaultActiveTabIndex;
                onTabChanged?.Invoke(defaultActiveTabIndex);
            }
        }

        private void ShowDefaultTabContent()
        {
            for (int i = 0; i < tabs.Count; i++)
            {
                if (i != defaultActiveTabIndex)
                {
                    transition.DeactivateTabContent(tabs[i]);
                }
                else
                {
                    transition.ActivateTabContent(tabs[i]);
                }
            }
        }

        private void InitializeTabs()
        {
            for (int i = 0; i < tabs.Count; i++)
            {
                int tabIndex = i;
                if (tabs[i].TabButton != null)
                {
                    tabs[i].TabButton.onClick.AddListener(() => SwitchToTab(tabIndex));
                }
            }
        }

        public void SwitchToTab(int tabIndex)
        {
            if (!IsValidTabIndex(tabIndex) || tabIndex == _currentActiveTabIndex) return;

            int previousTabIndex = _currentActiveTabIndex;

            AnimateButtonSelectionChange(previousTabIndex, tabIndex);

            transition.PlayTabTransitionAnimation(tabs[previousTabIndex], tabs[tabIndex], previousTabIndex,
                tabIndex);

            _currentActiveTabIndex = tabIndex;
            onTabChanged?.Invoke(tabIndex);
        }

        private void AnimateButtonSelectionChange(int previousTabIndex, int newTabIndex)
        {
            if (IsValidTabIndex(previousTabIndex))
            {
                tabs[previousTabIndex].TabButton.AnimateDeselection();
            }

            if (IsValidTabIndex(newTabIndex))
            {
                tabs[newTabIndex].TabButton.AnimateSelection();
            }
        }

        public bool IsValidTabIndex(int tabIndex)
        {
            return tabIndex >= 0 && tabIndex < tabs.Count;
        }

        public int GetTabCount()
        {
            return tabs.Count;
        }

        public void NextTab()
        {
            if (GetTabCount() == 0) return;

            int nextIndex = (_currentActiveTabIndex + 1) % GetTabCount();
            SwitchToTab(nextIndex);
        }

        public void PreviousTab()
        {
            if (GetTabCount() == 0) return;

            int prevIndex = (_currentActiveTabIndex - 1 + GetTabCount()) % GetTabCount();
            SwitchToTab(prevIndex);
        }
    }
}