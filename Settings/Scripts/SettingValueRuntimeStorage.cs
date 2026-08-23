using System;
using System.Collections.Generic;
using FakeMG.Settings.Converters;

namespace FakeMG.Settings
{
    public sealed class SettingValueRuntimeStorage<T> : ISettingValueRuntimeStorage
    {
        private readonly SettingDefinitionGenericSO<T> _settingSO;
        private T _value;

        public string SettingId => _settingSO.SettingId;
        public Type ValueType => typeof(T);
        public T Value => _value;

        public event Action<SettingDefinitionGenericSO<T>, T> OnValueChanged;

        public SettingValueRuntimeStorage(SettingDefinitionGenericSO<T> settingSO)
        {
            _settingSO = settingSO;
            _value = settingSO.GetDefaultValue();
        }

        #region Public Methods

        public void SetValue(T value)
        {
            if (EqualityComparer<T>.Default.Equals(_value, value))
            {
                return;
            }

            _value = value;
            OnValueChanged?.Invoke(_settingSO, _value);
        }

        public object GetValue()
        {
            return _value;
        }

        public PersistedSettingValue CapturePersistedValue()
        {
            if (!SettingValueConverterRegistry.TrySerialize(typeof(T), _value, out string serializedValue) ||
                !SettingValueConverterRegistry.TryGetTypeId(typeof(T), out string typeId))
            {
                throw new InvalidOperationException($"No settings converter is registered for '{typeof(T).FullName}'.");
            }

            return new PersistedSettingValue(serializedValue, typeId);
        }

        public bool TryRestore(PersistedSettingValue persistedValue, out string failureReason)
        {
            if (!SettingValueConverterRegistry.TryGetTypeId(typeof(T), out string expectedTypeId) ||
                !string.Equals(expectedTypeId, persistedValue.TypeId, StringComparison.Ordinal))
            {
                failureReason = $"Saved type '{persistedValue.TypeId}' does not match '{typeof(T).FullName}'.";
                return false;
            }

            if (!SettingValueConverterRegistry.TryDeserialize(persistedValue.SerializedValue, out T restoredValue))
            {
                failureReason = $"Saved value could not be converted to '{typeof(T).FullName}'.";
                return false;
            }

            SetValue(restoredValue);
            failureReason = string.Empty;
            return true;
        }

        public void RestoreDefault()
        {
            SetValue(_settingSO.GetDefaultValue());
        }

        #endregion
    }
}
