using System.Collections.Generic;
using System.Globalization;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.Settings.Display
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.SETTINGS_DISPLAY + "/Resolution Option Setting SO")]
    public class ResolutionOptionSettingSO : OptionSettingSO
    {
        private const char SIZE_SEPARATOR = 'x';
        private const string RESOLUTION_LABEL_FORMAT = "{0} x {1}";

        public override string GetDefaultValue()
        {
            Resolution currentResolution = Screen.currentResolution;
            string currentOptionId = FormatOptionId(currentResolution.width, currentResolution.height);
            return ContainsOption(currentOptionId) ? currentOptionId : base.GetDefaultValue();
        }

        /// <remarks>Screen.resolutions lists each size once per refresh rate, so sizes are deduplicated.</remarks>
        public override IReadOnlyList<SettingOption> GetOptions()
        {
            Resolution[] availableResolutions = Screen.resolutions;
            List<SettingOption> options = new(availableResolutions.Length);
            HashSet<string> seenOptionIds = new();
            foreach (Resolution resolution in availableResolutions)
            {
                string optionId = FormatOptionId(resolution.width, resolution.height);
                if (seenOptionIds.Add(optionId))
                {
                    options.Add(new SettingOption(optionId, string.Format(RESOLUTION_LABEL_FORMAT, resolution.width, resolution.height)));
                }
            }

            return options;
        }

        /// <returns>False when the id is malformed or the display no longer offers that size.</returns>
        public bool TryGetResolutionSize(string optionId, out Vector2Int sizePixels)
        {
            sizePixels = default;
            string[] sizeParts = optionId?.Split(SIZE_SEPARATOR);
            if (sizeParts == null || sizeParts.Length != 2 || !ContainsOption(optionId) ||
                !int.TryParse(sizeParts[0], NumberStyles.None, CultureInfo.InvariantCulture, out int widthPixels) ||
                !int.TryParse(sizeParts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int heightPixels))
            {
                return false;
            }

            sizePixels = new Vector2Int(widthPixels, heightPixels);
            return true;
        }

        private bool ContainsOption(string optionId)
        {
            foreach (SettingOption option in GetOptions())
            {
                if (option.Id == optionId)
                {
                    return true;
                }
            }

            return false;
        }

        private static string FormatOptionId(int widthPixels, int heightPixels)
        {
            return string.Concat(
                widthPixels.ToString(CultureInfo.InvariantCulture),
                SIZE_SEPARATOR.ToString(),
                heightPixels.ToString(CultureInfo.InvariantCulture));
        }
    }
}
