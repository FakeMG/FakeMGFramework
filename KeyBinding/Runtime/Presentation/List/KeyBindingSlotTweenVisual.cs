using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace FakeMG.KeyBinding
{
    /// <summary>Colors the slot's background, pulses its glyph while rebinding, and flashes it when its key changes.</summary>
    /// <remarks>All motion runs on unscaled time: a pause menu holds the simulation paused.</remarks>
    public sealed class KeyBindingSlotTweenVisual : KeyBindingSlotVisual
    {
        [Header("References")]
        [Required, SerializeField] private Image _background;
        [Required, SerializeField] private CanvasGroup _glyphCanvasGroup;

        [Space]
        [Header("Colors")]
        [SerializeField] private Color _idleColor = new(0.902f, 0.867f, 0.796f);
        [SerializeField] private Color _rebindingColor = new(0.851f, 0.588f, 0.169f);
        [SerializeField] private Color _changedFlashColor = new(0.431f, 0.620f, 0.290f);

        [Space]
        [Header("Motion")]
        [Tooltip("How long the background takes to settle into a new color.")]
        [SerializeField, Min(0f)] private float _colorFadeSeconds = 0.15f;
        [Tooltip("One half-cycle of the waiting pulse on the glyph.")]
        [SerializeField, Min(0.05f)] private float _rebindingPulseSeconds = 0.45f;
        [SerializeField, Range(0f, 1f)] private float _rebindingPulseMinAlpha01 = 0.35f;
        [Tooltip("How long the changed-binding flash takes to fade back to idle.")]
        [SerializeField, Min(0f)] private float _changedFlashSeconds = 0.5f;
        [SerializeField] private Vector3 _changedPunchScale = new(0.12f, 0.12f, 0f);

        private Transform GlyphTransform => _glyphCanvasGroup.transform;

        #region Public Methods

        public override void ShowIdle()
        {
            StopMotion();
            _background.DOColor(_idleColor, _colorFadeSeconds)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .SetLink(gameObject);
            _glyphCanvasGroup.DOFade(1f, _colorFadeSeconds)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .SetLink(gameObject);
            GlyphTransform.DOScale(Vector3.one, _colorFadeSeconds)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        public override void ShowRebinding()
        {
            StopMotion();
            _background.DOColor(_rebindingColor, _colorFadeSeconds)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .SetLink(gameObject);
            _glyphCanvasGroup.DOFade(_rebindingPulseMinAlpha01, _rebindingPulseSeconds)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        /// <remarks>The flash color lands instantly as impact feedback, then eases back to idle.</remarks>
        public override void ShowKeyChanged()
        {
            SnapToIdle();
            _background.color = _changedFlashColor;
            _background.DOColor(_idleColor, _changedFlashSeconds)
                .SetEase(Ease.InQuad)
                .SetUpdate(true)
                .SetLink(gameObject);
            GlyphTransform.DOPunchScale(_changedPunchScale, _changedFlashSeconds, 6, 0.5f)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        public override void SnapToIdle()
        {
            StopMotion();
            _background.color = _idleColor;
            _glyphCanvasGroup.alpha = 1f;
            GlyphTransform.localScale = Vector3.one;
        }

        #endregion

        #region Private Methods

        private void StopMotion()
        {
            _background.DOKill();
            _glyphCanvasGroup.DOKill();
            GlyphTransform.DOKill();
        }

        #endregion
    }
}
