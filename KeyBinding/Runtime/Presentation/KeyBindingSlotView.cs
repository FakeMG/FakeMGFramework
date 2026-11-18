using System;
using DG.Tweening;
using FakeMG.InputHints;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FakeMG.KeyBinding
{
    /// <remarks>All motion runs on unscaled time: a pause menu holds the simulation paused.</remarks>
    public sealed class KeyBindingSlotView : MonoBehaviour, ISelectHandler, IPointerClickHandler
    {
        private static readonly InputGlyph UNBOUND_GLYPH = new(null, "—");

        [Header("References")]
        [SerializeField] private Button _button;
        [SerializeField] private Image _background;
        [SerializeField] private InputGlyphView _glyphView;
        [SerializeField] private CanvasGroup _glyphCanvasGroup;

        [Space]
        [Header("Colors")]
        [SerializeField] private Color _idleColor = new(0.902f, 0.867f, 0.796f);
        [SerializeField] private Color _listeningColor = new(0.851f, 0.588f, 0.169f);
        [SerializeField] private Color _changedFlashColor = new(0.431f, 0.620f, 0.290f);

        [Space]
        [Header("Motion")]
        [Tooltip("How long the background takes to settle into a new color.")]
        [SerializeField, Min(0f)] private float _colorFadeSeconds = 0.15f;
        [Tooltip("One half-cycle of the waiting pulse on the glyph.")]
        [SerializeField, Min(0.05f)] private float _listeningPulseSeconds = 0.45f;
        [SerializeField, Range(0f, 1f)] private float _listeningPulseMinAlpha01 = 0.35f;
        [Tooltip("How long the changed-binding flash takes to fade back to idle.")]
        [SerializeField, Min(0f)] private float _changedFlashSeconds = 0.5f;
        [SerializeField] private Vector3 _changedPunchScale = new(0.12f, 0.12f, 0f);

        private InputGlyph _keyGlyph = UNBOUND_GLYPH;

        public KeyBindingSlot Slot { get; private set; }

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

        /// <remarks>Instant on purpose: used when the list is first shown or every binding changes at once.</remarks>
        public void ShowKey(InputGlyph keyGlyph)
        {
            _keyGlyph = Slot == null ? UNBOUND_GLYPH : keyGlyph;
            _glyphView.Show(_keyGlyph);
        }

        public void ShowUnbound()
        {
            ShowKey(UNBOUND_GLYPH);
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

        public void ShowListening(string listeningPrompt)
        {
            StopMotion();
            _glyphView.Show(new InputGlyph(null, listeningPrompt));
            _background.DOColor(_listeningColor, _colorFadeSeconds).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(gameObject);
            _glyphCanvasGroup.DOFade(_listeningPulseMinAlpha01, _listeningPulseSeconds)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        public void ShowIdle()
        {
            StopMotion();
            _glyphView.Show(_keyGlyph);
            _background.DOColor(_idleColor, _colorFadeSeconds).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(gameObject);
            _glyphCanvasGroup.DOFade(1f, _colorFadeSeconds).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(gameObject);
            _glyphView.transform.DOScale(Vector3.one, _colorFadeSeconds).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(gameObject);
        }

        /// <remarks>The flash color lands instantly as impact feedback, then eases back to idle.</remarks>
        public void ShowKeyChanged()
        {
            StopMotion();
            _glyphView.Show(_keyGlyph);
            _background.color = _changedFlashColor;
            _glyphCanvasGroup.alpha = 1f;
            _glyphView.transform.localScale = Vector3.one;
            _background.DOColor(_idleColor, _changedFlashSeconds).SetEase(Ease.InQuad).SetUpdate(true).SetLink(gameObject);
            _glyphView.transform.DOPunchScale(_changedPunchScale, _changedFlashSeconds, 6, 0.5f).SetUpdate(true).SetLink(gameObject);
        }

        #endregion

        #region Private Methods

        private void RaiseClicked()
        {
            OnClicked?.Invoke(this);
        }

        private void StopMotion()
        {
            _background.DOKill();
            _glyphCanvasGroup.DOKill();
            _glyphView.transform.DOKill();
        }

        private void SnapToIdle()
        {
            StopMotion();
            _background.color = _idleColor;
            _glyphCanvasGroup.alpha = 1f;
            _glyphView.transform.localScale = Vector3.one;
            _glyphView.Show(_keyGlyph);
        }

        #endregion
    }
}
