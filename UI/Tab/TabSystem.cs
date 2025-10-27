using System;
using System.Collections.Generic;
using FakeMG.Framework.UI.Tab.TabContentTransition;
using UnityEngine;

namespace FakeMG.Framework.UI.Tab
{
    public class TabSystem : MonoBehaviour
    {
        [SerializeField] protected List<TabData> _tabs = new();
        [SerializeField] protected int _defaultActiveTabIndex;
        [SerializeField] protected TabTransitionBase _transition;

        public event Action<int> OnTabChanged;

        private List<Action> _onTabSelectedEvents = new();
        private int _currentActiveTabIndex;
        public int CurrentTabIndex => _currentActiveTabIndex;

        private void Start()
        {
            InitializeTabs();

            if (IsValidTabIndex(_defaultActiveTabIndex))
            {
                ShowDefaultTabContent();
                _tabs[_defaultActiveTabIndex].TabButton.InstantlySelect();
                _currentActiveTabIndex = _defaultActiveTabIndex;
                _onTabSelectedEvents[_defaultActiveTabIndex]?.Invoke();
                OnTabChanged?.Invoke(_defaultActiveTabIndex);
            }
        }

        private void ShowDefaultTabContent()
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                if (i != _defaultActiveTabIndex)
                {
                    _transition.DeactivateTabContent(_tabs[i]);
                }
                else
                {
                    _transition.ActivateTabContent(_tabs[i]);
                }
            }
        }

        private void InitializeTabs()
        {
            // Initialize the events list to match the number of tabs
            _onTabSelectedEvents.Clear();
            for (int i = 0; i < _tabs.Count; i++)
            {
                _onTabSelectedEvents.Add(null);
            }

            for (int i = 0; i < _tabs.Count; i++)
            {
                int tabIndex = i;
                _tabs[i].TabButton.Button.onClick.AddListener(() => SwitchToTab(tabIndex));
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

            _transition.PlayTabTransitionAnimation(_tabs[previousTabIndex], _tabs[tabIndex], previousTabIndex, tabIndex);

            _currentActiveTabIndex = tabIndex;
            _onTabSelectedEvents[tabIndex]?.Invoke();
            OnTabChanged?.Invoke(tabIndex);
        }

        private void AnimateButtonSelectionChange(int previousTabIndex, int newTabIndex)
        {
            if (IsValidTabIndex(previousTabIndex))
            {
                _tabs[previousTabIndex].TabButton.AnimateDeselection();
            }

            if (IsValidTabIndex(newTabIndex))
            {
                _tabs[newTabIndex].TabButton.AnimateSelection();
            }
        }

        public bool IsValidTabIndex(int tabIndex)
        {
            return tabIndex >= 0 && tabIndex < _tabs.Count;
        }

        public int GetTabCount()
        {
            return _tabs.Count;
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