using System;
using System.Collections.Generic;
using FakeMG.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FakeMG.InputHints
{
    /// <summary>
    /// The glyphs of one device family, such as keyboard and mouse or PlayStation controllers, keyed by control path.
    /// Swapping glyph packs means swapping these assets; nothing else knows which pack is in use.
    /// </summary>
    [CreateAssetMenu(menuName = FakeMGEditorMenus.INPUT_HINTS + "/Input Glyph Set SO")]
    public sealed class InputGlyphSetSO : ScriptableObject
    {
        [Header("Devices")]
        [Tooltip("Input System layouts drawn with this set, e.g. 'Keyboard' and 'Mouse', or 'DualShockGamepad'. " +
                 "Layouts based on one of these are included, so 'Gamepad' covers every gamepad.")]
        [SerializeField] private string[] _deviceLayouts = Array.Empty<string>();

        [Space]
        [Header("Glyphs")]
        [Tooltip("Control paths relative to the device, e.g. 'buttonSouth', 'leftStick/up', 'scroll/y' or 'f'.")]
        [SerializeField] private List<InputGlyphEntry> _glyphs = new();

        private Dictionary<string, Sprite> _spritesByControlPath;

        #region Unity Lifecycle

        /// <remarks>Edits in the Inspector must reach the lookup, which is built once.</remarks>
        private void OnValidate()
        {
            _spritesByControlPath = null;
        }

        #endregion

        #region Public Methods

        public bool Covers(InputDevice device)
        {
            foreach (string layout in _deviceLayouts)
            {
                if (InputSystem.IsFirstLayoutBasedOnSecond(device.layout, layout))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryGetSprite(string controlPath, out Sprite sprite)
        {
            _spritesByControlPath ??= BuildLookup();
            return _spritesByControlPath.TryGetValue(controlPath, out sprite);
        }

        #endregion

        #region Private Methods

        private Dictionary<string, Sprite> BuildLookup()
        {
            var lookup = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
            foreach (InputGlyphEntry glyph in _glyphs)
            {
                lookup[glyph.ControlPath] = glyph.Sprite;
            }

            return lookup;
        }

        #endregion
    }

    [Serializable]
    public struct InputGlyphEntry
    {
        [SerializeField] private string _controlPath;
        [SerializeField] private Sprite _sprite;

        public string ControlPath => _controlPath;
        public Sprite Sprite => _sprite;

        public InputGlyphEntry(string controlPath, Sprite sprite)
        {
            _controlPath = controlPath;
            _sprite = sprite;
        }
    }
}
