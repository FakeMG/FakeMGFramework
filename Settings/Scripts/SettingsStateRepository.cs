using System;
using System.Collections.Generic;
using FakeMG.Framework;
using FakeMG.Settings.Converters;

namespace FakeMG.Settings
{
    public sealed class SettingsStateRepository
    {
        private readonly Dictionary<string, PersistedSettingValue> _persistedValues = new(StringComparer.Ordinal);
        private readonly Dictionary<string, ISettingValueRuntimeStorage> _runtimeStorages = new(StringComparer.Ordinal);

        public event Action OnSettingsChanged;

        #region Public Methods

        public void RegisterSetting<T>(SettingDefinitionGenericSO<T> settingSO)
        {
            if (settingSO == null)
            {
                throw new ArgumentNullException(nameof(settingSO));
            }

            if (_runtimeStorages.TryGetValue(settingSO.SettingId, out ISettingValueRuntimeStorage existingStorage))
            {
                if (existingStorage.ValueType != typeof(T))
                {
                    throw new InvalidOperationException($"Setting '{settingSO.SettingId}' is already registered as '{existingStorage.ValueType.FullName}'.");
                }

                return;
            }

            var storage = new SettingValueRuntimeStorage<T>(settingSO);
            if (_persistedValues.TryGetValue(settingSO.SettingId, out PersistedSettingValue persistedValue) && !storage.TryRestore(persistedValue, out string failureReason))
            {
                Echo.Warning($"Resetting setting '{settingSO.SettingId}': {failureReason}");
                storage.RestoreDefault();
            }

            _runtimeStorages.Add(settingSO.SettingId, storage);
            _persistedValues[settingSO.SettingId] = storage.CapturePersistedValue();
        }

        public T GetValue<T>(SettingDefinitionGenericSO<T> settingSO)
        {
            return GetRegisteredStorage(settingSO).Value;
        }

        public void SetValue<T>(SettingDefinitionGenericSO<T> settingSO, T newValue)
        {
            SettingValueRuntimeStorage<T> storage = GetRegisteredStorage(settingSO);
            if (EqualityComparer<T>.Default.Equals(storage.Value, newValue))
            {
                return;
            }

            storage.SetValue(newValue);
            _persistedValues[settingSO.SettingId] = storage.CapturePersistedValue();
            OnSettingsChanged?.Invoke();
        }

        public void Subscribe<T>(
            SettingDefinitionGenericSO<T> settingSO,
            Action<SettingDefinitionGenericSO<T>, T> callback)
        {
            GetRegisteredStorage(settingSO).OnValueChanged += callback;
        }

        public void Unsubscribe<T>(
            SettingDefinitionGenericSO<T> settingSO,
            Action<SettingDefinitionGenericSO<T>, T> callback)
        {
            GetRegisteredStorage(settingSO).OnValueChanged -= callback;
        }

        public SettingDataSnapshot CaptureSnapshot()
        {
            var snapshot = new SettingDataSnapshot();
            foreach (KeyValuePair<string, PersistedSettingValue> persistedEntry in _persistedValues)
            {
                snapshot.Values.Add(persistedEntry.Key, persistedEntry.Value.SerializedValue);
                snapshot.ValueTypes.Add(persistedEntry.Key, persistedEntry.Value.TypeId);
            }

            return snapshot;
        }

        public bool TryValidateSnapshot(SettingDataSnapshot snapshot, out string failureReason)
        {
            if (snapshot?.Values == null || snapshot.ValueTypes == null)
            {
                failureReason = "Settings snapshot or one of its maps is missing.";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }

        public void RestoreSnapshot(SettingDataSnapshot snapshot)
        {
            if (!TryValidateSnapshot(snapshot, out string failureReason))
            {
                throw new ArgumentException(failureReason, nameof(snapshot));
            }

            var nextPersistedValues = new Dictionary<string, PersistedSettingValue>(StringComparer.Ordinal);
            List<string> diagnostics = new();
            foreach (KeyValuePair<string, string> valueEntry in snapshot.Values)
            {
                if (!snapshot.ValueTypes.TryGetValue(valueEntry.Key, out string typeId))
                {
                    diagnostics.Add($"Setting '{valueEntry.Key}' is missing its type ID.");
                    continue;
                }

                if (!SettingValueConverterRegistry.TryResolveType(typeId, out Type valueType) ||
                    !SettingValueConverterRegistry.TryDeserialize(valueType, valueEntry.Value, out _))
                {
                    diagnostics.Add($"Setting '{valueEntry.Key}' has an unsupported type or corrupt value.");
                    continue;
                }

                nextPersistedValues.Add(valueEntry.Key, new PersistedSettingValue(valueEntry.Value, typeId));
            }

            foreach (string typedSettingId in snapshot.ValueTypes.Keys)
            {
                if (!snapshot.Values.ContainsKey(typedSettingId))
                {
                    diagnostics.Add($"Setting type '{typedSettingId}' has no matching value.");
                }
            }

            foreach (string registeredSettingId in _runtimeStorages.Keys)
            {
                if (!snapshot.Values.ContainsKey(registeredSettingId))
                {
                    diagnostics.Add($"Registered setting '{registeredSettingId}' is missing and will use its default.");
                }
            }

            foreach (string diagnostic in diagnostics)
            {
                Echo.Warning(diagnostic);
            }

            _persistedValues.Clear();
            foreach (KeyValuePair<string, PersistedSettingValue> persistedEntry in nextPersistedValues)
            {
                _persistedValues.Add(persistedEntry.Key, persistedEntry.Value);
            }

            foreach (ISettingValueRuntimeStorage storage in _runtimeStorages.Values)
            {
                string storageFailureReason = string.Empty;
                if (_persistedValues.TryGetValue(storage.SettingId, out PersistedSettingValue persistedValue) && storage.TryRestore(persistedValue, out storageFailureReason))
                {
                    continue;
                }

                if (_persistedValues.ContainsKey(storage.SettingId))
                {
                    Echo.Warning($"Resetting setting '{storage.SettingId}': {storageFailureReason}");
                }

                storage.RestoreDefault();
                _persistedValues[storage.SettingId] = storage.CapturePersistedValue();
            }
        }

        public void RestoreDefaults()
        {
            _persistedValues.Clear();
            foreach (ISettingValueRuntimeStorage storage in _runtimeStorages.Values)
            {
                storage.RestoreDefault();
                _persistedValues.Add(storage.SettingId, storage.CapturePersistedValue());
            }
        }

        #endregion

        #region Private Methods

        private SettingValueRuntimeStorage<T> GetRegisteredStorage<T>(SettingDefinitionGenericSO<T> settingSO)
        {
            if (settingSO == null)
            {
                throw new ArgumentNullException(nameof(settingSO));
            }

            if (!_runtimeStorages.TryGetValue(settingSO.SettingId, out ISettingValueRuntimeStorage storage))
            {
                throw new InvalidOperationException($"Setting '{settingSO.SettingId}' must be registered before use.");
            }

            return storage as SettingValueRuntimeStorage<T> ??
                   throw new InvalidOperationException($"Setting '{settingSO.SettingId}' is registered with a different value type.");
        }

        #endregion
    }
}
