using FakeMG.Framework.UI;
using FakeMG.InputContexts;
using FakeMG.InputHints;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;

namespace FakeMG.KeyBinding
{
    /// <summary>Wires the key binding list, its slots and its reset controls to the key binding service.</summary>
    public sealed class KeyBindingListSubscriber : MonoBehaviour
    {
        [Header("Views")]
        [Required, SerializeField] private KeyBindingListView _list;
        [Required, SerializeField] private Button _resetButton;
        [Required, SerializeField] private TransientStatusLabel _status;

        [Space]
        [Header("Input")]
        [Tooltip("The Controls page's own context, so Reset is offered only while the page is shown.")]
        [Required, SerializeField] private InputContext _controlsContext;
        [Required, SerializeField] private InputActionReference _resetActionReference;
        [Tooltip("Resets the selected binding only; a right click on a binding does the same.")]
        [Required, SerializeField] private InputActionReference _resetSelectedActionReference;

        private KeyBindingConfigSO _configSO;
        private KeyBindingService _keyBindings;
        private ActiveGlyphSetTracker _glyphSetTracker;
        private BindingDeviceFamilySO _shownDeviceFamilySO;

        #region Unity Lifecycle

        /// <remarks>Shows the bindings first: that builds the slots on the first open, so their events exist to subscribe to.</remarks>
        private void OnEnable()
        {
            ShowBindingsOfActiveDevice();
            foreach (KeyBindingSlotView slotView in _list.SlotViews)
            {
                slotView.OnClicked += StartRebindingSlot;
                slotView.OnNavigatedTo += _list.ScrollTo;
                slotView.OnResetRequested += ResetSlotToDefault;
            }

            _resetButton.onClick.AddListener(ResetShownBindingsToDefaults);
            _controlsContext.AddHandler(_resetActionReference, ResetShownBindingsToDefaults);
            _controlsContext.AddHandler(_resetSelectedActionReference, ResetSelectedSlotToDefault);
            _keyBindings.OnRebindStarted += ShowSlotRebinding;
            _keyBindings.OnRebindFinished += ShowRebindResult;
            _keyBindings.OnBindingsChanged += _list.ShowAllCurrentKeys;
            _glyphSetTracker.OnGlyphsChanged += ShowBindingsOfActiveDevice;
        }

        /// <remarks>Leaving the page mid-rebind cancels it, so the input isn't captured behind the player's back.</remarks>
        private void OnDisable()
        {
            _keyBindings.CancelRebind();
            foreach (KeyBindingSlotView slotView in _list.SlotViews)
            {
                slotView.OnClicked -= StartRebindingSlot;
                slotView.OnNavigatedTo -= _list.ScrollTo;
                slotView.OnResetRequested -= ResetSlotToDefault;
            }

            _resetButton.onClick.RemoveListener(ResetShownBindingsToDefaults);
            _controlsContext.RemoveHandler(_resetActionReference, ResetShownBindingsToDefaults);
            _controlsContext.RemoveHandler(_resetSelectedActionReference, ResetSelectedSlotToDefault);
            _keyBindings.OnRebindStarted -= ShowSlotRebinding;
            _keyBindings.OnRebindFinished -= ShowRebindResult;
            _keyBindings.OnBindingsChanged -= _list.ShowAllCurrentKeys;
            _glyphSetTracker.OnGlyphsChanged -= ShowBindingsOfActiveDevice;
        }

        #endregion

        #region Public Methods

        [Inject]
        public void Construct(KeyBindingConfigSO configSO, KeyBindingService keyBindings, ActiveGlyphSetTracker glyphSetTracker)
        {
            _configSO = configSO;
            _keyBindings = keyBindings;
            _glyphSetTracker = glyphSetTracker;
        }

        #endregion

        #region Private Methods

        /// <remarks>A rebind in progress belongs to the family being replaced, so it is cancelled before the slots change.</remarks>
        private void ShowBindingsOfActiveDevice()
        {
            BindingDeviceFamilySO deviceFamilySO = _configSO.FindDeviceFamilyDrawnWith(_glyphSetTracker.ActiveSetSO);
            if (deviceFamilySO != _shownDeviceFamilySO)
            {
                _keyBindings.CancelRebind();
            }

            bool isListFocused = _list.Contains(EventSystem.current.currentSelectedGameObject);
            _shownDeviceFamilySO = deviceFamilySO;
            _list.ShowDeviceFamily(deviceFamilySO, _glyphSetTracker.ActiveSetSO);
            if (isListFocused)
            {
                KeepSelectionInList();
            }
        }

        /// <remarks>Hiding the selected row clears the selection, which would strand keyboard and gamepad navigation.</remarks>
        private void KeepSelectionInList()
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (selected && selected.activeInHierarchy)
            {
                return;
            }

            KeyBindingSlotView firstShownSlotView = _list.FindFirstShownSlotView();
            EventSystem.current.SetSelectedGameObject(firstShownSlotView ? firstShownSlotView.gameObject : null);
        }

        private void StartRebindingSlot(KeyBindingSlotView slotView)
        {
            _keyBindings.StartRebind(slotView.Slot);
        }

        private void ResetShownBindingsToDefaults()
        {
            _keyBindings.ResetToDefaults(_shownDeviceFamilySO);
            _status.ShowConfirmation(KeyBindingStatusMessages.ForFamilyReset(_shownDeviceFamilySO));
        }

        private void ResetSelectedSlotToDefault()
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (selected && selected.TryGetComponent(out KeyBindingSlotView slotView) && slotView.Slot != null)
            {
                ResetSlotToDefault(slotView);
            }
        }

        /// <remarks>Needs no rebind guard: every action map, the UI's included, is suspended while a rebind listens.</remarks>
        private void ResetSlotToDefault(KeyBindingSlotView slotView)
        {
            KeyBindingSlot slot = slotView.Slot;
            KeyBindingSlot swappedSlot = _keyBindings.ResetToDefault(slot);
            ShowKeysChanged(slot, swappedSlot);
            _status.ShowConfirmation(KeyBindingStatusMessages.ForSlotReset(slot, swappedSlot));
        }

        private void ShowSlotRebinding(KeyBindingSlot slot)
        {
            _list.GetSlotView(slot).ShowRebinding(slot.DeviceFamilySO.RebindPrompt);
            _status.ShowConfirmation(KeyBindingStatusMessages.ForRebindStarted(slot));
        }

        private void ShowRebindResult(KeyBindingRebindResult result)
        {
            if (result.WasCancelled)
            {
                _list.GetSlotView(result.ReboundSlot).ShowIdle();
            }
            else
            {
                ShowKeysChanged(result.ReboundSlot, result.SwappedSlot);
            }

            _status.ShowConfirmation(KeyBindingStatusMessages.ForRebindResult(result));
        }

        /// <param name="swappedSlot">The slot that took the changed slot's old key; null when none did.</param>
        private void ShowKeysChanged(KeyBindingSlot changedSlot, KeyBindingSlot swappedSlot)
        {
            _list.GetSlotView(changedSlot).ShowKeyChanged();
            if (swappedSlot != null)
            {
                _list.GetSlotView(swappedSlot).ShowKeyChanged();
            }
        }

        #endregion
    }
}
