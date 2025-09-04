using System;
using System.Collections.Generic;
using FakeMG.Framework.UI.Tab.TabContentTransition;
using UnityEngine;

namespace FakeMG.Framework.UI.Tab
{
    public class TabSystem : MonoBehaviour
    {
        [SerializeField] protected List<TabData> tabs = new();
        [SerializeField] protected int defaultActiveTabIndex;
        [SerializeField] protected TabTransitionBase transition;

        public event Action<int> OnTabChanged;

        private List<Action> _onTabSelectedEvents = new();
        private int _currentActiveTabIndex;

        private void Start()
        {
            InitializeTabs();

            if (IsValidTabIndex(defaultActiveTabIndex))
            {
                ShowDefaultTabContent();
                tabs[defaultActiveTabIndex].TabButton.InstantlySelect();
                _currentActiveTabIndex = defaultActiveTabIndex;
                _onTabSelectedEvents[defaultActiveTabIndex]?.Invoke();
                OnTabChanged?.Invoke(defaultActiveTabIndex);
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
            // Initialize the events list to match the number of tabs
            _onTabSelectedEvents.Clear();
            for (int i = 0; i < tabs.Count; i++)
            {
                _onTabSelectedEvents.Add(null);
            }

            for (int i = 0; i < tabs.Count; i++)
            {
                int tabIndex = i;
                tabs[i].TabButton.button.onClick.AddListener(() => SwitchToTab(tabIndex));
            }
        }

        public void SubscribeToTabSelected(int tabIndex, Action action)
        {
            if (!IsValidTabIndex(tabIndex)) return;

            _onTabSelectedEvents[tabIndex] += action;
        }

        public void UnsubscribeFromTabSelected(int tabIndex, Action action)
        {
            if (!IsValidTabIndex(tabIndex)) return;

            _onTabSelectedEvents[tabIndex] -= action;
        }

        public void SwitchToTab(int tabIndex)
        {
            if (!IsValidTabIndex(tabIndex) || tabIndex == _currentActiveTabIndex) return;

            int previousTabIndex = _currentActiveTabIndex;

            AnimateButtonSelectionChange(previousTabIndex, tabIndex);

            transition.PlayTabTransitionAnimation(tabs[previousTabIndex], tabs[tabIndex], previousTabIndex, tabIndex);

            _currentActiveTabIndex = tabIndex;
            _onTabSelectedEvents[tabIndex]?.Invoke();
            OnTabChanged?.Invoke(tabIndex);
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