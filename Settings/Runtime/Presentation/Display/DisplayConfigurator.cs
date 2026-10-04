using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.Settings.Display
{
    public class DisplayConfigurator
    {
        // 1. RESOLUTION (Hook to a Dropdown)
        // Pass the index from a list of Screen.resolutions
        public void SetResolution(int resolutionIndex)
        {
            Resolution[] resolutions = Screen.resolutions;
            Resolution res = resolutions[resolutionIndex];
            Screen.SetResolution(res.width, res.height, Screen.fullScreenMode);
        }

        // 2. WINDOW MODE (Hook to a Dropdown)
        // 0 = Fullscreen, 1 = Borderless, 2 = Windowed
        public void SetWindowMode(int modeIndex)
        {
            switch (modeIndex)
            {
                case 0: Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen; break;
                case 1: Screen.fullScreenMode = FullScreenMode.FullScreenWindow; break;
                case 2: Screen.fullScreenMode = FullScreenMode.Windowed; break;
            }
        }

        // 3. VSYNC (Hook to a Toggle)
        public void SetVSync(bool isOn)
        {
            QualitySettings.vSyncCount = isOn ? 1 : 0;

            // Technical Note: If VSync is ON, we usually set targetFrameRate to -1 
            // to let the system handle it based on the monitor's refresh rate.
            if (isOn) Application.targetFrameRate = -1;
        }

        // 4. FPS CAP (Hook to a Dropdown or Slider)
        public void SetFrameRate(int fps)
        {
            // VSync must be OFF for this to take effect
            if (QualitySettings.vSyncCount == 0)
            {
                Application.targetFrameRate = fps;
            }
        }

        // 5. TARGET MONITOR (Hook to a Dropdown)
        public void SetMonitor(int monitorIndex)
        {
            // 1. Get all available display layouts
            List<DisplayInfo> displayLayouts = new();
            Screen.GetDisplayLayout(displayLayouts);

            // 2. Check if the index is valid
            if (monitorIndex < displayLayouts.Count)
            {
                // 3. Move the window to the new display's coordinates
                // We use the display's work area or position to center it
                DisplayInfo targetDisplay = displayLayouts[monitorIndex];

                Screen.MoveMainWindowTo(targetDisplay, Screen.mainWindowPosition);
            }
            else
            {
                Debug.LogWarning("Monitor index out of range!");
            }
        }

        //TODO: FOV (Hook to a Slider)
    }
}