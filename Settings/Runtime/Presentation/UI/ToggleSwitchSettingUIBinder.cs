using FakeMG.Framework.UI.Toggle;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using VContainer;

namespace FakeMG.Settings
{
    public class ToggleSwitchSettingUIBinder : MonoBehaviour
    {
        [Required, SerializeField] private SliderSettingSO _sliderSettingSO;
        [Required, SerializeField] private ToggleSwitch _toggleSwitch;
        [Required, SerializeField] private TMP_Text _labelText;
        [SerializeField] private bool _revert;

        [Inject] private readonly SettingsStateRepository _settingsStateRepository;

        private bool _isApplyingStoredValue;

        #region Unity Lifecycle

        private void OnEnable()
        {
            _toggleSwitch.OnValueChanged += StoreToggleState;
        }

        private void Start()
        {
            _settingsStateRepository.RegisterSetting(_sliderSettingSO);
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
            _labelText.text = _sliderSettingSO.Label;
        }

        private void ApplyStoredValue()
        {
            float storedValue = ClampStoredValue(_settingsStateRepository.GetValue(_sliderSettingSO));
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
            _settingsStateRepository.SetValue(_sliderSettingSO, storedValue);
        }

        private void StoreNormalizedValueIfNeeded(float storedValue, bool isOn)
        {
            float normalizedValue = ConvertToggleStateToStoredValue(isOn);
            if (Mathf.Approximately(storedValue, normalizedValue))
                return;

            _settingsStateRepository.SetValue(_sliderSettingSO, normalizedValue);
        }

        private bool ConvertStoredValueToToggleState(float storedValue)
        {
            if (Mathf.Approximately(_sliderSettingSO.StorageMinValue, _sliderSettingSO.StorageMaxValue))
                return false;

            float thresholdValue = (_sliderSettingSO.StorageMinValue + _sliderSettingSO.StorageMaxValue) * 0.5f;
            bool isOn = storedValue >= thresholdValue;
            return _revert ? !isOn : isOn;
        }

        private float ConvertToggleStateToStoredValue(bool isOn)
        {
            bool isEnabled = _revert ? !isOn : isOn;
            return isEnabled ? _sliderSettingSO.StorageMaxValue : _sliderSettingSO.StorageMinValue;
        }

        private float ClampStoredValue(float storedValue)
        {
            return Mathf.Clamp(storedValue, _sliderSettingSO.StorageMinValue, _sliderSettingSO.StorageMaxValue);
        }

        #endregion
    }
}
