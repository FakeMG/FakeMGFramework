using System;
using System.Collections.Generic;
using FakeMG.Framework;
using FakeMG.SaveLoad;
using FakeMG.Settings.Converters;

namespace FakeMG.Settings
{
    public class SettingDataManager : Saveable
    {
        private Dictionary<string, ISettingValueRuntimeStorage> _settingRuntimeStorages = new();
        private Dictionary<string, string> _serializedValues = new();
        private Dictionary<string, string> _serializedValueTypes = new();

        public override object CaptureState()
        {
            return new SettingDataSnapshot
            {
                Values = new Dictionary<string, string>(_serializedValues),
                ValueTypes = new Dictionary<string, string>(_serializedValueTypes)
            };
        }

        public override void RestoreDefaultState()
        {
            _settingRuntimeStorages = new Dictionary<string, ISettingValueRuntimeStorage>();
            _serializedValues = new Dictionary<string, string>();
            _serializedValueTypes = new Dictionary<string, string>();
        }

        public override void RestoreState(object data)
        {
            if (data is SettingDataSnapshot snapshot)
            {
                _settingRuntimeStorages = new Dictionary<string, ISettingValueRuntimeStorage>();
                _serializedValues = snapshot.Values != null
                    ? new Dictionary<string, string>(snapshot.Values)
                    : new Dictionary<string, string>();
                _serializedValueTypes = snapshot.ValueTypes != null
                    ? new Dictionary<string, string>(snapshot.ValueTypes)
                    : new Dictionary<string, string>();
                return;
            }

            RestoreDefaultState();
        }

        public void SetValue<T>(SettingDefinitionGenericSO<T> setting, T newValue)
        {
            var storage = GetOrCreateStorage(setting);
            if (!EqualityComparer<T>.Default.Equals(storage.value, newValue))
            {
                storage.value = newValue;
                PersistValue(setting.SettingId, storage);
                storage.OnChanged?.Invoke(setting, newValue);
            }
        }

        public T GetValue<T>(SettingDefinitionGenericSO<T> setting)
        {
            return GetOrCreateStorage(setting).value;
        }

        public void Subscribe<T>(SettingDefinitionGenericSO<T> setting, Action<SettingDefinitionGenericSO<T>, T> callback)
        {
            GetOrCreateStorage(setting).OnChanged += callback;
        }

        public void Unsubscribe<T>(SettingDefinitionGenericSO<T> setting, Action<SettingDefinitionGenericSO<T>, T> callback)
        {
            if (_settingRuntimeStorages.TryGetValue(setting.SettingId, out ISettingValueRuntimeStorage storage))
            {
                if (storage is SettingValueRuntimeStorage<T> typedStorage)
                {
                    typedStorage.OnChanged -= callback;
                    return;
                }

                Echo.Warning($"Ignored unsubscribe for setting '{setting.SettingId}' because the stored value type did not match {typeof(T).Name}.", context: this);
            }
        }

        private SettingValueRuntimeStorage<T> GetOrCreateStorage<T>(SettingDefinitionGenericSO<T> setting)
        {
            if (_settingRuntimeStorages.TryGetValue(setting.SettingId, out ISettingValueRuntimeStorage storage))
            {
                if (storage is SettingValueRuntimeStorage<T> typedStorage)
                {
                    return typedStorage;
                }

                Echo.Warning($"Resetting setting '{setting.SettingId}' because the stored value type did not match {typeof(T).Name}.", context: this);
                _settingRuntimeStorages.Remove(setting.SettingId);
                _serializedValues.Remove(setting.SettingId);
                _serializedValueTypes.Remove(setting.SettingId);
            }

            if (TryCreateStorageFromSerializedValue(setting, out SettingValueRuntimeStorage<T> restoredStorage))
            {
                return restoredStorage;
            }

            T defaultValue = setting.GetDefaultValue();
            SettingValueRuntimeStorage<T> createdStorage = new() { value = defaultValue };
            _settingRuntimeStorages.Add(setting.SettingId, createdStorage);
            PersistValue(setting.SettingId, createdStorage);
            return createdStorage;
        }

        /// <summary>
        /// Attempts to restore a setting value from serialized data.
        /// Removes corrupted data on deserialization failure to prevent repeated retry attempts.
        /// </summary>
        private bool TryCreateStorageFromSerializedValue<T>(SettingDefinitionGenericSO<T> setting, out SettingValueRuntimeStorage<T> storage)
        {
            if (_serializedValues.TryGetValue(setting.SettingId, out string serializedValue) &&
                SettingValueConverterRegistry.TryDeserialize(serializedValue, out T restoredValue))
            {
                storage = new SettingValueRuntimeStorage<T> { value = restoredValue };
                _settingRuntimeStorages[setting.SettingId] = storage;
                PersistValue(setting.SettingId, storage);
                return true;
            }

            if (_serializedValues.ContainsKey(setting.SettingId))
            {
                Echo.Warning($"Resetting setting '{setting.SettingId}' because the saved value could not be deserialized as {typeof(T).Name}.", context: this);
                _serializedValues.Remove(setting.SettingId);
                _serializedValueTypes.Remove(setting.SettingId);
            }

            storage = null;
            return false;
        }

        private void PersistValue(string settingId, ISettingValueRuntimeStorage storage)
        {
            if (SettingValueConverterRegistry.TrySerialize(storage.ValueType, storage.GetValue(), out string serializedValue))
            {
                if (SettingValueConverterRegistry.TryGetTypeId(storage.ValueType, out string typeId))
                {
                    _serializedValues[settingId] = serializedValue;
                    _serializedValueTypes[settingId] = typeId;
                    return;
                }
            }

            _serializedValues.Remove(settingId);
            _serializedValueTypes.Remove(settingId);
            Echo.Warning($"Skipped unsupported setting '{settingId}' while creating a save snapshot.", context: this);
        }
    }
}