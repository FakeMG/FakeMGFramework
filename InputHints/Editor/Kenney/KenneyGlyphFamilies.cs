using System.Collections.Generic;

namespace FakeMG.InputHints.Editor
{
    /// <summary>
    /// How the Kenney Input Prompts pack names its glyphs, per family. The only place that knows the pack: another glyph
    /// pack needs its own table like this one, and nothing at runtime changes.
    /// </summary>
    internal static class KenneyGlyphFamilies
    {
        /// <summary>Library order: specific gamepads before Xbox, which also draws every other gamepad.</summary>
        public static readonly KenneyGlyphFamily[] LIBRARY_ORDER =
        {
            new("PlayStation Series", "Kenney PlayStation 5 Glyph Set SO", new[] { "DualSenseGamepadHID" },
                PlayStation("playstation5_button_create", "playstation5_button_options")),
            new("PlayStation Series", "Kenney PlayStation 4 Glyph Set SO", new[] { "DualShockGamepad" },
                PlayStation("playstation4_button_share", "playstation4_button_options")),
            new("Nintendo Switch", "Kenney Switch Glyph Set SO", new[] { "SwitchProControllerHID" }, Switch()),
            new("Keyboard & Mouse", "Kenney Keyboard Mouse Glyph Set SO", new[] { "Keyboard", "Mouse" }, KeyboardMouse()),
            new("Xbox Series", "Kenney Xbox Glyph Set SO", new[] { "XInputController", "Gamepad" }, Xbox()),
        };

        #region Private Methods

        private static Dictionary<string, string> KeyboardMouse()
        {
            var icons = new Dictionary<string, string>
            {
                ["space"] = "keyboard_space",
                ["enter"] = "keyboard_enter",
                ["numpadEnter"] = "keyboard_numpad_enter",
                ["tab"] = "keyboard_tab",
                ["escape"] = "keyboard_escape",
                ["backspace"] = "keyboard_backspace",
                ["delete"] = "keyboard_delete",
                ["insert"] = "keyboard_insert",
                ["home"] = "keyboard_home",
                ["end"] = "keyboard_end",
                ["pageUp"] = "keyboard_page_up",
                ["pageDown"] = "keyboard_page_down",
                ["leftShift"] = "keyboard_shift",
                ["rightShift"] = "keyboard_shift",
                ["shift"] = "keyboard_shift",
                ["leftCtrl"] = "keyboard_ctrl",
                ["rightCtrl"] = "keyboard_ctrl",
                ["ctrl"] = "keyboard_ctrl",
                ["leftAlt"] = "keyboard_alt",
                ["rightAlt"] = "keyboard_alt",
                ["alt"] = "keyboard_alt",
                ["leftMeta"] = "keyboard_win",
                ["rightMeta"] = "keyboard_win",
                ["capsLock"] = "keyboard_capslock",
                ["numLock"] = "keyboard_numlock",
                ["scrollLock"] = "keyboard_scroll_lock",
                ["printScreen"] = "keyboard_printscreen",
                ["pause"] = "keyboard_pause",
                ["upArrow"] = "keyboard_arrow_up",
                ["downArrow"] = "keyboard_arrow_down",
                ["leftArrow"] = "keyboard_arrow_left",
                ["rightArrow"] = "keyboard_arrow_right",
                ["backquote"] = "keyboard_tilde",
                ["quote"] = "keyboard_apostrophe",
                ["semicolon"] = "keyboard_semicolon",
                ["comma"] = "keyboard_comma",
                ["period"] = "keyboard_period",
                ["slash"] = "keyboard_slash_forward",
                ["backslash"] = "keyboard_slash_back",
                ["leftBracket"] = "keyboard_bracket_open",
                ["rightBracket"] = "keyboard_bracket_close",
                ["minus"] = "keyboard_minus",
                ["equals"] = "keyboard_equals",
                ["numpadPlus"] = "keyboard_numpad_plus",
                ["numpadMultiply"] = "keyboard_asterisk",
                ["anyKey"] = "keyboard_any",

                ["leftButton"] = "mouse_left",
                ["rightButton"] = "mouse_right",
                ["middleButton"] = "mouse_scroll",
                ["forwardButton"] = "mouse_side_forward",
                ["backButton"] = "mouse_side_back",
                ["scroll"] = "mouse_scroll_vertical",
                ["scroll/y"] = "mouse_scroll_vertical",
                ["scroll/up"] = "mouse_scroll_up",
                ["scroll/down"] = "mouse_scroll_down",
                ["position"] = "mouse_move",
                ["delta"] = "mouse_move",
                ["delta/x"] = "mouse_horizontal",
                ["delta/y"] = "mouse_vertical",
            };

            for (char letter = 'a'; letter <= 'z'; letter++)
            {
                icons[letter.ToString()] = $"keyboard_{letter}";
            }

            for (int digit = 0; digit <= 9; digit++)
            {
                icons[digit.ToString()] = $"keyboard_{digit}";
                icons[$"numpad{digit}"] = $"keyboard_{digit}";
            }

            for (int function = 1; function <= 12; function++)
            {
                icons[$"f{function}"] = $"keyboard_f{function}";
            }

            return icons;
        }

