using TMPro;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace FakeMG.Settings
{
    public class ToggleSettingUIBinder : MonoBehaviour
    {
        [Required, SerializeField] private SliderSettingSO _sliderSettingSO;
        [Required, SerializeField] private Toggle _toggle;
        [Required, SerializeField] private TMP_Text _labelText;
        [SerializeField] private bool _revert;

        [Inject] private readonly SettingsStateRepository _settingsStateRepository;

        #region Unity Lifecycle

        private void OnEnable()
        {
            _toggle.onValueChanged.AddListener(StoreToggleState);
        }

        private void Start()
        {
            _settingsStateRepository.RegisterSetting(_sliderSettingSO);
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
            _labelText.text = _sliderSettingSO.Label;
        }

        private void ApplyStoredValue()
        {
            float storedValue = ClampStoredValue(_settingsStateRepository.GetValue(_sliderSettingSO));
            bool isOn = ConvertStoredValueToToggleState(storedValue);

            _toggle.SetIsOnWithoutNotify(isOn);
            StoreNormalizedValueIfNeeded(storedValue, isOn);
        }

        private void StoreToggleState(bool isOn)
        {
            float storedValue = ConvertToggleStateToStoredValue(isOn);
            _settingsStateRepository.SetValue(_sliderSettingSO, storedValue);
        }

        private void StoreNormalizedValueIfNeeded(float storedValue, bool isOn)
        {
            float normalizedValue = ConvertToggleStateToStoredValue(isOn);
            if (Mathf.Approximately(storedValue, normalizedValue))
            {
                return;
            }

            _settingsStateRepository.SetValue(_sliderSettingSO, normalizedValue);
        }

        private float ClampStoredValue(float storedValue)
        {
            return Mathf.Clamp(storedValue, _sliderSettingSO.StorageMinValue, _sliderSettingSO.StorageMaxValue);
        }

        private bool ConvertStoredValueToToggleState(float storedValue)
        {
            if (Mathf.Approximately(_sliderSettingSO.StorageMinValue, _sliderSettingSO.StorageMaxValue))
            {
                return false;
            }

            float thresholdValue = (_sliderSettingSO.StorageMinValue + _sliderSettingSO.StorageMaxValue) * 0.5f;
            bool isOn = storedValue >= thresholdValue;
            return _revert ? !isOn : isOn;
        }

        private float ConvertToggleStateToStoredValue(bool isOn)
        {
            bool isEnabled = _revert ? !isOn : isOn;
            return isEnabled ? _sliderSettingSO.StorageMaxValue : _sliderSettingSO.StorageMinValue;
        }

        #endregion
    }
}
