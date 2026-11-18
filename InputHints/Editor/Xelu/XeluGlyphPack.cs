using System.Collections.Generic;
using FakeMG.Framework;
using UnityEditor;

namespace FakeMG.InputHints.Editor
{
    /// <remarks>Gamepad hotspots were measured on the 1452×940 diagrams; a re-exported diagram needs them measured again.</remarks>
    internal static class XeluGlyphPack
    {
        private const string XBOX_DIAGRAM = "Xbox Series/XboxSeriesX_Diagram";

        /// <remarks>Xelu draws no Switch Pro controller; its layout matches the Xbox one, so it borrows the unlabelled Xbox outline.</remarks>
        private const string SWITCH_DIAGRAM = "Xbox Series/XboxSeriesX_Diagram_Simple";

        /// <remarks>Xelu draws no keyboard diagram; this one is rendered from its blank keys before the sets are built.</remarks>
        private const string KEYBOARD_MOUSE_DIAGRAM = "Keyboard & Mouse/Keyboard_Mouse_Diagram";
        private const string KEY_BLANK_FILE_PATH = "Keyboard & Mouse/Blanks/Blank_White_Normal.png";
        private const string MOUSE_BLANK_FILE_PATH = "Keyboard & Mouse/Blanks/Blank_White_Mouse.png";

        public static readonly InputGlyphPack PACK = new("Xelu", "Xelu Input Glyph Library SO", new InputGlyphFamily[]
        {
            new("PS5", "Xelu PlayStation 5 Glyph Set SO", new[] { "DualSenseGamepadHID" }, "PS5/PS5_Diagram",
                PlayStation("PS5"), PlayStation5Hotspots()),
            new("PS4", "Xelu PlayStation 4 Glyph Set SO", new[] { "DualShockGamepad" }, "PS4/PS4_Diagram",
                PlayStation("PS4"), PlayStation4Hotspots()),
            new("Switch", "Xelu Switch Glyph Set SO", new[] { "SwitchProControllerHID" }, SWITCH_DIAGRAM, Switch(), XboxHotspots()),
            new("Keyboard & Mouse", "Xelu Keyboard Mouse Glyph Set SO", new[] { "Keyboard", "Mouse" }, KEYBOARD_MOUSE_DIAGRAM,
                KeyboardMouse(), KeyboardMouseDiagramLayout.BuildHotspotPixels()),
            new("Xbox Series", "Xelu Xbox Glyph Set SO", new[] { "XInputController", "Gamepad" }, XBOX_DIAGRAM, Xbox(), XboxHotspots()),
        });

        #region Private Methods

        [MenuItem(FakeMGEditorMenus.INPUT_HINTS_BUILD_XELU_GLYPH_SETS)]
        private static void BuildFromSelectedFolder()
        {
            string texturesFolder = InputGlyphSetBuilder.SelectedFolder;
            KeyboardMouseDiagramRenderer.Render(
                $"{texturesFolder}/{KEY_BLANK_FILE_PATH}",
                $"{texturesFolder}/{MOUSE_BLANK_FILE_PATH}",
                $"{texturesFolder}/{KEYBOARD_MOUSE_DIAGRAM}.png");
            InputGlyphSetBuilder.BuildFromSelectedFolder(PACK);
        }

        [MenuItem(FakeMGEditorMenus.INPUT_HINTS_BUILD_XELU_GLYPH_SETS, true)]
        private static bool IsFolderSelected()
        {
            return InputGlyphSetBuilder.IsFolderSelected();
        }

