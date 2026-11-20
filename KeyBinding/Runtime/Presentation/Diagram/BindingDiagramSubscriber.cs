using FakeMG.Settings;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FakeMG.KeyBinding
{
    /// <summary>Pictures the key of the selected binding slot.</summary>
    /// <remarks>
    /// Follows the list's <see cref="KeyBindingListView.OnBindingsDisplayed"/> rather than the service's events: a device
    /// switch hands the slot views other slots, and only the list knows when that is done, so listening to the service or
    /// the glyph tracker directly could picture the previous device's key.
    /// </remarks>
    public sealed class BindingDiagramSubscriber : MonoBehaviour
    {
        [Required, SerializeField] private SettingFocusTracker _focusTracker;
        [Required, SerializeField] private KeyBindingListView _list;
        [Required, SerializeField] private BindingDiagramView _diagram;

        #region Unity Lifecycle

        private void OnEnable()
        {
            _focusTracker.OnSelectedObjectChanged += ShowBindingOfSelectedObject;
            _list.OnBindingsDisplayed += ShowBindingOfSelectedSlot;
            ShowBindingOfSelectedSlot();
        }

        private void OnDisable()
        {
            _focusTracker.OnSelectedObjectChanged -= ShowBindingOfSelectedObject;
            _list.OnBindingsDisplayed -= ShowBindingOfSelectedSlot;
        }

        #endregion

        #region Private Methods

        private void ShowBindingOfSelectedSlot()
        {
            ShowBindingOfSelectedObject(_focusTracker.SelectedObject);
        }

        private void ShowBindingOfSelectedObject(GameObject selectedObject)
        {
            KeyBindingSlot slot = selectedObject && selectedObject.TryGetComponent(out KeyBindingSlotView slotView) ? slotView.Slot : null;
            _diagram.Show(slot == null || slot.IsUnbound ? null : slot.ControlPath);
        }

        #endregion
    }
}
