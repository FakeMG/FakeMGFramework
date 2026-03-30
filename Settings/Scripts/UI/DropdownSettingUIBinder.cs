using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using VContainer;

namespace FakeMG.Settings
{
    public class DropdownSettingUIBinder : MonoBehaviour
    {
        [SerializeField] private OptionSettingSO _optionSetting;
        [SerializeField] private TMP_Dropdown _dropdown;
        [SerializeField] private TMP_Text _labelText;

        [Inject]
        private readonly SettingDataManager _settingDataManager;

        private void Start()
        {
            ApplyLabel();
            ApplySettingOptionsToDropdown();
            ApplyStoredSettingValueToDropdown();
        }

        private void ApplyLabel()
        {
            _labelText.text = _optionSetting.Label;
        }

        private void OnEnable()
        {
            _dropdown.onValueChanged.AddListener(StoreSelectedOptionIndex);
        }

        private void OnDisable()
        {
            _dropdown.onValueChanged.RemoveListener(StoreSelectedOptionIndex);
        }

        private void ApplySettingOptionsToDropdown()
        {
            List<string> optionLabels = _optionSetting.GetOptions();
            List<string> safeOptionLabels = GetSafeOptionLabels(optionLabels);

            _dropdown.ClearOptions();
            _dropdown.AddOptions(safeOptionLabels);
        }

        private void ApplyStoredSettingValueToDropdown()
        {
            string storedOptionValue = _settingDataManager.GetValue(_optionSetting);
            int matchedOptionIndex = GetOptionIndex(storedOptionValue);

            int clampedOptionIndex = ClampOptionIndex(matchedOptionIndex);
            string resolvedOptionValue = GetOptionValue(clampedOptionIndex);

            _dropdown.SetValueWithoutNotify(clampedOptionIndex);
            StoreResolvedOptionValueIfNeeded(storedOptionValue, resolvedOptionValue);
        }

        private void StoreSelectedOptionIndex(int selectedOptionIndex)
        {
            int clampedOptionIndex = ClampOptionIndex(selectedOptionIndex);
            string selectedOptionValue = GetOptionValue(clampedOptionIndex);
            _settingDataManager.SetValue(_optionSetting, selectedOptionValue);
        }

        private void StoreResolvedOptionValueIfNeeded(string storedOptionValue, string resolvedOptionValue)
        {
            if (string.Equals(storedOptionValue, resolvedOptionValue, StringComparison.Ordinal))
            {
                return;
            }

            _settingDataManager.SetValue(_optionSetting, resolvedOptionValue);
        }

        private int ClampOptionIndex(int optionIndex)
        {
            int maxOptionIndex = _dropdown.options.Count - 1;
            if (maxOptionIndex < 0)
            {
                return 0;
            }

            return Mathf.Clamp(optionIndex, 0, maxOptionIndex);
        }

        private int GetOptionIndex(string optionValue)
        {
            if (string.IsNullOrEmpty(optionValue))
            {
                return -1;
            }

            for (int index = 0; index < _dropdown.options.Count; index++)
            {
                if (string.Equals(_dropdown.options[index].text, optionValue, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }

        private string GetOptionValue(int optionIndex)
        {
            if (optionIndex < 0 || optionIndex >= _dropdown.options.Count)
            {
                return string.Empty;
            }

            return _dropdown.options[optionIndex].text ?? string.Empty;
        }

        private static List<string> GetSafeOptionLabels(List<string> optionLabels)
        {
            if (optionLabels == null)
            {
                return new List<string>();
            }

            return optionLabels;
        }
    }
}