        private static Dictionary<string, string> KeyboardMouse()
        {
            var glyphs = new Dictionary<string, string>
            {
                ["space"] = Key("Space"),
                ["enter"] = Key("Enter"),
                ["numpadEnter"] = Key("Enter"),
                ["tab"] = Key("Tab"),
                ["escape"] = Key("Esc"),
                ["backspace"] = Key("Backspace"),
                ["delete"] = Key("Del"),
                ["insert"] = Key("Insert"),
                ["home"] = Key("Home"),
                ["end"] = Key("End"),
                ["pageUp"] = Key("Page_Up"),
                ["pageDown"] = Key("Page_Down"),
                ["leftShift"] = Key("Shift"),
                ["rightShift"] = Key("Shift"),
                ["shift"] = Key("Shift"),
                ["leftCtrl"] = Key("Ctrl"),
                ["rightCtrl"] = Key("Ctrl"),
                ["ctrl"] = Key("Ctrl"),
                ["leftAlt"] = Key("Alt"),
                ["rightAlt"] = Key("Alt"),
                ["alt"] = Key("Alt"),
                ["leftMeta"] = Key("Win"),
                ["rightMeta"] = Key("Win"),
                ["capsLock"] = Key("Caps_Lock"),
                ["numLock"] = Key("Num_Lock"),
                ["printScreen"] = Key("Print_Screen"),
                ["upArrow"] = Key("Arrow_Up"),
                ["downArrow"] = Key("Arrow_Down"),
                ["leftArrow"] = Key("Arrow_Left"),
                ["rightArrow"] = Key("Arrow_Right"),
                ["backquote"] = Key("Tilda"),
                ["quote"] = Key("Quote"),
                ["semicolon"] = Key("Semicolon"),
                ["slash"] = Key("Slash"),
                ["leftBracket"] = Key("Bracket_Left"),
                ["rightBracket"] = Key("Bracket_Right"),
                ["minus"] = Key("Minus"),
                ["numpadMinus"] = Key("Minus"),
                ["numpadPlus"] = Key("Plus"),
                ["numpadMultiply"] = Key("Asterisk"),

                ["leftButton"] = Key("Mouse_Left"),
                ["rightButton"] = Key("Mouse_Right"),
                ["middleButton"] = Key("Mouse_Middle"),
                ["position"] = Key("Mouse_Simple"),
                ["delta"] = Key("Mouse_Simple"),
            };

            // Xelu draws no scroll wheel of its own; the wheel is the middle button, so it reads as one.
            foreach (string scrollPath in new[] { "scroll", "scroll/y", "scroll/up", "scroll/down" })
            {
                glyphs[scrollPath] = Key("Mouse_Middle");
            }

            for (char letter = 'a'; letter <= 'z'; letter++)
            {
                glyphs[letter.ToString()] = Key(char.ToUpperInvariant(letter).ToString());
            }

            for (int digit = 0; digit <= 9; digit++)
            {
                glyphs[digit.ToString()] = Key(digit.ToString());
                glyphs[$"numpad{digit}"] = Key(digit.ToString());
            }

            for (int function = 1; function <= 12; function++)
            {
                glyphs[$"f{function}"] = Key($"F{function}");
            }

            return glyphs;
        }

        private static string Key(string keyName)
        {
            return $"{keyName}_Key_Dark";
        }

        private static Dictionary<string, string> Xbox()
        {
            Dictionary<string, string> glyphs = SticksAndDpad("XboxSeriesX");
            glyphs["buttonSouth"] = "XboxSeriesX_A";
            glyphs["buttonEast"] = "XboxSeriesX_B";
            glyphs["buttonWest"] = "XboxSeriesX_X";
            glyphs["buttonNorth"] = "XboxSeriesX_Y";
            glyphs["leftShoulder"] = "XboxSeriesX_LB";
            glyphs["rightShoulder"] = "XboxSeriesX_RB";
            glyphs["leftTrigger"] = "XboxSeriesX_LT";
            glyphs["rightTrigger"] = "XboxSeriesX_RT";
            glyphs["start"] = "XboxSeriesX_Menu";
            glyphs["select"] = "XboxSeriesX_View";
            return glyphs;
        }

        /// <param name="prefix">File name prefix of the console, e.g. "PS5".</param>
        private static Dictionary<string, string> PlayStation(string prefix)
        {
            Dictionary<string, string> glyphs = SticksAndDpad(prefix);
            glyphs["buttonSouth"] = $"{prefix}_Cross";
            glyphs["buttonEast"] = $"{prefix}_Circle";
            glyphs["buttonWest"] = $"{prefix}_Square";
            glyphs["buttonNorth"] = $"{prefix}_Triangle";
            glyphs["leftShoulder"] = $"{prefix}_L1";
            glyphs["rightShoulder"] = $"{prefix}_R1";
            glyphs["leftTrigger"] = $"{prefix}_L2";
            glyphs["rightTrigger"] = $"{prefix}_R2";
            glyphs["start"] = $"{prefix}_Options";
            glyphs["select"] = $"{prefix}_Share";
            glyphs["touchpadButton"] = $"{prefix}_Touch_Pad";
            return glyphs;
        }

        /// <remarks>Gamepad buttons are positional: the bottom face button, buttonSouth, is labelled B on a Switch controller.</remarks>
        private static Dictionary<string, string> Switch()
        {
            Dictionary<string, string> glyphs = SticksAndDpad("Switch");
            glyphs["buttonSouth"] = "Switch_B";
            glyphs["buttonEast"] = "Switch_A";
            glyphs["buttonWest"] = "Switch_Y";
            glyphs["buttonNorth"] = "Switch_X";
            glyphs["leftShoulder"] = "Switch_LB";
            glyphs["rightShoulder"] = "Switch_RB";
            glyphs["leftTrigger"] = "Switch_LT";
            glyphs["rightTrigger"] = "Switch_RT";
            glyphs["start"] = "Switch_Plus";
            glyphs["select"] = "Switch_Minus";
            return glyphs;
        }

