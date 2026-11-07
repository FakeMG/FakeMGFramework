using UnityEngine;

namespace FakeMG.InputHints
{
    /// <summary>
    /// How one control is drawn: its glyph, or its name when the glyph set has no glyph for it, so a missing glyph still
    /// tells the player what to press.
    /// </summary>
    public readonly struct InputGlyph
    {
        public Sprite Sprite { get; }
        public string ControlName { get; }
        public bool HasSprite => Sprite;

        public InputGlyph(Sprite sprite, string controlName)
        {
            Sprite = sprite;
            ControlName = controlName;
        }
    }
}
