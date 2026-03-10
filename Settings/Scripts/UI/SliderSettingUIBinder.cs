using FakeMG.Framework.ExtensionMethods;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace FakeMG.Settings
{
    public class SliderSettingUIBinder : MonoBehaviour
    {
        private const string DEFAULT_VALUE_FORMAT = "0.##";

        [SerializeField] private SliderSettingSO _sliderSetting;
        [SerializeField] private Slider _slider;
        [SerializeField] private TMP_Text _labelText;
        [SerializeField] private TMP_Text _valueText;
        [SerializeField] private float _uiMinValue;
        [SerializeField] private float _uiMaxValue = 1f;
        [SerializeField] private bool _useWholeNumbers;
        [SerializeField] private string _valueFormat = DEFAULT_VALUE_FORMAT;

        [Inject] private readonly SettingDataManager _settingDataManager;

        private void OnValidate()
        {
            if (_slider)
            {
                ApplySliderPresentation();
            }
        }

        private void OnEnable()
        {
            _slider.onValueChanged.AddListener(StoreSliderValue);
        }

        private void Start()
        {
            ApplyLabel();
            ApplySliderPresentation();
            ApplyStoredSettingValueToSlider();
        }

        private void OnDisable()
        {
            _slider.onValueChanged.RemoveListener(StoreSliderValue);
        }

        private void ApplyLabel()
        {
            _labelText.text = _sliderSetting.Label;
        }

        private void ApplySliderPresentation()
        {
            _slider.minValue = _uiMinValue;
            _slider.maxValue = _uiMaxValue;
            _slider.wholeNumbers = _useWholeNumbers;
        }

        private void ApplyStoredSettingValueToSlider()
        {
            float storedSliderValue = ClampStoredValue(_settingDataManager.GetValue(_sliderSetting));
            float sliderValue = ConvertStoredValueToSliderValue(storedSliderValue);

            _slider.SetValueWithoutNotify(sliderValue);
            UpdateValueLabel(sliderValue);
            StoreNormalizedSliderValueIfNeeded(storedSliderValue);
        }

        private void StoreSliderValue(float sliderValue)
        {
            float normalizedSliderValue = ConvertSliderValueToStoredValue(sliderValue);

            UpdateValueLabel(sliderValue);
            _settingDataManager.SetValue(_sliderSetting, normalizedSliderValue);
        }

        private void StoreNormalizedSliderValueIfNeeded(float storedSliderValue)
        {
            float normalizedSliderValue = ConvertSliderValueToStoredValue(_slider.value);

            if (Mathf.Approximately(storedSliderValue, normalizedSliderValue))
            {
                return;
            }

            _settingDataManager.SetValue(_sliderSetting, normalizedSliderValue);
        }

        private void UpdateValueLabel(float sliderValue)
        {
            _valueText.text = sliderValue.ToString(_valueFormat);
        }

        private float ConvertStoredValueToSliderValue(float storedSliderValue)
        {
            float clampedStoredValue = ClampStoredValue(storedSliderValue);

            if (HasCollapsedRange(_sliderSetting.StorageMinValue, _sliderSetting.StorageMaxValue) ||
                HasCollapsedRange(_uiMinValue, _uiMaxValue))
            {
                return NormalizeSliderValue(_uiMinValue);
            }

            float sliderValue = clampedStoredValue.Remap(
                _sliderSetting.StorageMinValue,
                _sliderSetting.StorageMaxValue,
                _uiMinValue,
                _uiMaxValue);

            return NormalizeSliderValue(sliderValue);
        }

        private float ConvertSliderValueToStoredValue(float sliderValue)
        {
            float normalizedSliderValue = NormalizeSliderValue(sliderValue);

            if (HasCollapsedRange(_uiMinValue, _uiMaxValue) ||
                HasCollapsedRange(_sliderSetting.StorageMinValue, _sliderSetting.StorageMaxValue))
            {
                return ClampStoredValue(_sliderSetting.StorageMinValue);
            }

            float storedValue = normalizedSliderValue.Remap(
                _uiMinValue,
                _uiMaxValue,
                _sliderSetting.StorageMinValue,
                _sliderSetting.StorageMaxValue);

            return ClampStoredValue(storedValue);
        }

        private float NormalizeSliderValue(float sliderValue)
        {
            float clampedValue = Mathf.Clamp(sliderValue, _uiMinValue, _uiMaxValue);

            if (!_useWholeNumbers)
            {
                return clampedValue;
            }

            return Mathf.Round(clampedValue);
        }

        private float ClampStoredValue(float storedValue)
        {
            return Mathf.Clamp(storedValue, _sliderSetting.StorageMinValue, _sliderSetting.StorageMaxValue);
        }

        private static bool HasCollapsedRange(float minValue, float maxValue)
        {
            return Mathf.Approximately(minValue, maxValue);
        }
    }
}