        /// <remarks>Xelu draws a stick without directions, so every direction of a stick shares its glyph.</remarks>
        private static Dictionary<string, string> SticksAndDpad(string prefix)
        {
            var glyphs = new Dictionary<string, string>();
            AddStick(glyphs, "leftStick", $"{prefix}_Left_Stick");
            AddStick(glyphs, "rightStick", $"{prefix}_Right_Stick");
            glyphs["leftStickPress"] = $"{prefix}_Left_Stick_Click";
            glyphs["rightStickPress"] = $"{prefix}_Right_Stick_Click";
            glyphs["dpad"] = $"{prefix}_Dpad";
            glyphs["dpad/up"] = $"{prefix}_Dpad_Up";
            glyphs["dpad/down"] = $"{prefix}_Dpad_Down";
            glyphs["dpad/left"] = $"{prefix}_Dpad_Left";
            glyphs["dpad/right"] = $"{prefix}_Dpad_Right";
            return glyphs;
        }

        private static void AddStick(Dictionary<string, string> glyphs, string controlPath, string glyphFileName)
        {
            glyphs[controlPath] = glyphFileName;
            foreach (string direction in new[] { "up", "down", "left", "right", "x", "y" })
            {
                glyphs[$"{controlPath}/{direction}"] = glyphFileName;
            }
        }

        private static DiagramHotspotPixels[] XboxHotspots()
        {
            return new DiagramHotspotPixels[]
            {
                new("buttonNorth", 1093, 318, 100, 100),
                new("buttonEast", 1187, 403, 100, 100),
                new("buttonWest", 995, 410, 100, 100),
                new("buttonSouth", 1088, 492, 100, 100),
                new("leftStick", 377, 422, 180, 180),
                new("rightStick", 913, 622, 180, 180),
                new("dpad", 550, 615, 240, 210),
                new("dpad/up", 550, 560, 80, 80),
                new("dpad/down", 550, 672, 80, 80),
                new("dpad/left", 485, 617, 80, 80),
                new("dpad/right", 617, 617, 80, 80),
                new("select", 630, 410, 72, 72),
                new("start", 831, 410, 72, 72),
                new("leftShoulder", 385, 172, 300, 70),
                new("rightShoulder", 1067, 172, 300, 70),
                new("leftTrigger", 350, 82, 130, 110),
                new("rightTrigger", 1102, 82, 130, 110),
            };
        }

        private static DiagramHotspotPixels[] PlayStation5Hotspots()
        {
            return new DiagramHotspotPixels[]
            {
                new("buttonNorth", 1155, 355, 105, 105),
                new("buttonEast", 1255, 447, 105, 105),
                new("buttonWest", 1052, 447, 105, 105),
                new("buttonSouth", 1155, 537, 105, 105),
                new("dpad", 308, 447, 240, 240),
                new("dpad/up", 305, 378, 85, 85),
                new("dpad/down", 310, 515, 85, 85),
                new("dpad/left", 240, 450, 85, 85),
                new("dpad/right", 375, 450, 85, 85),
                new("leftStick", 520, 640, 180, 180),
                new("rightStick", 940, 640, 180, 180),
                new("select", 409, 302, 60, 95),
                new("start", 1045, 302, 60, 95),
                new("leftShoulder", 305, 192, 215, 80),
                new("rightShoulder", 1147, 192, 215, 80),
                new("leftTrigger", 320, 110, 180, 100),
                new("rightTrigger", 1132, 110, 180, 100),
                new("touchpadButton", 726, 350, 500, 260),
            };
        }

        private static DiagramHotspotPixels[] PlayStation4Hotspots()
        {
            return new DiagramHotspotPixels[]
            {
                new("buttonNorth", 1140, 317, 100, 100),
                new("buttonEast", 1233, 405, 100, 100),
                new("buttonWest", 1043, 405, 100, 100),
                new("buttonSouth", 1138, 493, 100, 100),
                new("dpad", 320, 400, 250, 250),
                new("dpad/up", 320, 340, 85, 95),
                new("dpad/down", 320, 460, 85, 95),
                new("dpad/left", 260, 400, 95, 85),
                new("dpad/right", 380, 400, 95, 85),
                new("leftStick", 522, 600, 180, 180),
                new("rightStick", 935, 600, 180, 180),
                new("select", 460, 277, 60, 95),
                new("start", 998, 277, 60, 95),
                new("leftShoulder", 328, 155, 180, 60),
                new("rightShoulder", 1125, 155, 180, 60),
                new("leftTrigger", 335, 103, 150, 65),
                new("rightTrigger", 1117, 103, 150, 65),
                new("touchpadButton", 727, 312, 440, 270),
            };
        }

        #endregion
    }
}
