using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.InputHints.Editor
{
    /// <summary>
    /// A full US keyboard with the mouse below its navigation keys, measured in key units: one unit is a letter key's pitch. Rects have
    /// a top-left origin, as in an image.
    /// </summary>
    /// <remarks>The diagram image and its hotspots are both made from this table, so they cannot drift apart.</remarks>
    internal static class KeyboardMouseDiagramLayout
    {
        /// <remarks>About twice the size the settings panel shows a key at, so labels stay sharp without mipmaps.</remarks>
        public const int PIXELS_PER_KEY_UNIT = 48;
        public const float KEY_GAP_KEY_UNITS = 0.08f;

        /// <summary>Width over height of the Xelu mouse blank once cropped to its outline.</summary>
        public const float MOUSE_ASPECT = 68f / 92f;

        private const float NAVIGATION_LEFT_KEY_UNITS = 15.25f;
        private const float NAVIGATION_WIDTH_KEY_UNITS = 3f;
        private const float NUMPAD_LEFT_KEY_UNITS = 18.5f;
        private const float NUMPAD_WIDTH_KEY_UNITS = 4f;
        private const float KEYBOARD_HEIGHT_KEY_UNITS = 6.5f;
        private const float MOUSE_TOP_GAP_KEY_UNITS = 0.5f;
        private const float MOUSE_HEIGHT_KEY_UNITS = 3.5f;

        /// <remarks>Below the keyboard rather than beside it, so the diagram stays narrow and its keys stay large when fit to width.</remarks>
        public static readonly Rect MOUSE_KEY_UNITS = new(
            NAVIGATION_LEFT_KEY_UNITS + (NAVIGATION_WIDTH_KEY_UNITS - MOUSE_HEIGHT_KEY_UNITS * MOUSE_ASPECT) / 2f,
            KEYBOARD_HEIGHT_KEY_UNITS + MOUSE_TOP_GAP_KEY_UNITS,
            MOUSE_HEIGHT_KEY_UNITS * MOUSE_ASPECT,
            MOUSE_HEIGHT_KEY_UNITS);

        public static readonly Vector2 SIZE_KEY_UNITS = new(NUMPAD_LEFT_KEY_UNITS + NUMPAD_WIDTH_KEY_UNITS, MOUSE_KEY_UNITS.yMax);

        /// <summary>Mouse controls as fractions of the mouse's rect, measured on the Xelu mouse blank.</summary>
        private static readonly (string controlPath, Rect rect01)[] MOUSE_CONTROLS =
        {
            ("leftButton", new Rect(0.03f, 0.03f, 0.42f, 0.36f)),
            ("rightButton", new Rect(0.55f, 0.03f, 0.42f, 0.36f)),
            ("middleButton", new Rect(0.40f, 0.10f, 0.19f, 0.28f)),
        };

        /// <summary>Mouse controls drawn as another control: the wheel is the middle button.</summary>
        private static readonly (string controlPath, string drawnControlPath)[] MOUSE_CONTROL_ALIASES =
        {
            ("scroll", "middleButton"),
        };

        /// <summary>Keyboard controls that stand for either of two keys; the diagram points at the left one.</summary>
        private static readonly (string controlPath, string drawnControlPath)[] EITHER_SIDE_KEYS =
        {
            ("shift", "leftShift"),
            ("ctrl", "leftCtrl"),
            ("alt", "leftAlt"),
        };

        public static Vector2Int SizePixels => Vector2Int.RoundToInt(SIZE_KEY_UNITS * PIXELS_PER_KEY_UNIT);
        public static IReadOnlyList<DiagramKey> Keys { get; } = BuildKeys();

        #region Public Methods

        public static List<DiagramHotspotPixels> BuildHotspotPixels()
        {
            var hotspots = new List<DiagramHotspotPixels>();
            var keyRectsByControlPath = new Dictionary<string, Rect>();
            foreach (DiagramKey key in Keys)
            {
                keyRectsByControlPath.Add(key.ControlPath, key.RectKeyUnits);
                hotspots.Add(ToHotspotPixels(key.ControlPath, key.RectKeyUnits));
            }

            foreach ((string controlPath, string drawnControlPath) in EITHER_SIDE_KEYS)
            {
                hotspots.Add(ToHotspotPixels(controlPath, keyRectsByControlPath[drawnControlPath]));
            }

            var mouseRectsByControlPath = new Dictionary<string, Rect>();
            foreach ((string controlPath, Rect rect01) in MOUSE_CONTROLS)
            {
                var rectKeyUnits = new Rect(
                    MOUSE_KEY_UNITS.x + rect01.x * MOUSE_KEY_UNITS.width,
                    MOUSE_KEY_UNITS.y + rect01.y * MOUSE_KEY_UNITS.height,
                    rect01.width * MOUSE_KEY_UNITS.width,
                    rect01.height * MOUSE_KEY_UNITS.height);
                mouseRectsByControlPath.Add(controlPath, rectKeyUnits);
                hotspots.Add(ToHotspotPixels(controlPath, rectKeyUnits));
            }

            foreach ((string controlPath, string drawnControlPath) in MOUSE_CONTROL_ALIASES)
            {
                hotspots.Add(ToHotspotPixels(controlPath, mouseRectsByControlPath[drawnControlPath]));
            }

            return hotspots;
        }

        #endregion

        #region Private Methods

        private static DiagramHotspotPixels ToHotspotPixels(string controlPath, Rect rectKeyUnits)
        {
            return new DiagramHotspotPixels(
                controlPath,
                rectKeyUnits.center.x * PIXELS_PER_KEY_UNIT,
                rectKeyUnits.center.y * PIXELS_PER_KEY_UNIT,
                rectKeyUnits.width * PIXELS_PER_KEY_UNIT,
                rectKeyUnits.height * PIXELS_PER_KEY_UNIT);
        }

        private static List<DiagramKey> BuildKeys()
        {
            var keys = new List<DiagramKey>();
            AddRow(keys, 0f, 0f, ("escape", "Esc", 1f));
            AddRow(keys, 0f, 2f, ("f1", "F1", 1f), ("f2", "F2", 1f), ("f3", "F3", 1f), ("f4", "F4", 1f));
            AddRow(keys, 0f, 6.5f, ("f5", "F5", 1f), ("f6", "F6", 1f), ("f7", "F7", 1f), ("f8", "F8", 1f));
            AddRow(keys, 0f, 11f, ("f9", "F9", 1f), ("f10", "F10", 1f), ("f11", "F11", 1f), ("f12", "F12", 1f));

            AddRow(keys, 1.5f, 0f,
                ("backquote", "`", 1f), ("1", "1", 1f), ("2", "2", 1f), ("3", "3", 1f), ("4", "4", 1f), ("5", "5", 1f),
                ("6", "6", 1f), ("7", "7", 1f), ("8", "8", 1f), ("9", "9", 1f), ("0", "0", 1f), ("minus", "-", 1f),
                ("equals", "=", 1f), ("backspace", "Bksp", 2f));
            AddRow(keys, 2.5f, 0f,
                ("tab", "Tab", 1.5f), Letter('q'), Letter('w'), Letter('e'), Letter('r'), Letter('t'), Letter('y'),
                Letter('u'), Letter('i'), Letter('o'), Letter('p'), ("leftBracket", "[", 1f), ("rightBracket", "]", 1f),
                ("backslash", "\\", 1.5f));
            AddRow(keys, 3.5f, 0f,
                ("capsLock", "Caps", 1.75f), Letter('a'), Letter('s'), Letter('d'), Letter('f'), Letter('g'), Letter('h'),
                Letter('j'), Letter('k'), Letter('l'), ("semicolon", ";", 1f), ("quote", "'", 1f), ("enter", "Enter", 2.25f));
            AddRow(keys, 4.5f, 0f,
                ("leftShift", "Shift", 2.25f), Letter('z'), Letter('x'), Letter('c'), Letter('v'), Letter('b'), Letter('n'),
                Letter('m'), ("comma", ",", 1f), ("period", ".", 1f), ("slash", "/", 1f), ("rightShift", "Shift", 2.75f));
            AddRow(keys, 5.5f, 0f,
                ("leftCtrl", "Ctrl", 1.25f), ("leftMeta", "Win", 1.25f), ("leftAlt", "Alt", 1.25f), ("space", "Space", 6.25f),
                ("rightAlt", "Alt", 1.25f), ("rightMeta", "Win", 1.25f), ("contextMenu", "Menu", 1.25f), ("rightCtrl", "Ctrl", 1.25f));

            AddRow(keys, 0f, NAVIGATION_LEFT_KEY_UNITS, ("printScreen", "PrtSc", 1f), ("scrollLock", "ScrLk", 1f), ("pause", "Pause", 1f));
            AddRow(keys, 1.5f, NAVIGATION_LEFT_KEY_UNITS, ("insert", "Ins", 1f), ("home", "Home", 1f), ("pageUp", "PgUp", 1f));
            AddRow(keys, 2.5f, NAVIGATION_LEFT_KEY_UNITS, ("delete", "Del", 1f), ("end", "End", 1f), ("pageDown", "PgDn", 1f));
            AddRow(keys, 4.5f, NAVIGATION_LEFT_KEY_UNITS + 1f, ("upArrow", "↑", 1f));
            AddRow(keys, 5.5f, NAVIGATION_LEFT_KEY_UNITS, ("leftArrow", "←", 1f), ("downArrow", "↓", 1f), ("rightArrow", "→", 1f));

            AddRow(keys, 1.5f, NUMPAD_LEFT_KEY_UNITS,
                ("numLock", "Num", 1f), ("numpadDivide", "/", 1f), ("numpadMultiply", "*", 1f), ("numpadMinus", "-", 1f));
            AddRow(keys, 2.5f, NUMPAD_LEFT_KEY_UNITS, Numpad(7), Numpad(8), Numpad(9));
            AddRow(keys, 3.5f, NUMPAD_LEFT_KEY_UNITS, Numpad(4), Numpad(5), Numpad(6));
            AddRow(keys, 4.5f, NUMPAD_LEFT_KEY_UNITS, Numpad(1), Numpad(2), Numpad(3));
            AddRow(keys, 5.5f, NUMPAD_LEFT_KEY_UNITS, ("numpad0", "0", 2f), ("numpadPeriod", ".", 1f));
            AddTallKey(keys, 2.5f, NUMPAD_LEFT_KEY_UNITS + 3f, "numpadPlus", "+");
            AddTallKey(keys, 4.5f, NUMPAD_LEFT_KEY_UNITS + 3f, "numpadEnter", "Enter");
            return keys;
        }

        private static (string, string, float) Letter(char letter)
        {
            return (letter.ToString(), char.ToUpperInvariant(letter).ToString(), 1f);
        }

        private static (string, string, float) Numpad(int digit)
        {
            return ($"numpad{digit}", digit.ToString(), 1f);
        }

        private static void AddTallKey(List<DiagramKey> keys, float topKeyUnits, float leftKeyUnits, string controlPath, string label)
        {
            const float TALL_KEY_HEIGHT_KEY_UNITS = 2f;
            keys.Add(new DiagramKey(controlPath, label, ToKeyRect(leftKeyUnits, topKeyUnits, 1f, TALL_KEY_HEIGHT_KEY_UNITS)));
        }

        private static void AddRow(
            List<DiagramKey> keys,
            float topKeyUnits,
            float leftKeyUnits,
            params (string controlPath, string label, float widthKeyUnits)[] rowKeys)
        {
            float xKeyUnits = leftKeyUnits;
            foreach ((string controlPath, string label, float widthKeyUnits) in rowKeys)
            {
                keys.Add(new DiagramKey(controlPath, label, ToKeyRect(xKeyUnits, topKeyUnits, widthKeyUnits, 1f)));
                xKeyUnits += widthKeyUnits;
            }
        }

        /// <returns>The key's visible rect: its slot on the grid shrunk by half the gap on each side.</returns>
        private static Rect ToKeyRect(float leftKeyUnits, float topKeyUnits, float widthKeyUnits, float heightKeyUnits)
        {
            return new Rect(
                leftKeyUnits + KEY_GAP_KEY_UNITS / 2f,
                topKeyUnits + KEY_GAP_KEY_UNITS / 2f,
                widthKeyUnits - KEY_GAP_KEY_UNITS,
                heightKeyUnits - KEY_GAP_KEY_UNITS);
        }

        #endregion
    }

    internal readonly struct DiagramKey
    {
        public string ControlPath { get; }
        public string Label { get; }
        public Rect RectKeyUnits { get; }

        public DiagramKey(string controlPath, string label, Rect rectKeyUnits)
        {
            ControlPath = controlPath;
            Label = label;
            RectKeyUnits = rectKeyUnits;
        }
    }
}
