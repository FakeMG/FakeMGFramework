using System.Collections.Generic;
using FakeMG.Settings;
using TMPro;
using UnityEngine;

namespace FakeMG.KeyBinding
{
    public sealed class KeyBindingRowView : MonoBehaviour, ISettingRow
    {
        [SerializeField] private TMP_Text _actionLabel;
        [SerializeField] private KeyBindingSlotView _primarySlotView;
        [SerializeField] private KeyBindingSlotView _secondarySlotView;
        [SerializeField] private SettingRowHighlight _highlight;

        public RebindableAction RebindableAction { get; private set; }
        public KeyBindingSlotView PrimarySlotView => _primarySlotView;
        public KeyBindingSlotView SecondarySlotView => _secondarySlotView;

        public string Title => RebindableAction.Label;
        public string Description => RebindableAction.Description;

        #region Public Methods

        public void ShowAction(RebindableAction rebindableAction)
        {
            RebindableAction = rebindableAction;
            _actionLabel.text = rebindableAction.Label;
        }

        public void ShowSlots(IReadOnlyList<KeyBindingSlot> slots, int slotsPerAction)
        {
            _primarySlotView.Bind(slots.Count > 0 ? slots[0] : null);
            _secondarySlotView.Bind(slots.Count > 1 ? slots[1] : null);
            _secondarySlotView.gameObject.SetActive(slotsPerAction > 1);
        }

        public void ShowFocused()
        {
            _highlight.ShowFocused();
        }

        public void ShowUnfocused()
        {
            _highlight.ShowUnfocused();
        }

        #endregion
    }
}
