using System.Collections.Generic;
using System.Globalization;
using FakeMG.Framework;
using UnityEngine;
using UnityEngine.Serialization;

namespace FakeMG.Settings.Display
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.SETTINGS_DISPLAY + "/Frame Rate Option Setting SO")]
    public class FrameRateOptionSettingSO : OptionSettingSO
    {
        private const int DEFAULT_FRAME_RATE_FPS = 60;
        private const string FRAME_RATE_LABEL_FORMAT = "{0} FPS";

        [FormerlySerializedAs("_frameRateValues")]
        [SerializeField] private int[] _frameRateOptionsFps = { 30, 60, 120, 144, 240 };

        public override IReadOnlyList<SettingOption> GetOptions()
        {
            List<SettingOption> options = new(_frameRateOptionsFps.Length);
            foreach (int frameRateFps in _frameRateOptionsFps)
            {
                options.Add(new SettingOption(frameRateFps.ToString(CultureInfo.InvariantCulture), string.Format(FRAME_RATE_LABEL_FORMAT, frameRateFps)));
            }

            return options;
        }

        public int GetFrameRateFps(string optionId)
        {
            int optionIndex = IndexOfOptionOrDefault(optionId);
            return optionIndex == NO_OPTION_INDEX ? DEFAULT_FRAME_RATE_FPS : _frameRateOptionsFps[optionIndex];
        }
    }
}
