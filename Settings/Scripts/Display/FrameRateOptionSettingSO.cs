using System.Collections.Generic;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.Settings.Display
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.SETTINGS_DISPLAY + "/Frame Rate Option Setting")]
    public class FrameRateOptionSettingSO : OptionSettingSO
    {
        private const int DEFAULT_FRAME_RATE = 60;
        private const string FRAME_RATE_FORMAT = "{0} FPS";

        [SerializeField] private int[] _frameRateValues = { 30, 60, 120, 144, 240 };

        public override List<string> GetOptions()
        {
            List<string> frameRateOptions = new(_frameRateValues.Length);

            for (int index = 0; index < _frameRateValues.Length; index++)
            {
                int frameRate = _frameRateValues[index];
                string optionLabel = string.Format(FRAME_RATE_FORMAT, frameRate);
                frameRateOptions.Add(optionLabel);
            }

            return frameRateOptions;
        }

        public int GetFrameRateValue(int optionIndex)
        {
            if (_frameRateValues.Length == 0)
            {
                return DEFAULT_FRAME_RATE;
            }

            int clampedIndex = Mathf.Clamp(optionIndex, 0, _frameRateValues.Length - 1);
            return _frameRateValues[clampedIndex];
        }
    }
}