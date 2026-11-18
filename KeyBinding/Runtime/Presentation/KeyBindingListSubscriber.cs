using FakeMG.Framework.UI;
using FakeMG.InputContexts;
using FakeMG.InputHints;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;

namespace FakeMG.KeyBinding
{
    /// <remarks>
    /// Rebind messages name actions, not their keys: a pad button's name comes from whichever matching pad connected first,
    /// so with several brands connected it can name the wrong one. The rebound slots show the right glyph instead.
    /// </remarks>
    public sealed class KeyBindingListSubscriber : MonoBehaviour
    {
        private const string SWAPPED_MESSAGE_FORMAT = "{0} swapped bindings with {1}.";
        private const string TAKEN_MESSAGE_FORMAT = "{0} took that binding from {1}, which is now unbound.";
        private const string RESET_MESSAGE_FORMAT = "{0} bindings reset to defaults.";
        private const string SLOT_RESET_MESSAGE_FORMAT = "{0} reset to its default binding.";
        private const string LISTENING_MESSAGE_FORMAT = "{0} for {1}. {2} cancels.";
        private const string BOUND_MESSAGE_FORMAT = "{0} rebound.";
        private const string CANCELLED_MESSAGE = "Kept the previous binding.";

        [Header("Views")]
        [SerializeField] private KeyBindingListView _list;
        [SerializeField] private Button _resetButton;
        [SerializeField] private TransientStatusLabel _status;

        [Space]
        [Header("Input")]
        [Tooltip("The Controls page's own context, so Reset is offered only while the page is shown.")]
        [SerializeField] private InputContext _controlsContext;
        [SerializeField] private InputActionReference _resetActionReference;
        [Tooltip("Resets the selected binding only; a right click on a binding does the same.")]
        [SerializeField] private InputActionReference _resetSelectedActionReference;

        private KeyBindingConfigSO _configSO;
        private KeyBindingService _keyBindings;
        private ActiveGlyphSetTracker _glyphSetTracker;
        private BindingDeviceFamilySO _shownDeviceFamilySO;

        #region Unity Lifecycle

        private void OnEnable()
        {
            _list.OnSlotClicked += StartRebindingSlot;
            _list.OnSlotResetRequested += ResetSlotToDefault;
            _resetButton.onClick.AddListener(ResetShownBindingsToDefaults);
            _controlsContext.AddHandler(_resetActionReference, ResetShownBindingsToDefaults);
            _controlsContext.AddHandler(_resetSelectedActionReference, ResetSelectedSlotToDefault);
            _keyBindings.OnRebindStarted += ShowSlotListening;
            _keyBindings.OnRebindFinished += ShowRebindResult;
            _keyBindings.OnBindingsChanged += ShowAllCurrentKeys;
            _glyphSetTracker.OnGlyphsChanged += ShowBindingsOfActiveDevice;
            ShowBindingsOfActiveDevice();
        }

        /// <remarks>Leaving the page mid-rebind cancels it, so the input isn't captured behind the player's back.</remarks>
        private void OnDisable()
        {
            _keyBindings.CancelRebind();
            _list.OnSlotClicked -= StartRebindingSlot;
            _list.OnSlotResetRequested -= ResetSlotToDefault;
            _resetButton.onClick.RemoveListener(ResetShownBindingsToDefaults);
            _controlsContext.RemoveHandler(_resetActionReference, ResetShownBindingsToDefaults);
            _controlsContext.RemoveHandler(_resetSelectedActionReference, ResetSelectedSlotToDefault);
            _keyBindings.OnRebindStarted -= ShowSlotListening;
            _keyBindings.OnRebindFinished -= ShowRebindResult;
            _keyBindings.OnBindingsChanged -= ShowAllCurrentKeys;
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

        private void StartRebindingSlot(KeyBindingSlot slot)
        {
            _keyBindings.StartRebind(slot);
        }

        private void ResetShownBindingsToDefaults()
        {
            _keyBindings.ResetToDefaults(_shownDeviceFamilySO);
            _status.ShowConfirmation(string.Format(RESET_MESSAGE_FORMAT, _shownDeviceFamilySO.DisplayName));
        }

        private void ResetSelectedSlotToDefault()
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (selected && selected.TryGetComponent(out KeyBindingSlotView slotView) && slotView.Slot != null)
            {
                ResetSlotToDefault(slotView.Slot);
            }
        }

        private void ResetSlotToDefault(KeyBindingSlot slot)
        {
            if (_keyBindings.IsRebinding)
            {
                return;
            }

            KeyBindingSlot swappedSlot = _keyBindings.ResetToDefault(slot);
            ShowKeyChange(slot, swappedSlot, string.Format(SLOT_RESET_MESSAGE_FORMAT, slot.RebindableAction.Label));
        }

        private void ShowSlotListening(KeyBindingSlot slot)
        {
            BindingDeviceFamilySO deviceFamilySO = slot.DeviceFamilySO;
            _list.GetSlotView(slot).ShowListening(deviceFamilySO.ListeningPrompt);
            _status.ShowConfirmation(string.Format(
                LISTENING_MESSAGE_FORMAT,
                deviceFamilySO.ListeningPrompt,
                slot.RebindableAction.Label,
                deviceFamilySO.CancelControlName));
        }

        private void ShowRebindResult(KeyBindingRebindResult result)
        {
            if (result.WasCancelled)
            {
                _list.GetSlotView(result.ReboundSlot).ShowIdle();
                _status.ShowConfirmation(CANCELLED_MESSAGE);
                return;
            }

            string reboundMessage = string.Format(BOUND_MESSAGE_FORMAT, result.ReboundSlot.RebindableAction.Label);
            ShowKeyChange(result.ReboundSlot, result.SwappedSlot, reboundMessage);
        }

        /// <param name="swappedSlot">The slot that took the changed slot's old key; null when none did.</param>
        /// <param name="unswappedMessage">Shown when no other slot changed.</param>
        private void ShowKeyChange(KeyBindingSlot changedSlot, KeyBindingSlot swappedSlot, string unswappedMessage)
        {
            _list.GetSlotView(changedSlot).ShowKeyChanged();
            if (swappedSlot == null)
            {
                _status.ShowConfirmation(unswappedMessage);
                return;
            }

            _list.GetSlotView(swappedSlot).ShowKeyChanged();
            string messageFormat = swappedSlot.IsUnbound ? TAKEN_MESSAGE_FORMAT : SWAPPED_MESSAGE_FORMAT;
            _status.ShowConfirmation(string.Format(messageFormat, changedSlot.RebindableAction.Label, swappedSlot.RebindableAction.Label));
        }

        private void ShowAllCurrentKeys()
        {
            _list.ShowAllCurrentKeys();
        }

        #endregion
    }
}
