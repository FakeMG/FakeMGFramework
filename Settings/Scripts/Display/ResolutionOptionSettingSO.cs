using System.Collections.Generic;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.Settings.Display
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.SETTINGS_DISPLAY + "/Resolution Option Setting")]
    public class ResolutionOptionSettingSO : OptionSettingSO
    {
        private const string RESOLUTION_FORMAT = "{0} x {1}";

        public override string GetDefaultValue()
        {
            Resolution[] availableResolutions = Screen.resolutions;
            Resolution currentResolution = Screen.currentResolution;

            for (int index = 0; index < availableResolutions.Length; index++)
            {
                Resolution resolution = availableResolutions[index];
                if (resolution.width != currentResolution.width)
                {
                    continue;
                }

                if (resolution.height != currentResolution.height)
                {
                    continue;
                }

                return FormatResolutionLabel(resolution);
            }

            return base.GetDefaultValue();
        }

        public override List<string> GetOptions()
        {
            Resolution[] availableResolutions = Screen.resolutions;
            List<string> resolutionOptions = new(availableResolutions.Length);

            for (int index = 0; index < availableResolutions.Length; index++)
            {
                Resolution resolution = availableResolutions[index];
                string optionLabel = string.Format(
                    RESOLUTION_FORMAT,
                    resolution.width,
                    resolution.height);

                resolutionOptions.Add(optionLabel);
            }

            return resolutionOptions;
        }

        private static string FormatResolutionLabel(Resolution resolution)
        {
            return string.Format(RESOLUTION_FORMAT, resolution.width, resolution.height);
        }
    }
}