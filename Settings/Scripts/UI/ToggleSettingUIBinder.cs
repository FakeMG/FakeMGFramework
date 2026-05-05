using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace FakeMG.Settings
{
    public class ToggleSettingUIBinder : MonoBehaviour
    {
        [SerializeField] private SliderSettingSO _sliderSetting;
        [SerializeField] private Toggle _toggle;
        [SerializeField] private TMP_Text _labelText;
        [SerializeField] private bool _revert;

        [Inject] private readonly SettingDataManager _settingDataManager;

        #region Unity Lifecycle

        private void OnEnable()
        {
            _toggle.onValueChanged.AddListener(StoreToggleState);
        }

        private void Start()
        {
            ApplyLabel();
            ApplyStoredValue();
        }

        private void OnDisable()
        {
            _toggle.onValueChanged.RemoveListener(StoreToggleState);
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

            _toggle.SetIsOnWithoutNotify(isOn);
            StoreNormalizedValueIfNeeded(storedValue, isOn);
        }

        private void StoreToggleState(bool isOn)
        {
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
            bool isOn = storedValue >= thresholdValue;
            return _revert ? !isOn : isOn;
        }

        private float ConvertToggleStateToStoredValue(bool isOn)
        {
            bool isEnabled = _revert ? !isOn : isOn;
            return isEnabled ? _sliderSetting.StorageMaxValue : _sliderSetting.StorageMinValue;
        }

        #endregion
    }
}
