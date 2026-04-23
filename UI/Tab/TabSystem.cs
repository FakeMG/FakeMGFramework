using System;
using System.Collections.Generic;
using FakeMG.Framework.UI.Tab.TabButton;
using FakeMG.Framework.UI.Tab.TabContentTransition;
using UnityEngine;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FakeMG.Framework.UI.Tab
{
    public class TabSystem : MonoBehaviour
    {
        [SerializeField] protected List<TabData> _tabs = new();
        [SerializeField] protected int _defaultActiveTabIndex;
        [SerializeField] protected TabTransitionBase _transition;
        [SerializeField] protected TabBarAnimatorBase _tabBarAnimator;

        private readonly List<Action> _onTabSelectedEvents = new();
        private int _currentActiveTabIndex = -1;

        public event Action<int> OnTabChanged;

        public int CurrentTabIndex => _currentActiveTabIndex;

        #region Unity Lifecycle
        private void Awake()
        {
            InitializeTabSelectedEvents();
        }

        private void OnEnable()
        {
            SubscribeToTabButtons();
        }

        private void Start()
        {
            SelectDefaultTab();
        }

        private void OnDisable()
        {
            UnsubscribeFromTabButtons();
        }
        #endregion

        #region Public Methods
        public void SubscribeToTabSelected(int tabIndex, Action action)
        {
            if (!IsValidTabIndex(tabIndex))
            {
                Debug.LogWarning($"Cannot subscribe to tab index {tabIndex}; configured tab count is {GetTabCount()}.", this);
                return;
            }

            _onTabSelectedEvents[tabIndex] += action;
        }

        public void UnsubscribeFromTabSelected(int tabIndex, Action action)
        {
            if (!IsValidTabIndex(tabIndex))
            {
                Debug.LogWarning($"Cannot unsubscribe from tab index {tabIndex}; configured tab count is {GetTabCount()}.", this);
                return;
            }

            _onTabSelectedEvents[tabIndex] -= action;
        }

        public void SwitchToTab(int tabIndex)
        {
            if (!IsValidTabIndex(tabIndex))
            {
                Debug.LogWarning($"Cannot switch to tab index {tabIndex}; configured tab count is {GetTabCount()}.", this);
                return;
            }

            if (tabIndex == _currentActiveTabIndex)
            {
                return;
            }

            if (!CanSwitchToTab(tabIndex))
            {
                return;
            }

            int previousTabIndex = _currentActiveTabIndex;

            AnimateButtonSelectionChange(previousTabIndex, tabIndex);
            AnimateBarSelectionChange(previousTabIndex, tabIndex);
            PlayTabContentTransition(previousTabIndex, tabIndex);

            _currentActiveTabIndex = tabIndex;
            InvokeTabSelected(tabIndex);
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
            if (GetTabCount() == 0)
            {
                Debug.LogWarning("Cannot switch to the next tab because no tabs are configured.", this);
                return;
            }

            int nextIndex = (_currentActiveTabIndex + 1) % GetTabCount();
            SwitchToTab(nextIndex);
        }

        public void PreviousTab()
        {
            if (GetTabCount() == 0)
            {
                Debug.LogWarning("Cannot switch to the previous tab because no tabs are configured.", this);
                return;
            }

            int previousIndex = (_currentActiveTabIndex - 1 + GetTabCount()) % GetTabCount();
            SwitchToTab(previousIndex);
        }

#if UNITY_EDITOR
        [Button("Gather Child Tab Buttons")]
        [ContextMenu("Gather Child Tab Buttons")]
        public void GatherChildTabButtons()
        {
            if (!TabButtonGatherer.TryGetChildTabButtons(this, out List<TabButtonBase> tabButtons))
            {
                return;
            }
            
            Undo.RecordObject(this, "Gather Child Tab Buttons");

            _tabs = TabButtonGatherer.BuildTabsPreservingContent(tabButtons, _tabs);
            InitializeTabSelectedEvents();
            EditorUtility.SetDirty(this);

            Debug.Log($"{nameof(TabSystem)} on {name} gathered {tabButtons.Count} child tab buttons in hierarchy order.", this);
        }
#endif
        #endregion

        #region Private Methods
        private void InitializeTabSelectedEvents()
        {
            _onTabSelectedEvents.Clear();

            for (int i = 0; i < _tabs.Count; i++)
            {
                _onTabSelectedEvents.Add(null);
            }
        }

        private void SubscribeToTabButtons()
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                if (!HasConfiguredTabButton(i))
                {
                    continue;
                }

                _tabs[i].TabButton.SelectionRequested += SwitchToRequestedTab;
            }
        }

        private void UnsubscribeFromTabButtons()
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                if (!HasConfiguredTabButton(i))
                {
                    continue;
                }

                _tabs[i].TabButton.SelectionRequested -= SwitchToRequestedTab;
            }
        }

        private void SelectDefaultTab()
        {
            if (!IsValidTabIndex(_defaultActiveTabIndex))
            {
                Debug.LogWarning($"Cannot select default tab index {_defaultActiveTabIndex}; configured tab count is {GetTabCount()}.", this);
                return;
            }

            if (!CanSwitchToTab(_defaultActiveTabIndex))
            {
                return;
            }

            ShowDefaultTabContent();
            ApplyDefaultButtonState();
            ApplyDefaultBarState();

            _currentActiveTabIndex = _defaultActiveTabIndex;
            InvokeTabSelected(_defaultActiveTabIndex);
        }

        private void ShowDefaultTabContent()
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                if (i == _defaultActiveTabIndex)
                {
                    _transition.ActivateTabContent(_tabs[i]);
                    continue;
                }

                if (!HasConfiguredTabContent(i))
                {
                    continue;
                }

                _transition.DeactivateTabContent(_tabs[i]);
            }
        }

        private void ApplyDefaultButtonState()
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                if (!HasConfiguredTabButton(i))
                {
                    continue;
                }

                if (i == _defaultActiveTabIndex)
                {
                    _tabs[i].TabButton.InstantlySelect();
                    continue;
                }

                _tabs[i].TabButton.InstantlyDeselect();
            }
        }

        private void ApplyDefaultBarState()
        {
            if (_tabBarAnimator == null)
            {
                return;
            }

            _tabBarAnimator.ApplyInstantState(_defaultActiveTabIndex);
        }

        private void SwitchToRequestedTab(TabButtonBase tabButton)
        {
            int tabIndex = GetTabIndex(tabButton);
            if (!IsValidTabIndex(tabIndex))
            {
                Debug.LogWarning($"Cannot switch tab because {tabButton.name} is not configured in this tab system.", this);
                return;
            }

            SwitchToTab(tabIndex);
        }

        private int GetTabIndex(TabButtonBase tabButton)
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                if (!HasConfiguredTabButton(i))
                {
                    continue;
                }

                if (_tabs[i].TabButton == tabButton)
                {
                    return i;
                }
            }

            return -1;
        }

        private void AnimateButtonSelectionChange(int previousTabIndex, int selectedTabIndex)
        {
            if (IsValidTabIndex(previousTabIndex))
            {
                _tabs[previousTabIndex].TabButton.AnimateDeselection();
            }

            _tabs[selectedTabIndex].TabButton.AnimateSelection();
        }

        private void AnimateBarSelectionChange(int previousTabIndex, int selectedTabIndex)
        {
            if (_tabBarAnimator == null)
            {
                return;
            }

            _tabBarAnimator.AnimateSelectionChange(previousTabIndex, selectedTabIndex);
        }

        private void PlayTabContentTransition(int previousTabIndex, int selectedTabIndex)
        {
            if (IsValidTabIndex(previousTabIndex))
            {
                _transition.PlayTabTransitionAnimation(_tabs[previousTabIndex], _tabs[selectedTabIndex], previousTabIndex, selectedTabIndex);
                return;
            }

            _transition.ActivateTabContent(_tabs[selectedTabIndex]);
        }

        private bool CanSwitchToTab(int selectedTabIndex)
        {
            bool canSwitch = HasConfiguredTransition();
            canSwitch &= HasConfiguredTabButton(selectedTabIndex);
            canSwitch &= HasConfiguredTabContent(selectedTabIndex);

            if (IsValidTabIndex(_currentActiveTabIndex))
            {
                canSwitch &= HasConfiguredTabButton(_currentActiveTabIndex);
                canSwitch &= HasConfiguredTabContent(_currentActiveTabIndex);
            }

            return canSwitch;
        }

        private bool HasConfiguredTransition()
        {
            if (_transition != null)
            {
                return true;
            }

            Debug.LogError($"{nameof(TabSystem)} on {name} requires a {nameof(TabTransitionBase)} reference.", this);
            return false;
        }

        private bool HasConfiguredTabButton(int tabIndex)
        {
            if (_tabs[tabIndex].TabButton != null)
            {
                return true;
            }

            Debug.LogError($"{nameof(TabSystem)} on {name} requires a tab button at index {tabIndex}.", this);
            return false;
        }

        private bool HasConfiguredTabContent(int tabIndex)
        {
            if (_tabs[tabIndex].TabContent != null)
            {
                return true;
            }

            Debug.LogError($"{nameof(TabSystem)} on {name} requires tab content at index {tabIndex}.", this);
            return false;
        }

        private void InvokeTabSelected(int tabIndex)
        {
            _onTabSelectedEvents[tabIndex]?.Invoke();
            OnTabChanged?.Invoke(tabIndex);
        }
        #endregion
    }
}
