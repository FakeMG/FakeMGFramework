using FakeMG.Framework.ExtensionMethods;
using TMPro;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace FakeMG.Settings
{
    /// <summary>Shows a stored value on a slider whose range may differ from the storage range, e.g. 0-100 for 0-1.</summary>
    public sealed class SliderSettingUIBinder : SettingUIBinder
    {
        private const string DEFAULT_VALUE_FORMAT = "0.##";

        [Required, SerializeField] private SliderSettingSO _sliderSettingSO;
        [Required, SerializeField] private Slider _slider;
        [Required, SerializeField] private TMP_Text _valueText;
        [SerializeField] private float _uiMinValue;
        [SerializeField] private float _uiMaxValue = 1f;
        [SerializeField] private bool _useWholeNumbers;
        [SerializeField] private string _valueFormat = DEFAULT_VALUE_FORMAT;

        public override SettingDefinitionSO SettingSO => _sliderSettingSO;

        #region Unity Lifecycle

        private void OnValidate()
        {
            ApplySliderRange();
        }

        private void Awake()
        {
            ApplySliderRange();
        }

        private void OnEnable()
        {
            _slider.onValueChanged.AddListener(StoreSliderValue);
        }

        private void OnDisable()
        {
            _slider.onValueChanged.RemoveListener(StoreSliderValue);
        }

        #endregion

        #region Private Methods

        private void ApplySliderRange()
        {
            _slider.minValue = _uiMinValue;
            _slider.maxValue = _uiMaxValue;
            _slider.wholeNumbers = _useWholeNumbers;
        }

        /// <remarks>Stores the value back when the slider had to clamp or round it, so the save matches what is shown.</remarks>
        protected override void ShowStoredValue()
        {
            float storedValue = Settings.GetValue(_sliderSettingSO);
            float sliderValue = ToSliderValue(storedValue);

            _slider.SetValueWithoutNotify(sliderValue);
            UpdateValueLabel(sliderValue);
            StoreStorageValueIfChanged(storedValue, ToStorageValue(sliderValue));
        }

        private void StoreSliderValue(float sliderValue)
        {
            UpdateValueLabel(sliderValue);
            Settings.SetValue(_sliderSettingSO, ToStorageValue(sliderValue));
        }

        private void StoreStorageValueIfChanged(float storedValue, float storageValue)
        {
            if (Mathf.Approximately(storedValue, storageValue))
            {
                return;
            }

            Settings.SetValue(_sliderSettingSO, storageValue);
        }

        private void UpdateValueLabel(float sliderValue)
        {
            _valueText.text = sliderValue.ToString(_valueFormat);
        }

        private float ToSliderValue(float storedValue)
        {
            if (HasCollapsedRange(_sliderSettingSO.StorageMinValue, _sliderSettingSO.StorageMaxValue) || HasCollapsedRange(_uiMinValue, _uiMaxValue))
            {
                return ClampToSliderRange(_uiMinValue);
            }

            float sliderValue = ClampToStorageRange(storedValue).Remap(
                _sliderSettingSO.StorageMinValue,
                _sliderSettingSO.StorageMaxValue,
                _uiMinValue,
                _uiMaxValue);
            return ClampToSliderRange(sliderValue);
        }

        private float ToStorageValue(float sliderValue)
        {
            if (HasCollapsedRange(_uiMinValue, _uiMaxValue) || HasCollapsedRange(_sliderSettingSO.StorageMinValue, _sliderSettingSO.StorageMaxValue))
            {
                return ClampToStorageRange(_sliderSettingSO.StorageMinValue);
            }

            float storageValue = ClampToSliderRange(sliderValue).Remap(
                _uiMinValue,
                _uiMaxValue,
                _sliderSettingSO.StorageMinValue,
                _sliderSettingSO.StorageMaxValue);
            return ClampToStorageRange(storageValue);
        }

        /// <remarks>Also rounds to a whole number when the slider uses whole numbers.</remarks>
        private float ClampToSliderRange(float sliderValue)
        {
            float clampedValue = Mathf.Clamp(sliderValue, _uiMinValue, _uiMaxValue);
            return _useWholeNumbers ? Mathf.Round(clampedValue) : clampedValue;
        }

        private float ClampToStorageRange(float storageValue)
        {
            return Mathf.Clamp(storageValue, _sliderSettingSO.StorageMinValue, _sliderSettingSO.StorageMaxValue);
        }

        private static bool HasCollapsedRange(float minValue, float maxValue)
        {
            return Mathf.Approximately(minValue, maxValue);
        }

        #endregion
    }
}
