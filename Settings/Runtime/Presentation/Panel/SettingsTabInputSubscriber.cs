using FakeMG.ActionMapManagement;
using FakeMG.Framework.UI.Tab;
using FakeMG.InputContexts;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using VContainer;

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

        private IActionMapManager _actionMaps;

        #region Unity Lifecycle

        private void OnEnable()
        {
            _panelContext.AddHandler(_previousTabActionReference, ShowPreviousTab);
            _panelContext.AddHandler(_nextTabActionReference, ShowNextTab);
            _actionMaps.EnableActionMap(_settingsMenuActionMapSO.ActionMapName);
        }

        private void OnDisable()
        {
            _actionMaps.DisableActionMap(_settingsMenuActionMapSO.ActionMapName);
            _panelContext.RemoveHandler(_previousTabActionReference, ShowPreviousTab);
            _panelContext.RemoveHandler(_nextTabActionReference, ShowNextTab);
        }

        #endregion

        #region Public Methods

        [Inject]
        public void Construct(IActionMapManager actionMaps)
        {
            _actionMaps = actionMaps;
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

        /// <remarks>The selected object may sit on the page that was just hidden, which would leave navigation stranded.</remarks>
        private void FocusCurrentTabButton()
        {
            EventSystem.current.SetSelectedGameObject(_tabs.GetTabButton(_tabs.CurrentTabIndex).gameObject);
        }

        #endregion
    }
}
