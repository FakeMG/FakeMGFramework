using System.Collections.Generic;
using FakeMG.Framework.UI;
using TMPro;
using UnityEngine;

namespace FakeMG.InputHints
{
    /// <summary>One hint in the bar: its glyphs and label. Display-only, so it ignores the pointer.</summary>
    public sealed class InputHintBarEntry : MonoBehaviour
    {
        [SerializeField] private InputGlyphView _glyphPrefab;
        [SerializeField] private Transform _glyphParent;
        [SerializeField] private TMP_Text _label;

        private ReusableViewList<InputGlyphView> _glyphViews;

        /// <remarks>Created on first use: the bar fills entries while its frame may still be inactive, before any Awake.</remarks>
        private ReusableViewList<InputGlyphView> GlyphViews => _glyphViews ??= new ReusableViewList<InputGlyphView>(_glyphPrefab, _glyphParent);

        #region Public Methods

        public void Show(InputHint hint, IReadOnlyList<InputGlyph> glyphs)
        {
            _label.text = hint.Label;
            GlyphViews.Show(glyphs.Count);
            for (int i = 0; i < glyphs.Count; i++)
            {
                GlyphViews[i].Show(glyphs[i]);
            }
        }

        #endregion
    }
}
