using DG.Tweening;
using FakeMG.InputHints;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace FakeMG.KeyBinding
{
    /// <remarks>
    /// The key is drawn large when the device has no diagram, or its diagram lacks the control, e.g. a numpad key. All
    /// motion runs on unscaled time: a pause menu holds the simulation paused.
    /// </remarks>
    public sealed class BindingDiagramView : MonoBehaviour
    {
        [Header("Diagram")]
        [SerializeField] private CanvasGroup _diagramGroup;
        [Tooltip("Sized to the diagram's aspect, so shapes normalized to the picture land on its controls.")]
        [SerializeField] private AspectRatioFitter _diagramFitter;
        [Tooltip("Fills the device's shape behind everything else.")]
        [SerializeField] private RawImage _diagramFill;
        [Tooltip("Fills the pictured control, between the device's fill and its outlines.")]
        [SerializeField] private RawImage _highlight;
        [SerializeField] private Image _diagramOutlines;

        [Space]
        [Header("Key")]
        [SerializeField] private CanvasGroup _keyGroup;
        [SerializeField] private InputGlyphView _keyGlyphView;

        [Space]
        [Header("Motion")]
        [SerializeField, Min(0f)] private float _fadeSeconds = 0.15f;
        [Tooltip("How long a newly pictured control takes to fill in.")]
        [SerializeField, Min(0f)] private float _highlightFadeSeconds = 0.12f;

        private ActiveGlyphSetTracker _glyphSetTracker;
        private InputGlyphResolver _glyphResolver;

        #region Unity Lifecycle

        private void OnDisable()
        {
            _diagramGroup.DOKill();
            _keyGroup.DOKill();
            _highlight.DOKill();
            _diagramGroup.alpha = 0f;
            _keyGroup.alpha = 0f;
            SetHighlightAlpha(1f);
        }

        #endregion

        #region Public Methods

        /// <remarks>Only stores its dependencies: the pause menu injects its cached popup again on every open.</remarks>
        [Inject]
        public void Construct(ActiveGlyphSetTracker glyphSetTracker, InputGlyphResolver glyphResolver)
        {
            _glyphSetTracker = glyphSetTracker;
            _glyphResolver = glyphResolver;
        }

        /// <param name="bindingPath">Null shows nothing.</param>
        public void Show(string bindingPath)
        {
            InputGlyphSetSO glyphSetSO = _glyphSetTracker.ActiveSetSO;
            bool hasPicture = glyphSetSO && !string.IsNullOrEmpty(bindingPath);
            InputGlyphDiagramHotspot hotspot = default;
            bool isDiagramShown = hasPicture
                && glyphSetSO.DeviceDiagram
                && _glyphResolver.TryGetDiagramHotspot(bindingPath, glyphSetSO, out hotspot);
            if (isDiagramShown)
            {
                ShowDiagram(glyphSetSO);
                ShowHighlight(hotspot);
            }
            else if (hasPicture)
            {
                _keyGlyphView.Show(_glyphResolver.ResolveBindingPath(bindingPath, glyphSetSO));
            }

            FadeTo(_diagramGroup, isDiagramShown);
            FadeTo(_keyGroup, hasPicture && !isDiagramShown);
        }

        #endregion

        #region Private Methods

        private void ShowDiagram(InputGlyphSetSO glyphSetSO)
        {
            Sprite outlines = glyphSetSO.DeviceDiagram;
            _diagramOutlines.sprite = outlines;
            _diagramFitter.aspectRatio = outlines.rect.width / outlines.rect.height;
            _diagramFill.texture = glyphSetSO.DiagramMasks;
            _diagramFill.uvRect = glyphSetSO.DiagramFillUvRect;
            _highlight.texture = glyphSetSO.DiagramMasks;
        }

        /// <remarks>Fills in again only when it moves, so a rebind to the same control does not flicker.</remarks>
        private void ShowHighlight(InputGlyphDiagramHotspot hotspot)
        {
            RectTransform highlightRect = _highlight.rectTransform;
            bool hasMoved = highlightRect.anchorMin != hotspot.AnchorMin01 || highlightRect.anchorMax != hotspot.AnchorMax01;
            highlightRect.anchorMin = hotspot.AnchorMin01;
            highlightRect.anchorMax = hotspot.AnchorMax01;
            highlightRect.offsetMin = Vector2.zero;
            highlightRect.offsetMax = Vector2.zero;
            _highlight.uvRect = hotspot.MaskUvRect;
            if (hasMoved)
            {
                _highlight.DOKill();
                SetHighlightAlpha(0f);
                _highlight.DOFade(1f, _highlightFadeSeconds).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(gameObject);
            }
        }

        private void SetHighlightAlpha(float alpha01)
        {
            Color color = _highlight.color;
            color.a = alpha01;
            _highlight.color = color;
        }

        private void FadeTo(CanvasGroup group, bool isShown)
        {
            group.DOKill();
            group.DOFade(isShown ? 1f : 0f, _fadeSeconds).SetEase(isShown ? Ease.OutQuad : Ease.InQuad).SetUpdate(true).SetLink(gameObject);
        }

        #endregion
    }
}
