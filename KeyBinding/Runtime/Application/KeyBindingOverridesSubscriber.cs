using System;
using FakeMG.Settings;
using VContainer.Unity;

namespace FakeMG.KeyBinding
{
    /// <summary>Applies the saved binding overrides at boot, and again whenever the settings restore or change them.</summary>
    public sealed class KeyBindingOverridesSubscriber : IStartable, IDisposable
    {
        private readonly SettingsStateRepository _settings;
        private readonly KeyBindingService _keyBindings;
        private readonly KeyBindingOverridesSettingSO _overridesSettingSO;

        public KeyBindingOverridesSubscriber(SettingsStateRepository settings, KeyBindingService keyBindings, KeyBindingConfigSO configSO)
        {
            _settings = settings;
            _keyBindings = keyBindings;
            _overridesSettingSO = configSO.KeyBindingOverridesSettingSO;
        }

        #region Public Methods

        public void Start()
        {
            _keyBindings.ApplyOverrides(_settings.GetValue(_overridesSettingSO));
            _settings.OnSettingChanged += ApplyChangedOverrides;
        }

        public void Dispose()
        {
            _settings.OnSettingChanged -= ApplyChangedOverrides;
        }

        #endregion

        #region Private Methods

        private void ApplyChangedOverrides(SettingDefinitionSO changedSettingSO)
        {
            if (changedSettingSO == _overridesSettingSO)
            {
                _keyBindings.ApplyOverrides(_settings.GetValue(_overridesSettingSO));
            }
        }

        #endregion
    }
}
