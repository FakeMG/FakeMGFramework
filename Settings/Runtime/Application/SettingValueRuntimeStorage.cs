using System;
using System.Collections.Generic;

namespace FakeMG.Settings
{
    public sealed class SettingValueRuntimeStorage<T> : ISettingValueRuntimeStorage
    {
        private readonly SettingDefinitionGenericSO<T> _settingSO;

        public SettingDefinitionSO SettingSO => _settingSO;
        public T Value { get; private set; }

        /// <exception cref="InvalidOperationException">The value type cannot be saved as text.</exception>
        public SettingValueRuntimeStorage(SettingDefinitionGenericSO<T> settingSO)
        {
            if (!SettingValueText.CanConvert(typeof(T)))
            {
                throw new InvalidOperationException($"Setting '{settingSO.SettingId}' has a value type that cannot be saved: '{typeof(T).FullName}'.");
            }

            _settingSO = settingSO;
            Value = settingSO.GetDefaultValue();
        }

        #region Public Methods

        /// <returns>Whether the value changed.</returns>
        public bool ChangeValue(T value)
        {
            if (EqualityComparer<T>.Default.Equals(Value, value))
            {
                return false;
            }

            Value = value;
            return true;
        }

        public string FormatValue()
        {
            return SettingValueText.Format(Value);
        }

        public bool TryRestoreFromText(string savedText, out bool hasChanged)
        {
            if (!SettingValueText.TryParse(savedText, out T restoredValue))
            {
                hasChanged = false;
                return false;
            }

            hasChanged = ChangeValue(restoredValue);
            return true;
        }

        public bool ResetToDefault()
        {
            return ChangeValue(_settingSO.GetDefaultValue());
        }

        #endregion
    }
}
