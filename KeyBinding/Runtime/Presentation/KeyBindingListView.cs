using System;
using System.Collections.Generic;
using FakeMG.Framework.ExtensionMethods;
using FakeMG.InputHints;
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
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _content;
        [SerializeField] private GameObject _secondaryColumnHeader;

        [Space]
        [Header("Prefabs")]
        [SerializeField] private TMP_Text _sectionHeaderPrefab;
        [SerializeField] private KeyBindingRowView _rowPrefab;

        private readonly List<ListedSection> _sections = new();
        private readonly List<KeyBindingSlotView> _slotViews = new();
        private KeyBindingCatalogSO _catalogSO;
        private KeyBindingService _keyBindings;
        private InputGlyphResolver _glyphResolver;
        private InputGlyphSetSO _glyphSetSO;

        public event Action<KeyBindingSlot> OnSlotClicked;
        public event Action<KeyBindingSlot> OnSlotResetRequested;
        public event Action OnKeysShown;

        #region Unity Lifecycle

        private void OnDestroy()
        {
            foreach (KeyBindingSlotView slotView in _slotViews)
            {
                slotView.OnClicked -= RaiseSlotClicked;
                slotView.OnNavigatedTo -= ScrollToSlot;
                slotView.OnResetRequested -= RaiseSlotResetRequested;
            }
        }

        #endregion

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
            foreach (ListedSection section in _sections)
            {
                foreach (KeyBindingRowView row in section.Rows)
                {
                    ShowCurrentKey(row.PrimarySlotView);
                    ShowCurrentKey(row.SecondarySlotView);
                }
            }

            OnKeysShown?.Invoke();
        }

        public KeyBindingSlotView GetSlotView(KeyBindingSlot slot)
        {
            return _slotViews.Find(slotView => slotView.Slot == slot);
        }

        /// <returns>Null when no row is shown.</returns>
        public KeyBindingSlotView FindFirstShownSlotView()
        {
            return _slotViews.Find(slotView => slotView.Slot != null && slotView.gameObject.activeInHierarchy);
        }

        public bool Contains(GameObject candidate)
        {
            return candidate && candidate.transform.IsChildOf(_content);
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
                    AddSlotView(row.PrimarySlotView);
                    AddSlotView(row.SecondarySlotView);
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

        private void AddSlotView(KeyBindingSlotView slotView)
        {
            _slotViews.Add(slotView);
            slotView.OnClicked += RaiseSlotClicked;
            slotView.OnNavigatedTo += ScrollToSlot;
            slotView.OnResetRequested += RaiseSlotResetRequested;
        }

        private InputGlyph ResolveGlyph(KeyBindingSlot slot)
        {
            return _glyphSetSO ? _glyphResolver.ResolveBindingPath(slot.ControlPath, _glyphSetSO) : new InputGlyph(null, slot.GetDisplayText());
        }

        private void ScrollToSlot(KeyBindingSlotView slotView)
        {
            _scrollRect.ScrollTo(slotView.transform);
        }

        private void RaiseSlotClicked(KeyBindingSlotView slotView)
        {
            OnSlotClicked?.Invoke(slotView.Slot);
        }

        private void RaiseSlotResetRequested(KeyBindingSlotView slotView)
        {
            OnSlotResetRequested?.Invoke(slotView.Slot);
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
