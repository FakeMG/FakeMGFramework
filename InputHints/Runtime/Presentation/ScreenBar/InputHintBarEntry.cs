using System.Collections.Generic;
using FakeMG.Framework.UI;
using TMPro;
using UnityEngine;

namespace FakeMG.InputHints
{
    public sealed class InputHintBarEntry : MonoBehaviour
    {
        [SerializeField] private InputGlyphView _glyphPrefab;
        [SerializeField] private TMP_Text _label;

        private ReusableViewList<InputGlyphView> _glyphViews;

        /// <remarks>
        /// Created on first use: the bar fills entries while its frame may still be inactive, before any Awake. Glyphs share
        /// the entry's own layout group with the label, so the bar nests one layout less.
        /// </remarks>
        private ReusableViewList<InputGlyphView> GlyphViews => _glyphViews ??= new ReusableViewList<InputGlyphView>(_glyphPrefab, transform);

        #region Public Methods

        public void Show(InputHint hint, IReadOnlyList<InputGlyph> glyphs)
        {
            _label.text = hint.Label;
            GlyphViews.Show(glyphs.Count);
            for (int i = 0; i < glyphs.Count; i++)
            {
                GlyphViews[i].Show(glyphs[i]);
            }

            _label.transform.SetAsLastSibling();
        }

        #endregion
    }
}
