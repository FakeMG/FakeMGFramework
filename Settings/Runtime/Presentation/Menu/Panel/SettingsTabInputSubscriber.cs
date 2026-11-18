using FakeMG.ActionMapManagement;
using FakeMG.Framework.EventBus;
using FakeMG.Framework.UI.Tab;
using FakeMG.InputContexts;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace FakeMG.Settings
{
    public sealed class SettingsTabInputSubscriber : MonoBehaviour
    {
        [Header("Tabs")]
        [SerializeField] private TabSystem _tabs;

        [Space]
        [Header("Input")]
        [Tooltip("The settings panel's own context, so tab paging stops while a dialog covers it.")]
        [SerializeField] private InputContext _panelContext;
        [SerializeField] private InputActionReference _previousTabActionReference;
        [SerializeField] private InputActionReference _nextTabActionReference;
        [Tooltip("Holds the tab actions. Enabled only while the settings panel is open.")]
        [SerializeField] private ActionMapSO _settingsMenuActionMapSO;

        #region Unity Lifecycle

        private void OnEnable()
        {
            _panelContext.AddHandler(_previousTabActionReference, ShowPreviousTab);
            _panelContext.AddHandler(_nextTabActionReference, ShowNextTab);
            EventBus<EnableActionMapEvent>.Raise(new EnableActionMapEvent { ActionMap = _settingsMenuActionMapSO });
        }

        private void OnDisable()
        {
            EventBus<DisableActionMapEvent>.Raise(new DisableActionMapEvent { ActionMap = _settingsMenuActionMapSO });
            _panelContext.RemoveHandler(_previousTabActionReference, ShowPreviousTab);
            _panelContext.RemoveHandler(_nextTabActionReference, ShowNextTab);
        }

        #endregion

        #region Private Methods

        private void ShowPreviousTab()
        {
            _tabs.PreviousTab();
            FocusCurrentTabButton();
        }

        private void ShowNextTab()
        {
            _tabs.NextTab();
            FocusCurrentTabButton();
        }

        /// <remarks>The focused control may sit on the page that was just hidden, which would leave navigation stranded.</remarks>
        private void FocusCurrentTabButton()
        {
            EventSystem.current.SetSelectedGameObject(_tabs.GetTabButton(_tabs.CurrentTabIndex).gameObject);
        }

        #endregion
    }
}
