using FakeMG.Framework.UI.Toggle;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FakeMG.Settings
{
    public sealed class ToggleSwitchSettingUIBinder : SettingUIBinder
    {
        [Required, SerializeField] private BoolSettingSO _toggleSettingSO;
        [Required, SerializeField] private ToggleSwitch _toggleSwitch;

        public override SettingDefinitionSO SettingSO => _toggleSettingSO;

        #region Unity Lifecycle

        private void OnEnable()
        {
            _toggleSwitch.OnValueChanged += StoreToggleState;
        }

        private void OnDisable()
        {
            _toggleSwitch.OnValueChanged -= StoreToggleState;
        }

        #endregion

        #region Private Methods

        /// <remarks>Setting the state without animation raises no value change, so showing the value never stores it back.</remarks>
        protected override void ShowStoredValue()
        {
            _toggleSwitch.SetStateWithoutAnimation(Settings.GetValue(_toggleSettingSO));
        }

        private void StoreToggleState(bool isOn)
        {
            Settings.SetValue(_toggleSettingSO, isOn);
        }

        #endregion
    }
}
