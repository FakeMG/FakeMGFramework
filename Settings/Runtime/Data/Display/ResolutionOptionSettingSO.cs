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
            Resolution currentResolution = Screen.currentResolution;
            string currentLabel = FormatResolutionLabel(currentResolution.width, currentResolution.height);
            return GetOptions().Contains(currentLabel) ? currentLabel : base.GetDefaultValue();
        }

        /// <remarks>Screen.resolutions lists each size once per refresh rate, so sizes are deduplicated.</remarks>
        public override List<string> GetOptions()
        {
            Resolution[] availableResolutions = Screen.resolutions;
            List<string> resolutionOptions = new(availableResolutions.Length);

            for (int index = 0; index < availableResolutions.Length; index++)
            {
                Resolution resolution = availableResolutions[index];
                string optionLabel = FormatResolutionLabel(resolution.width, resolution.height);
                if (!resolutionOptions.Contains(optionLabel))
                {
                    resolutionOptions.Add(optionLabel);
                }
            }

            return resolutionOptions;
        }

        public bool TryGetResolutionSize(string optionLabel, out Vector2Int sizePixels)
        {
            Resolution[] availableResolutions = Screen.resolutions;
            for (int index = 0; index < availableResolutions.Length; index++)
            {
                Resolution resolution = availableResolutions[index];
                if (FormatResolutionLabel(resolution.width, resolution.height) == optionLabel)
                {
                    sizePixels = new Vector2Int(resolution.width, resolution.height);
                    return true;
                }
            }

            sizePixels = default;
            return false;
        }

        private static string FormatResolutionLabel(int widthPixels, int heightPixels)
        {
            return string.Format(RESOLUTION_FORMAT, widthPixels, heightPixels);
        }
    }
}
