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

        [Inject] private readonly SettingDataManager _settingDataManager;

        private bool _isApplyingStoredValue;

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
            {
                return;
            }

            float storedValue = ConvertToggleStateToStoredValue(isOn);
            _settingDataManager.SetValue(_sliderSetting, storedValue);
        }

        private void StoreNormalizedValueIfNeeded(float storedValue, bool isOn)
        {
            float normalizedValue = ConvertToggleStateToStoredValue(isOn);
            if (Mathf.Approximately(storedValue, normalizedValue))
            {
                return;
            }

            _settingDataManager.SetValue(_sliderSetting, normalizedValue);
        }

        private float ClampStoredValue(float storedValue)
        {
            return Mathf.Clamp(storedValue, _sliderSetting.StorageMinValue, _sliderSetting.StorageMaxValue);
        }

        private bool ConvertStoredValueToToggleState(float storedValue)
        {
            if (Mathf.Approximately(_sliderSetting.StorageMinValue, _sliderSetting.StorageMaxValue))
            {
                return false;
            }

            float thresholdValue = (_sliderSetting.StorageMinValue + _sliderSetting.StorageMaxValue) * 0.5f;
            return storedValue >= thresholdValue;
        }

        private float ConvertToggleStateToStoredValue(bool isOn)
        {
            return isOn ? _sliderSetting.StorageMaxValue : _sliderSetting.StorageMinValue;
        }
    }
}

