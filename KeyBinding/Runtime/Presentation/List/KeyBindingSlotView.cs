using System;
using FakeMG.InputHints;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FakeMG.KeyBinding
{
    /// <summary>One binding cap: shows its key, reports clicks, and hands its look to a <see cref="KeyBindingSlotVisual"/>.</summary>
    public sealed class KeyBindingSlotView : MonoBehaviour, ISelectHandler, IPointerClickHandler
    {
        private static readonly InputGlyph UNBOUND_GLYPH = new(null, "—");

        [Required, SerializeField] private Button _button;
        [Required, SerializeField] private InputGlyphView _glyphView;
        [Required, SerializeField] private KeyBindingSlotVisual _visual;

        private InputGlyph _keyGlyph = UNBOUND_GLYPH;

        public KeyBindingSlot Slot { get; private set; }
        public Selectable Selectable => _button;

        public event Action<KeyBindingSlotView> OnClicked;
        public event Action<KeyBindingSlotView> OnNavigatedTo;
        public event Action<KeyBindingSlotView> OnResetRequested;

        #region Unity Lifecycle

        private void OnEnable()
        {
            _button.onClick.AddListener(RaiseClicked);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(RaiseClicked);
            SnapToIdle();
        }

        #endregion

        #region Public Methods

        /// <param name="slot">Null when the action has no binding to show here; the cap is then shown empty and inert.</param>
        public void Bind(KeyBindingSlot slot)
        {
            Slot = slot;
            _button.interactable = slot != null;
            _keyGlyph = UNBOUND_GLYPH;
            SnapToIdle();
        }

        public void ShowUnbound()
        {
            ShowKey(UNBOUND_GLYPH);
        }

        /// <remarks>Instant on purpose: used when the list is first shown or every binding changes at once.</remarks>
        public void ShowKey(InputGlyph keyGlyph)
        {
            _keyGlyph = Slot == null ? UNBOUND_GLYPH : keyGlyph;
            _glyphView.Show(_keyGlyph);
        }

        /// <remarks>Only navigation selects with an <see cref="AxisEventData"/>; the pointer selects buttons it hovers.</remarks>
        public void OnSelect(BaseEventData eventData)
        {
            if (eventData is AxisEventData)
            {
                OnNavigatedTo?.Invoke(this);
            }
        }

        /// <remarks>The button handles the left click; a right click asks for the slot's default key back.</remarks>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right && _button.interactable)
            {
                OnResetRequested?.Invoke(this);
            }
        }

        public void ShowRebinding(string rebindPrompt)
        {
            _glyphView.Show(new InputGlyph(null, rebindPrompt));
            _visual.ShowRebinding();
        }

        public void ShowIdle()
        {
            _glyphView.Show(_keyGlyph);
            _visual.ShowIdle();
        }

        public void ShowKeyChanged()
        {
            _glyphView.Show(_keyGlyph);
            _visual.ShowKeyChanged();
        }

        #endregion

        #region Private Methods

        private void RaiseClicked()
        {
            OnClicked?.Invoke(this);
        }

        private void SnapToIdle()
        {
            _glyphView.Show(_keyGlyph);
            _visual.SnapToIdle();
        }

        #endregion
    }
}
