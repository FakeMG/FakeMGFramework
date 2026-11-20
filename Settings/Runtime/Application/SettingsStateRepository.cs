using System;
using System.Collections.Generic;
using FakeMG.Framework;

namespace FakeMG.Settings
{
    /// <summary>
    /// Holds every setting's current value. A setting needs no registration: its storage is created the first time it is read
    /// or written, from the saved value if one was loaded, or from its default.
    /// </summary>
    public sealed class SettingsStateRepository
    {
        private readonly Dictionary<string, ISettingValueRuntimeStorage> _storages = new(StringComparer.Ordinal);

        /// <summary>Saved values whose setting has not been used yet. They are kept so saving does not drop them.</summary>
        private readonly Dictionary<string, string> _unusedSavedValues = new(StringComparer.Ordinal);

        /// <summary>Raised for every value change, restores included.</summary>
        public event Action<SettingDefinitionSO> OnSettingChanged;

        /// <summary>Raised only by <see cref="SetValue{T}"/>, so restoring saved values never triggers a save.</summary>
        public event Action OnSettingsEdited;

        #region Public Methods

        public T GetValue<T>(SettingDefinitionGenericSO<T> settingSO)
        {
            return GetOrCreateStorage(settingSO).Value;
        }

        public void SetValue<T>(SettingDefinitionGenericSO<T> settingSO, T newValue)
        {
            if (!GetOrCreateStorage(settingSO).ChangeValue(newValue))
            {
                return;
            }

            OnSettingChanged?.Invoke(settingSO);
            OnSettingsEdited?.Invoke();
        }

        public SettingDataSnapshot CaptureSnapshot()
        {
            var snapshot = new SettingDataSnapshot { Values = new Dictionary<string, string>(_unusedSavedValues, StringComparer.Ordinal) };
            foreach (ISettingValueRuntimeStorage storage in _storages.Values)
            {
                snapshot.Values[storage.SettingSO.SettingId] = storage.FormatValue();
            }

            return snapshot;
        }

        /// <remarks>A setting missing from the snapshot, or saved with an invalid value, goes back to its default.</remarks>
        public void RestoreSnapshot(SettingDataSnapshot snapshot)
        {
            _unusedSavedValues.Clear();
            foreach (KeyValuePair<string, string> savedValue in snapshot.Values)
            {
                if (!_storages.ContainsKey(savedValue.Key))
                {
                    _unusedSavedValues.Add(savedValue.Key, savedValue.Value);
                }
            }

            foreach (ISettingValueRuntimeStorage storage in _storages.Values)
            {
                bool hasChanged = snapshot.Values.TryGetValue(storage.SettingSO.SettingId, out string savedText)
                    ? RestoreOrResetToDefault(storage, savedText)
                    : storage.ResetToDefault();
                RaiseSettingChangedIf(hasChanged, storage.SettingSO);
            }
        }

        public void RestoreDefaults()
        {
            _unusedSavedValues.Clear();
            foreach (ISettingValueRuntimeStorage storage in _storages.Values)
            {
                RaiseSettingChangedIf(storage.ResetToDefault(), storage.SettingSO);
            }
        }

        #endregion

        #region Private Methods

        /// <exception cref="InvalidOperationException">Two settings share an id but not a value type.</exception>
        private SettingValueRuntimeStorage<T> GetOrCreateStorage<T>(SettingDefinitionGenericSO<T> settingSO)
        {
            if (_storages.TryGetValue(settingSO.SettingId, out ISettingValueRuntimeStorage existingStorage))
            {
                return existingStorage as SettingValueRuntimeStorage<T> ??
                       throw new InvalidOperationException($"Setting id '{settingSO.SettingId}' is used by settings of different value types.");
            }

            var storage = new SettingValueRuntimeStorage<T>(settingSO);
            if (_unusedSavedValues.Remove(settingSO.SettingId, out string savedText))
            {
                RestoreOrResetToDefault(storage, savedText);
            }

            _storages.Add(settingSO.SettingId, storage);
            return storage;
        }

        /// <returns>Whether the value changed.</returns>
        private static bool RestoreOrResetToDefault(ISettingValueRuntimeStorage storage, string savedText)
        {
            if (storage.TryRestoreFromText(savedText, out bool hasChanged))
            {
                return hasChanged;
            }

            Echo.Warning($"Resetting setting '{storage.SettingSO.SettingId}' to its default: its saved value '{savedText}' is invalid.");
            return storage.ResetToDefault();
        }

        private void RaiseSettingChangedIf(bool hasChanged, SettingDefinitionSO settingSO)
        {
            if (hasChanged)
            {
                OnSettingChanged?.Invoke(settingSO);
            }
        }

        #endregion
    }
}
