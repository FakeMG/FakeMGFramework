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
        //TODO: Can't inject in multiple instances of this class
        [Inject] private SettingDataManager _settingDataManager;

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
            int storedOptionIndex = _settingDataManager.GetValue(_optionSetting);
            int clampedOptionIndex = ClampOptionIndex(storedOptionIndex);

            _dropdown.SetValueWithoutNotify(clampedOptionIndex);
            StoreClampedOptionIndexIfNeeded(storedOptionIndex, clampedOptionIndex);
        }

        private void StoreSelectedOptionIndex(int selectedOptionIndex)
        {
            int clampedOptionIndex = ClampOptionIndex(selectedOptionIndex);
            _settingDataManager.SetValue(_optionSetting, clampedOptionIndex);
        }

        private void StoreClampedOptionIndexIfNeeded(int storedOptionIndex, int clampedOptionIndex)
        {
            if (storedOptionIndex == clampedOptionIndex)
            {
                return;
            }

            _settingDataManager.SetValue(_optionSetting, clampedOptionIndex);
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