        private static Dictionary<string, string> Xbox()
        {
            Dictionary<string, string> icons = SticksAndDpad("xbox_stick_l", "xbox_stick_r", "xbox_dpad");
            icons["buttonSouth"] = "xbox_button_a";
            icons["buttonEast"] = "xbox_button_b";
            icons["buttonWest"] = "xbox_button_x";
            icons["buttonNorth"] = "xbox_button_y";
            icons["leftShoulder"] = "xbox_lb";
            icons["rightShoulder"] = "xbox_rb";
            icons["leftTrigger"] = "xbox_lt";
            icons["rightTrigger"] = "xbox_rt";
            icons["start"] = "xbox_button_menu";
            icons["select"] = "xbox_button_view";
            return icons;
        }

        private static Dictionary<string, string> PlayStation(string selectIconName, string startIconName)
        {
            Dictionary<string, string> icons = SticksAndDpad("playstation_stick_l", "playstation_stick_r", "playstation_dpad");
            icons["buttonSouth"] = "playstation_button_cross";
            icons["buttonEast"] = "playstation_button_circle";
            icons["buttonWest"] = "playstation_button_square";
            icons["buttonNorth"] = "playstation_button_triangle";
            icons["leftShoulder"] = "playstation_trigger_l1";
            icons["rightShoulder"] = "playstation_trigger_r1";
            icons["leftTrigger"] = "playstation_trigger_l2";
            icons["rightTrigger"] = "playstation_trigger_r2";
            icons["start"] = startIconName;
            icons["select"] = selectIconName;
            return icons;
        }

        /// <remarks>Gamepad buttons are positional: the bottom face button, buttonSouth, is labelled B on a Switch controller.</remarks>
        private static Dictionary<string, string> Switch()
        {
            Dictionary<string, string> icons = SticksAndDpad("switch_stick_l", "switch_stick_r", "switch_dpad");
            icons["buttonSouth"] = "switch_button_b";
            icons["buttonEast"] = "switch_button_a";
            icons["buttonWest"] = "switch_button_y";
            icons["buttonNorth"] = "switch_button_x";
            icons["leftShoulder"] = "switch_button_l";
            icons["rightShoulder"] = "switch_button_r";
            icons["leftTrigger"] = "switch_button_zl";
            icons["rightTrigger"] = "switch_button_zr";
            icons["start"] = "switch_button_plus";
            icons["select"] = "switch_button_minus";
            return icons;
        }

        private static Dictionary<string, string> SticksAndDpad(string leftStickIconName, string rightStickIconName, string dpadIconName)
        {
            var icons = new Dictionary<string, string>();
            AddDirectional(icons, "leftStick", leftStickIconName);
            AddDirectional(icons, "rightStick", rightStickIconName);
            AddDirectional(icons, "dpad", dpadIconName);
            icons["leftStickPress"] = $"{leftStickIconName}_press";
            icons["rightStickPress"] = $"{rightStickIconName}_press";
            return icons;
        }

        private static void AddDirectional(Dictionary<string, string> icons, string controlPath, string iconName)
        {
            icons[controlPath] = iconName;
            icons[$"{controlPath}/up"] = $"{iconName}_up";
            icons[$"{controlPath}/down"] = $"{iconName}_down";
            icons[$"{controlPath}/left"] = $"{iconName}_left";
            icons[$"{controlPath}/right"] = $"{iconName}_right";
            icons[$"{controlPath}/x"] = $"{iconName}_horizontal";
            icons[$"{controlPath}/y"] = $"{iconName}_vertical";
        }

        #endregion
    }
}
