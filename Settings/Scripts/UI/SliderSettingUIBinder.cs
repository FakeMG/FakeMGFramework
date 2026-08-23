using FakeMG.Framework.ExtensionMethods;
using TMPro;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace FakeMG.Settings
{
    public class SliderSettingUIBinder : MonoBehaviour
    {
        private const string DEFAULT_VALUE_FORMAT = "0.##";

        [Required, SerializeField] private SliderSettingSO _sliderSettingSO;
        [Required, SerializeField] private Slider _slider;
        [Required, SerializeField] private TMP_Text _labelText;
        [Required, SerializeField] private TMP_Text _valueText;
        [SerializeField] private float _uiMinValue;
        [SerializeField] private float _uiMaxValue = 1f;
        [SerializeField] private bool _useWholeNumbers;
        [SerializeField] private string _valueFormat = DEFAULT_VALUE_FORMAT;

        [Inject] private readonly SettingsStateRepository _settingsStateRepository;

        #region Unity Lifecycle

        private void OnValidate()
        {
            ApplySliderPresentation();
        }

        private void OnEnable()
        {
            _slider.onValueChanged.AddListener(StoreSliderValue);
        }

        private void Start()
        {
            _settingsStateRepository.RegisterSetting(_sliderSettingSO);
            ApplyLabel();
            ApplySliderPresentation();
            ApplyStoredSettingValueToSlider();
        }

        private void OnDisable()
        {
            _slider.onValueChanged.RemoveListener(StoreSliderValue);
        }

        #endregion

        #region Private Methods

        private void ApplyLabel()
        {
            _labelText.text = _sliderSettingSO.Label;
        }

        private void ApplySliderPresentation()
        {
            _slider.minValue = _uiMinValue;
            _slider.maxValue = _uiMaxValue;
            _slider.wholeNumbers = _useWholeNumbers;
        }

        private void ApplyStoredSettingValueToSlider()
        {
            float storedSliderValue = ClampStoredValue(_settingsStateRepository.GetValue(_sliderSettingSO));
            float sliderValue = ConvertStoredValueToSliderValue(storedSliderValue);

            _slider.SetValueWithoutNotify(sliderValue);
            UpdateValueLabel(sliderValue);
            StoreNormalizedSliderValueIfNeeded(storedSliderValue);
        }

        private void StoreSliderValue(float sliderValue)
        {
            float normalizedSliderValue = ConvertSliderValueToStoredValue(sliderValue);

            UpdateValueLabel(sliderValue);
            _settingsStateRepository.SetValue(_sliderSettingSO, normalizedSliderValue);
        }

        private void StoreNormalizedSliderValueIfNeeded(float storedSliderValue)
        {
            float normalizedSliderValue = ConvertSliderValueToStoredValue(_slider.value);

            if (Mathf.Approximately(storedSliderValue, normalizedSliderValue))
            {
                return;
            }

            _settingsStateRepository.SetValue(_sliderSettingSO, normalizedSliderValue);
        }

        private void UpdateValueLabel(float sliderValue)
        {
            _valueText.text = sliderValue.ToString(_valueFormat);
        }

        private float ConvertStoredValueToSliderValue(float storedSliderValue)
        {
            float clampedStoredValue = ClampStoredValue(storedSliderValue);

            if (HasCollapsedRange(_sliderSettingSO.StorageMinValue, _sliderSettingSO.StorageMaxValue) ||
                HasCollapsedRange(_uiMinValue, _uiMaxValue))
            {
                return NormalizeSliderValue(_uiMinValue);
            }

            float sliderValue = clampedStoredValue.Remap(
                _sliderSettingSO.StorageMinValue,
                _sliderSettingSO.StorageMaxValue,
                _uiMinValue,
                _uiMaxValue);

            return NormalizeSliderValue(sliderValue);
        }

        private float ConvertSliderValueToStoredValue(float sliderValue)
        {
            float normalizedSliderValue = NormalizeSliderValue(sliderValue);

            if (HasCollapsedRange(_uiMinValue, _uiMaxValue) ||
                HasCollapsedRange(_sliderSettingSO.StorageMinValue, _sliderSettingSO.StorageMaxValue))
            {
                return ClampStoredValue(_sliderSettingSO.StorageMinValue);
            }

            float storedValue = normalizedSliderValue.Remap(
                _uiMinValue,
                _uiMaxValue,
                _sliderSettingSO.StorageMinValue,
                _sliderSettingSO.StorageMaxValue);

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
            return Mathf.Clamp(storedValue, _sliderSettingSO.StorageMinValue, _sliderSettingSO.StorageMaxValue);
        }

        private static bool HasCollapsedRange(float minValue, float maxValue)
        {
            return Mathf.Approximately(minValue, maxValue);
        }

        #endregion
    }
}
