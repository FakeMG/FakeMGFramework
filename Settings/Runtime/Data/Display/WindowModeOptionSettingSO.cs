using System;
using System.Collections.Generic;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.Settings.Display
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.SETTINGS_DISPLAY + "/Window Mode Option Setting SO")]
    public class WindowModeOptionSettingSO : OptionSettingSO
    {
        private static readonly SettingOption[] WINDOW_MODE_OPTIONS =
        {
            new(nameof(FullScreenMode.ExclusiveFullScreen), "Fullscreen"),
            new(nameof(FullScreenMode.FullScreenWindow), "Borderless"),
            new(nameof(FullScreenMode.Windowed), "Windowed")
        };

        public override IReadOnlyList<SettingOption> GetOptions()
        {
            return WINDOW_MODE_OPTIONS;
        }

        public FullScreenMode GetFullScreenMode(string optionId)
        {
            return Enum.Parse<FullScreenMode>(WINDOW_MODE_OPTIONS[IndexOfOptionOrDefault(optionId)].Id);
        }
    }
}
