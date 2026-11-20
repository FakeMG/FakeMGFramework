using UnityEngine;

namespace FakeMG.Settings.Display
{
    /// <summary>Pushes the display settings into the engine's screen and frame pacing.</summary>
    /// <remarks>Window mode and resolution have no effect inside the editor; they only apply in a build.</remarks>
    public sealed class DisplaySettingsApplier
    {
        private const int UNCAPPED_FRAME_RATE_FPS = -1;
        private const int VSYNC_EVERY_FRAME = 1;
        private const int VSYNC_OFF = 0;

        private readonly SettingsStateRepository _settings;
        private readonly DisplaySettingsConfigSO _configSO;

        public DisplaySettingsApplier(SettingsStateRepository settings, DisplaySettingsConfigSO configSO)
        {
            _settings = settings;
            _configSO = configSO;
        }

        #region Public Methods

        public void ApplyScreenMode()
        {
            FullScreenMode windowMode = _configSO.WindowModeSettingSO.GetFullScreenMode(_settings.GetValue(_configSO.WindowModeSettingSO));
            string resolutionOptionId = _settings.GetValue(_configSO.ResolutionSettingSO);
            if (!_configSO.ResolutionSettingSO.TryGetResolutionSize(resolutionOptionId, out Vector2Int sizePixels))
            {
                Screen.fullScreenMode = windowMode;
                return;
            }

            Screen.SetResolution(sizePixels.x, sizePixels.y, windowMode);
        }

        /// <remarks>VSync paces frames to the display, so the frame rate cap only applies while it is off.</remarks>
        public void ApplyFramePacing()
        {
            bool isVSyncOn = _settings.GetValue(_configSO.VSyncSettingSO);
            QualitySettings.vSyncCount = isVSyncOn ? VSYNC_EVERY_FRAME : VSYNC_OFF;
            Application.targetFrameRate = isVSyncOn
                ? UNCAPPED_FRAME_RATE_FPS
                : _configSO.FrameRateSettingSO.GetFrameRateFps(_settings.GetValue(_configSO.FrameRateSettingSO));
        }

        #endregion
    }
}
