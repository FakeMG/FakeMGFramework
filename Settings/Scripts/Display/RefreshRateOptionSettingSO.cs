using System;
using System.Collections.Generic;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.Settings.Display
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.SETTINGS_DISPLAY + "/Refresh Rate Option Setting")]
    public class RefreshRateOptionSettingSO : OptionSettingSO
    {
        private const int DEFAULT_REFRESH_RATE_HZ = 60;
        private const int HERTZ_TO_MILLIHERTZ = 1000;
        private const string REFRESH_RATE_FORMAT = "{0}Hz";

        public override string GetDefaultValue()
        {
            List<int> refreshRateMilliHertzValues = GetRefreshRateMilliHertzValues();
            if (refreshRateMilliHertzValues.Count == 0)
            {
                return base.GetDefaultValue();
            }

            int currentRefreshRateMilliHertz = GetRefreshRateMilliHertz(Screen.currentResolution);
            for (int index = 0; index < refreshRateMilliHertzValues.Count; index++)
            {
                int refreshRateMilliHertz = refreshRateMilliHertzValues[index];
                if (refreshRateMilliHertz == currentRefreshRateMilliHertz)
                {
                    return FormatRefreshRateLabel(refreshRateMilliHertz);
                }
            }

            return string.Format(REFRESH_RATE_FORMAT, DEFAULT_REFRESH_RATE_HZ);
        }

        public override List<string> GetOptions()
        {
            List<int> refreshRateMilliHertzValues = GetRefreshRateMilliHertzValues();
            List<string> refreshRateOptions = new List<string>(refreshRateMilliHertzValues.Count);

            for (int index = 0; index < refreshRateMilliHertzValues.Count; index++)
            {
                int refreshRateHertz = refreshRateMilliHertzValues[index] / HERTZ_TO_MILLIHERTZ;
                string optionLabel = string.Format(REFRESH_RATE_FORMAT, refreshRateHertz);
                refreshRateOptions.Add(optionLabel);
            }

            return refreshRateOptions;
        }

        public int GetRefreshRateValue(int optionIndex)
        {
            List<int> refreshRateMilliHertzValues = GetRefreshRateMilliHertzValues();
            if (refreshRateMilliHertzValues.Count == 0)
            {
                return DEFAULT_REFRESH_RATE_HZ;
            }

            int clampedIndex = Mathf.Clamp(optionIndex, 0, refreshRateMilliHertzValues.Count - 1);
            return refreshRateMilliHertzValues[clampedIndex] / HERTZ_TO_MILLIHERTZ;
        }

        private static string FormatRefreshRateLabel(int refreshRateMilliHertz)
        {
            int refreshRateHertz = refreshRateMilliHertz / HERTZ_TO_MILLIHERTZ;
            return string.Format(REFRESH_RATE_FORMAT, refreshRateHertz);
        }

        private static List<int> GetRefreshRateMilliHertzValues()
        {
            Resolution[] availableResolutions = Screen.resolutions;
            List<int> refreshRateMilliHertzValues = new List<int>(availableResolutions.Length);
            HashSet<int> seenRefreshRatesMilliHertz = new HashSet<int>();

            for (int index = 0; index < availableResolutions.Length; index++)
            {
                int refreshRateMilliHertz = GetRefreshRateMilliHertz(availableResolutions[index]);

                if (seenRefreshRatesMilliHertz.Add(refreshRateMilliHertz))
                {
                    refreshRateMilliHertzValues.Add(refreshRateMilliHertz);
                }
            }

            refreshRateMilliHertzValues.Sort();
            return refreshRateMilliHertzValues;
        }

        private static int GetRefreshRateMilliHertz(Resolution resolution)
        {
            double refreshRateHertz = GetRefreshRateHertz(resolution);
            double refreshRateMilliHertz = refreshRateHertz * HERTZ_TO_MILLIHERTZ;
            return Convert.ToInt32(Math.Round(refreshRateMilliHertz, MidpointRounding.AwayFromZero));
        }

        private static double GetRefreshRateHertz(Resolution resolution)
        {
#if UNITY_2022_2_OR_NEWER
            return resolution.refreshRateRatio.value;
#else
            return resolution.refreshRate;
#endif
        }
    }
}