using FakeMG.Framework.UI.Toggle;
using TMPro;
using UnityEngine;
using VContainer;

namespace FakeMG.Settings
{
    public class ToggleSwitchSettingUIBinder : MonoBehaviour
    {
        [SerializeField] private SliderSettingSO _sliderSetting;
        [SerializeField] private ToggleSwitch _toggleSwitch;
        [SerializeField] private TMP_Text _labelText;
        [SerializeField] private bool _revert;

        [Inject] private readonly SettingDataManager _settingDataManager;

        private bool _isApplyingStoredValue;

        #region Unity Lifecycle

        private void OnEnable()
        {
            _toggleSwitch.OnValueChanged += StoreToggleState;
        }

        private void Start()
        {
            ApplyLabel();
            ApplyStoredValue();
        }

        private void OnDisable()
        {
            _toggleSwitch.OnValueChanged -= StoreToggleState;
        }

        #endregion

        #region Private Methods

        private void ApplyLabel()
        {
            _labelText.text = _sliderSetting.Label;
        }

        private void ApplyStoredValue()
        {
            float storedValue = ClampStoredValue(_settingDataManager.GetValue(_sliderSetting));
            bool isOn = ConvertStoredValueToToggleState(storedValue);

            _isApplyingStoredValue = true;
            _toggleSwitch.SetStateWithoutAnimation(isOn);
            _isApplyingStoredValue = false;

            StoreNormalizedValueIfNeeded(storedValue, isOn);
        }

        private void StoreToggleState(bool isOn)
        {
            if (_isApplyingStoredValue)
                return;

            float storedValue = ConvertToggleStateToStoredValue(isOn);
            _settingDataManager.SetValue(_sliderSetting, storedValue);
        }

        private void StoreNormalizedValueIfNeeded(float storedValue, bool isOn)
        {
            float normalizedValue = ConvertToggleStateToStoredValue(isOn);
            if (Mathf.Approximately(storedValue, normalizedValue))
                return;

            _settingDataManager.SetValue(_sliderSetting, normalizedValue);
        }

        private bool ConvertStoredValueToToggleState(float storedValue)
        {
            if (Mathf.Approximately(_sliderSetting.StorageMinValue, _sliderSetting.StorageMaxValue))
                return false;

            float thresholdValue = (_sliderSetting.StorageMinValue + _sliderSetting.StorageMaxValue) * 0.5f;
            bool isOn = storedValue >= thresholdValue;
            return _revert ? !isOn : isOn;
        }

        private float ConvertToggleStateToStoredValue(bool isOn)
        {
            bool isEnabled = _revert ? !isOn : isOn;
            return isEnabled ? _sliderSetting.StorageMaxValue : _sliderSetting.StorageMinValue;
        }

        private float ClampStoredValue(float storedValue)
        {
            return Mathf.Clamp(storedValue, _sliderSetting.StorageMinValue, _sliderSetting.StorageMaxValue);
        }

        #endregion
    }
}
