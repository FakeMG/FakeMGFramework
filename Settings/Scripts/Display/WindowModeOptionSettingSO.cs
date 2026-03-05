using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.Settings.Display
{
    [CreateAssetMenu(menuName = "Settings/Display/Window Mode Option Setting")]
    public class WindowModeOptionSettingSO : OptionSettingSO
    {
        private static readonly string[] WINDOW_MODE_OPTIONS =
        {
            "Fullscreen",
            "Borderless",
            "Windowed"
        };

        public override List<string> GetOptions()
        {
            List<string> windowModeOptions = new(WINDOW_MODE_OPTIONS.Length);

            for (int index = 0; index < WINDOW_MODE_OPTIONS.Length; index++)
            {
                windowModeOptions.Add(WINDOW_MODE_OPTIONS[index]);
            }

            return windowModeOptions;
        }
    }
}