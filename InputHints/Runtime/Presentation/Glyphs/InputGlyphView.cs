using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FakeMG.InputHints
{
    public sealed class InputGlyphView : MonoBehaviour
    {
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
