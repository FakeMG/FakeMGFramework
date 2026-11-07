using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace FakeMG.InputHints
{
    /// <summary>Draws one control: its glyph, or its name when the glyph set has no glyph for it.</summary>
    public sealed class InputGlyphView : MonoBehaviour
    {
        [FormerlySerializedAs("_icon")]
        [SerializeField] private Image _glyphImage;
        [SerializeField] private TMP_Text _controlName;

        #region Public Methods

        public void Show(InputGlyph glyph)
        {
            _glyphImage.enabled = glyph.HasSprite;
            _glyphImage.sprite = glyph.Sprite;
            _controlName.enabled = !glyph.HasSprite;
            _controlName.text = glyph.ControlName;
        }

        #endregion
    }
}
