using FakeMG.Settings;
using UnityEngine;

namespace FakeMG.KeyBinding
{
    public sealed class BindingDiagramSubscriber : MonoBehaviour
    {
        [SerializeField] private SettingFocusTracker _focusTracker;
        [SerializeField] private KeyBindingListView _list;
        [SerializeField] private BindingDiagramView _diagram;

        #region Unity Lifecycle

        private void OnEnable()
        {
            _focusTracker.OnSelectedControlChanged += ShowBindingOfSelectedControl;
            _list.OnKeysShown += ShowBindingOfSelectedSlot;
            ShowBindingOfSelectedSlot();
        }

        private void OnDisable()
        {
            _focusTracker.OnSelectedControlChanged -= ShowBindingOfSelectedControl;
            _list.OnKeysShown -= ShowBindingOfSelectedSlot;
        }

        #endregion

        #region Private Methods

        /// <remarks>Also runs once the list shows new keys, so the picture follows rebinds, swaps, resets and device switches.</remarks>
        private void ShowBindingOfSelectedSlot()
        {
            ShowBindingOfSelectedControl(_focusTracker.SelectedControl);
        }

        private void ShowBindingOfSelectedControl(GameObject selectedControl)
        {
            KeyBindingSlot slot = selectedControl && selectedControl.TryGetComponent(out KeyBindingSlotView slotView) ? slotView.Slot : null;
            _diagram.Show(slot == null || slot.IsUnbound ? null : slot.ControlPath);
        }

        #endregion
    }
}
