using System;
using System.Collections.Generic;
using VContainer.Unity;

namespace FakeMG.Settings.Display
{
    /// <summary>Applies the display settings at boot, and again whenever one of them changes.</summary>
    public sealed class DisplaySettingsSubscriber : IStartable, IDisposable
    {
        private readonly SettingsStateRepository _settings;
        private readonly DisplaySettingsApplier _applier;
        private readonly Dictionary<SettingDefinitionSO, Action> _applyBySetting;

        public DisplaySettingsSubscriber(SettingsStateRepository settings, DisplaySettingsApplier applier, DisplaySettingsConfigSO configSO)
        {
            _settings = settings;
            _applier = applier;
            _applyBySetting = new Dictionary<SettingDefinitionSO, Action>
            {
                [configSO.WindowModeSettingSO] = applier.ApplyScreenMode,
                [configSO.ResolutionSettingSO] = applier.ApplyScreenMode,
                [configSO.VSyncSettingSO] = applier.ApplyFramePacing,
                [configSO.FrameRateSettingSO] = applier.ApplyFramePacing
            };
        }

        #region Public Methods

        public void Start()
        {
            _applier.ApplyScreenMode();
            _applier.ApplyFramePacing();
            _settings.OnSettingChanged += ApplyChangedDisplaySetting;
        }

        public void Dispose()
        {
            _settings.OnSettingChanged -= ApplyChangedDisplaySetting;
        }

        #endregion

        #region Private Methods

        private void ApplyChangedDisplaySetting(SettingDefinitionSO changedSettingSO)
        {
            if (_applyBySetting.TryGetValue(changedSettingSO, out Action apply))
            {
                apply();
            }
        }

        #endregion
    }
}
