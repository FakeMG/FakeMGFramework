using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FakeMG.Settings
{
    /// <summary>Highlights the row of the selected object and shows its details.</summary>
    /// <remarks>Polls the selection, as uGUI raises no event when it changes.</remarks>
    public sealed class SettingFocusTracker : MonoBehaviour
    {
        [Required, SerializeField] private SettingDetailsView _details;
        [Tooltip("Knows which row each selectable of the shown page belongs to.")]
        [Required, SerializeField] private SettingsTabNavigation _navigation;

        private ISettingRow _focusedRow;

        public GameObject SelectedObject { get; private set; }

        public event Action<GameObject> OnSelectedObjectChanged;

        #region Unity Lifecycle

        private void OnDisable()
        {
            SelectedObject = null;
            FocusRow(null);
        }

        private void Update()
        {
            FocusRowOfNewSelectionAndNotify();
        }

        #endregion

        #region Private Methods

        private void FocusRowOfNewSelectionAndNotify()
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;
            bool isHiddenSelection = selected && !selected.activeInHierarchy;
            if (selected == SelectedObject || isHiddenSelection)
            {
                return;
            }

            SelectedObject = selected;
            FocusRow(_navigation.FindRowOf(selected));
            OnSelectedObjectChanged?.Invoke(selected);
        }

        private void FocusRow(ISettingRow row)
        {
            if (row == _focusedRow)
            {
                return;
            }

            _focusedRow?.ShowUnfocused();
            _focusedRow = row;
            _focusedRow?.ShowFocused();
            _details.Show(_focusedRow);
        }

        #endregion
    }
}
