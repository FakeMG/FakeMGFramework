using System;
using System.Collections.Generic;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.Settings.Display
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.SETTINGS_DISPLAY + "/Window Mode Option Setting")]
    public class WindowModeOptionSettingSO : OptionSettingSO
    {
        private static readonly string[] WINDOW_MODE_OPTIONS =
        {
            "Fullscreen",
            "Borderless",
            "Windowed"
        };

        private static readonly FullScreenMode[] WINDOW_MODES =
        {
            FullScreenMode.ExclusiveFullScreen,
            FullScreenMode.FullScreenWindow,
            FullScreenMode.Windowed
        };

        public override List<string> GetOptions()
        {
            return new List<string>(WINDOW_MODE_OPTIONS);
        }

        /// <remarks>An unknown label falls back to the first option, matching how the dropdown resolves it.</remarks>
        public FullScreenMode GetFullScreenMode(string optionLabel)
        {
            int optionIndex = Array.IndexOf(WINDOW_MODE_OPTIONS, optionLabel);
            return WINDOW_MODES[Mathf.Max(optionIndex, 0)];
        }
    }
}
