using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace FakeMG.InputHints
{
    /// <summary>Shows the glyph of one action's binding; <see cref="ActionGlyphSubscriber"/> tells it the active device's set.</summary>
    /// <remarks>Hidden while the device has no binding for the action.</remarks>
    public sealed class ActionGlyphView : MonoBehaviour
    {
        [SerializeField] private InputActionReference _actionReference;
        [SerializeField] private InputGlyphView _glyphView;

        private readonly List<InputGlyph> _glyphs = new();
        private InputGlyphResolver _glyphResolver;

        #region Public Methods

        [Inject]
        public void Construct(InputGlyphResolver glyphResolver)
        {
            _glyphResolver = glyphResolver;
        }

        /// <param name="activeSetSO">Null before any device is known; the glyph is then hidden.</param>
        public void ShowGlyphFrom(InputGlyphSetSO activeSetSO)
        {
            _glyphs.Clear();
            if (activeSetSO)
            {
                _glyphResolver.Resolve(_actionReference.action, activeSetSO, _glyphs);
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
