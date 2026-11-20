using System.Collections.Generic;
using TMPro;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FakeMG.Settings
{
    /// <summary>Shows an option setting's labels in a dropdown and stores the chosen option's id.</summary>
    public sealed class DropdownSettingUIBinder : SettingUIBinder
    {
        [Required, SerializeField] private OptionSettingSO _optionSettingSO;
        [Required, SerializeField] private TMP_Dropdown _dropdown;

        private IReadOnlyList<SettingOption> _options;

        public override SettingDefinitionSO SettingSO => _optionSettingSO;

        #region Unity Lifecycle

        private void OnEnable()
        {
            _dropdown.onValueChanged.AddListener(StoreSelectedOption);
        }

        private void OnDisable()
        {
            _dropdown.onValueChanged.RemoveListener(StoreSelectedOption);
        }

        #endregion

        #region Private Methods

        /// <remarks>An unknown stored id shows the first option, and that option is stored so the save matches what is shown.</remarks>
        protected override void ShowStoredValue()
        {
            ShowOptions();
            string storedOptionId = Settings.GetValue(_optionSettingSO);
            int shownOptionIndex = _optionSettingSO.IndexOfOptionOrDefault(storedOptionId);
            if (shownOptionIndex == OptionSettingSO.NO_OPTION_INDEX)
            {
                return;
            }

            _dropdown.SetValueWithoutNotify(shownOptionIndex);
            StoreSelectedOption(shownOptionIndex);
        }

        private void ShowOptions()
        {
            _options = _optionSettingSO.GetOptions();
            List<string> optionLabels = new(_options.Count);
            foreach (SettingOption option in _options)
            {
                optionLabels.Add(option.Label);
            }

            _dropdown.ClearOptions();
            _dropdown.AddOptions(optionLabels);
        }

        private void StoreSelectedOption(int selectedOptionIndex)
        {
            Settings.SetValue(_optionSettingSO, _options[selectedOptionIndex].Id);
        }

        #endregion
    }
}
