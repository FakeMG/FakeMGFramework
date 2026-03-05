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
        [SerializeField] private string _valueFormat = DEFAULT_VALUE_FORMAT;
        [Inject] private SettingDataManager _settingDataManager;

        private void Start()
        {
            ApplyLabel();
            ApplySliderBoundsFromSetting();
            ApplyStoredSettingValueToSlider();
        }

        private void ApplyLabel()
        {
            _labelText.text = _sliderSetting.Label;
        }

        private void OnEnable()
        {
            _slider.onValueChanged.AddListener(StoreSliderValue);
        }

        private void OnDisable()
        {
            _slider.onValueChanged.RemoveListener(StoreSliderValue);
        }

        private void ApplySliderBoundsFromSetting()
        {
            _slider.minValue = _sliderSetting.MinValue;
            _slider.maxValue = _sliderSetting.MaxValue;
            _slider.wholeNumbers = _sliderSetting.UseWholeNumbers;
        }

        private void ApplyStoredSettingValueToSlider()
        {
            float storedSliderValue = _settingDataManager.GetValue(_sliderSetting);
            float normalizedSliderValue = NormalizeSliderValue(storedSliderValue);

            _slider.SetValueWithoutNotify(normalizedSliderValue);
            UpdateValueLabel(normalizedSliderValue);
            StoreNormalizedSliderValueIfNeeded(storedSliderValue, normalizedSliderValue);
        }

        private void StoreSliderValue(float sliderValue)
        {
            float normalizedSliderValue = NormalizeSliderValue(sliderValue);

            UpdateValueLabel(normalizedSliderValue);
            _settingDataManager.SetValue(_sliderSetting, normalizedSliderValue);
        }

        private void StoreNormalizedSliderValueIfNeeded(float storedSliderValue, float normalizedSliderValue)
        {
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

        private float NormalizeSliderValue(float sliderValue)
        {
            float minValue = _sliderSetting.MinValue;
            float maxValue = _sliderSetting.MaxValue;
            float clampedValue = Mathf.Clamp(sliderValue, minValue, maxValue);

            if (!_sliderSetting.UseWholeNumbers)
            {
                return clampedValue;
            }

            return Mathf.Round(clampedValue);
        }
    }
}