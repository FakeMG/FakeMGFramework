using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FakeMG.Settings
{
    /// <remarks>Polls the selection, as uGUI raises no event when it changes.</remarks>
    public sealed class SettingFocusTracker : MonoBehaviour
    {
        [SerializeField] private SettingDetailsView _details;

        private ISettingRow _focusedRow;

        public GameObject SelectedControl { get; private set; }

        public event Action<GameObject> OnSelectedControlChanged;

        #region Unity Lifecycle

        private void OnDisable()
        {
            SelectedControl = null;
            FocusRow(null);
        }

        private void Update()
        {
            FollowSelection();
        }

        #endregion

        #region Private Methods

        private void FollowSelection()
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;
            bool isHiddenSelection = selected && !selected.activeInHierarchy;
            if (selected == SelectedControl || isHiddenSelection)
            {
                return;
            }

            SelectedControl = selected;
            FocusRow(selected ? selected.GetComponentInParent<ISettingRow>() : null);
            OnSelectedControlChanged?.Invoke(selected);
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
