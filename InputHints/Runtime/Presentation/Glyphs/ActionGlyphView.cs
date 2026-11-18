using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace FakeMG.InputHints
{
    /// <remarks>Hidden while the device has no binding for the action.</remarks>
    public sealed class ActionGlyphView : MonoBehaviour
    {
        [SerializeField] private InputActionReference _actionReference;
        [SerializeField] private InputGlyphView _glyphView;

        private readonly List<InputGlyph> _glyphs = new();
        private ActiveGlyphSetTracker _glyphSetTracker;
        private InputGlyphResolver _glyphResolver;

        #region Unity Lifecycle

        private void OnEnable()
        {
            _glyphSetTracker.OnGlyphsChanged += ShowGlyphOfActiveDevice;
            ShowGlyphOfActiveDevice();
        }

        private void OnDisable()
        {
            _glyphSetTracker.OnGlyphsChanged -= ShowGlyphOfActiveDevice;
        }

        #endregion

        #region Public Methods

        [Inject]
        public void Construct(ActiveGlyphSetTracker glyphSetTracker, InputGlyphResolver glyphResolver)
        {
            _glyphSetTracker = glyphSetTracker;
            _glyphResolver = glyphResolver;
        }

        #endregion

        #region Private Methods

        private void ShowGlyphOfActiveDevice()
        {
            _glyphs.Clear();
            if (_glyphSetTracker.ActiveSetSO)
            {
                _glyphResolver.Resolve(_actionReference.action, _glyphSetTracker.ActiveSetSO, _glyphs);
            }

            bool hasGlyph = _glyphs.Count > 0;
            _glyphView.gameObject.SetActive(hasGlyph);
            if (hasGlyph)
            {
                _glyphView.Show(_glyphs[0]);
            }
        }

        #endregion
    }
}
