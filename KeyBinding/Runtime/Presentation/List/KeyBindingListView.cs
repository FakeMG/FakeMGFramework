using System;
using System.Collections.Generic;
using FakeMG.Framework.ExtensionMethods;
using FakeMG.InputHints;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace FakeMG.KeyBinding
{
    public sealed class KeyBindingListView : MonoBehaviour
    {
        [Header("Layout")]
        [Tooltip("Follows keyboard and gamepad navigation so the selected binding is always in view.")]
        [Required, SerializeField] private ScrollRect _scrollRect;
        [Required, SerializeField] private RectTransform _content;
        [Required, SerializeField] private GameObject _secondaryColumnHeader;

        [Space]
        [Header("Prefabs")]
        [Required, SerializeField] private TMP_Text _sectionHeaderPrefab;
        [Required, SerializeField] private KeyBindingRowView _rowPrefab;

        private readonly List<ListedSection> _sections = new();
        private readonly List<KeyBindingSlotView> _slotViews = new();
        private KeyBindingCatalogSO _catalogSO;
        private KeyBindingService _keyBindings;
        private InputGlyphResolver _glyphResolver;
        private InputGlyphSetSO _glyphSetSO;

        /// <remarks>Empty until the first <see cref="ShowDeviceFamily"/> builds the rows.</remarks>
        public IReadOnlyList<KeyBindingSlotView> SlotViews => _slotViews;

        /// <summary>
        /// Raised after the list shows new keys: on a device switch, a rebind, a swap or a reset. Anything that pictures the
        /// selected slot's key refreshes here, after the slots have their new bindings.
        /// </summary>
        public event Action OnBindingsDisplayed;

        #region Public Methods

        /// <remarks>Only stores its dependencies: the pause menu injects its cached popup again on every open.</remarks>
        [Inject]
        public void Construct(KeyBindingConfigSO configSO, KeyBindingService keyBindings, InputGlyphResolver glyphResolver)
        {
            _catalogSO = configSO.KeyBindingCatalogSO;
            _keyBindings = keyBindings;
            _glyphResolver = glyphResolver;
        }

        /// <param name="glyphSetSO">Draws the bindings; null shows them as text.</param>
        /// <remarks>Builds the rows the first time it runs.</remarks>
        public void ShowDeviceFamily(BindingDeviceFamilySO deviceFamilySO, InputGlyphSetSO glyphSetSO)
        {
            if (_sections.Count == 0)
            {
                BuildRows();
            }

            _glyphSetSO = glyphSetSO;
            foreach (ListedSection section in _sections)
            {
                bool isAnyRowShown = false;
                foreach (KeyBindingRowView row in section.Rows)
                {
                    IReadOnlyList<KeyBindingSlot> slots = _keyBindings.GetSlots(row.RebindableAction, deviceFamilySO);
                    bool isRowShown = slots.Count > 0;
                    row.gameObject.SetActive(isRowShown);
                    row.ShowSlots(slots, deviceFamilySO.SlotsPerAction);
                    isAnyRowShown |= isRowShown;
                }

                section.Header.gameObject.SetActive(isAnyRowShown);
            }

            _secondaryColumnHeader.SetActive(deviceFamilySO.SlotsPerAction > 1);
            ShowAllCurrentKeys();
        }

        public void ShowAllCurrentKeys()
        {
            foreach (KeyBindingSlotView slotView in _slotViews)
            {
                ShowCurrentKey(slotView);
            }

            OnBindingsDisplayed?.Invoke();
        }

        public KeyBindingSlotView GetSlotView(KeyBindingSlot slot)
        {
            foreach (KeyBindingSlotView slotView in _slotViews)
            {
                if (slotView.Slot == slot)
                {
                    return slotView;
                }
            }

            return null;
        }

        /// <returns>Null when no row is shown.</returns>
        public KeyBindingSlotView FindFirstShownSlotView()
        {
            foreach (KeyBindingSlotView slotView in _slotViews)
            {
                if (slotView.Slot != null && slotView.gameObject.activeInHierarchy)
                {
                    return slotView;
                }
            }

            return null;
        }

        public bool Contains(GameObject candidate)
        {
            return candidate && candidate.transform.IsChildOf(_content);
        }

        public void ScrollTo(KeyBindingSlotView slotView)
        {
            _scrollRect.ScrollTo(slotView.transform);
        }

        #endregion

        #region Private Methods

        private void BuildRows()
        {
            foreach (KeyBindingSection section in _catalogSO.Sections)
            {
                TMP_Text sectionHeader = Instantiate(_sectionHeaderPrefab, _content);
                sectionHeader.text = section.Title;
                var listedSection = new ListedSection(sectionHeader);
                _sections.Add(listedSection);

                foreach (RebindableAction rebindableAction in section.Actions)
                {
                    KeyBindingRowView row = Instantiate(_rowPrefab, _content);
                    row.ShowAction(rebindableAction);
                    listedSection.Rows.Add(row);
                    _slotViews.Add(row.PrimarySlotView);
                    _slotViews.Add(row.SecondarySlotView);
                }
            }
        }

        private void ShowCurrentKey(KeyBindingSlotView slotView)
        {
            KeyBindingSlot slot = slotView.Slot;
            if (slot == null)
            {
                return;
            }

            if (slot.IsUnbound)
            {
                slotView.ShowUnbound();
            }
            else
            {
                slotView.ShowKey(ResolveGlyph(slot));
            }
        }

        private InputGlyph ResolveGlyph(KeyBindingSlot slot)
        {
            return _glyphSetSO ? _glyphResolver.ResolveBindingPath(slot.ControlPath, _glyphSetSO) : new InputGlyph(null, slot.GetDisplayText());
        }

        #endregion

        private sealed class ListedSection
        {
            public TMP_Text Header { get; }
            public List<KeyBindingRowView> Rows { get; } = new();

            public ListedSection(TMP_Text header)
            {
                Header = header;
            }
        }
    }
}
