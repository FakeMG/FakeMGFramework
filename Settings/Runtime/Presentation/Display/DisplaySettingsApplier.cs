using System;
using UnityEngine;
using VContainer.Unity;

namespace FakeMG.Settings.Display
{
    /// <summary>Registers the display settings at boot: their UI rows only register them once the settings screen is first shown.</summary>
    /// <remarks>Window mode and resolution have no effect inside the editor; they only apply in a build.</remarks>
    public sealed class DisplaySettingsApplier : IStartable, IDisposable
    {
        private const int UNCAPPED_FRAME_RATE = -1;
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

        public void Start()
        {
            _settings.RegisterSetting(_configSO.WindowModeSettingSO);
            _settings.RegisterSetting(_configSO.ResolutionSettingSO);
            _settings.RegisterSetting(_configSO.VSyncSettingSO);
            _settings.RegisterSetting(_configSO.FrameRateSettingSO);

            ApplyScreenMode();
            ApplyFramePacing();

            _settings.Subscribe(_configSO.WindowModeSettingSO, ApplyScreenModeWhenSettingChanged);
            _settings.Subscribe(_configSO.ResolutionSettingSO, ApplyScreenModeWhenSettingChanged);
            _settings.Subscribe(_configSO.VSyncSettingSO, ApplyFramePacingWhenSettingChanged);
            _settings.Subscribe(_configSO.FrameRateSettingSO, ApplyFramePacingWhenSettingChanged);
        }

        public void Dispose()
        {
            _settings.Unsubscribe(_configSO.WindowModeSettingSO, ApplyScreenModeWhenSettingChanged);
            _settings.Unsubscribe(_configSO.ResolutionSettingSO, ApplyScreenModeWhenSettingChanged);
            _settings.Unsubscribe(_configSO.VSyncSettingSO, ApplyFramePacingWhenSettingChanged);
            _settings.Unsubscribe(_configSO.FrameRateSettingSO, ApplyFramePacingWhenSettingChanged);
        }

        #endregion

        #region Private Methods

        private void ApplyScreenModeWhenSettingChanged(SettingDefinitionGenericSO<string> settingSO, string optionLabel)
        {
            ApplyScreenMode();
        }

        private void ApplyFramePacingWhenSettingChanged(SettingDefinitionGenericSO<string> settingSO, string optionLabel)
        {
            ApplyFramePacing();
        }

        private void ApplyScreenMode()
        {
            FullScreenMode windowMode = _configSO.WindowModeSettingSO.GetFullScreenMode(_settings.GetValue(_configSO.WindowModeSettingSO));
            string resolutionLabel = _settings.GetValue(_configSO.ResolutionSettingSO);
            if (!_configSO.ResolutionSettingSO.TryGetResolutionSize(resolutionLabel, out Vector2Int sizePixels))
            {
                Screen.fullScreenMode = windowMode;
                return;
            }

            Screen.SetResolution(sizePixels.x, sizePixels.y, windowMode);
        }

        private void ApplyFramePacing()
        {
            bool isVSyncOn = _configSO.VSyncSettingSO.IsOn(_settings.GetValue(_configSO.VSyncSettingSO));
            QualitySettings.vSyncCount = isVSyncOn ? VSYNC_EVERY_FRAME : VSYNC_OFF;
            Application.targetFrameRate = isVSyncOn
                ? UNCAPPED_FRAME_RATE
                : _configSO.FrameRateSettingSO.GetFrameRateValue(_settings.GetValue(_configSO.FrameRateSettingSO));
        }

        #endregion
    }
}
