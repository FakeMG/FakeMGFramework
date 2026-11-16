using UnityEngine;

namespace FakeMG.InputHints
{
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
