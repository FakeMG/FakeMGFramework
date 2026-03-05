using System;
using System.Collections.Generic;
using FakeMG.SaveLoad;

namespace FakeMG.Settings
{
    public interface ISettingValue { }

    public class SettingValue<T> : ISettingValue
    {
        public T value;
        public Action<T> OnChanged;
    }

    public class SettingDataManager : Saveable
    {
        private Dictionary<string, ISettingValue> _currentData = new();

        public override object CaptureState()
        {
            return _currentData;
        }

        public override void RestoreDefaultState()
        {
            _currentData = new Dictionary<string, ISettingValue>();
        }

        public override void RestoreState(object data)
        {
            if (data is Dictionary<string, ISettingValue> settingData)
            {
                _currentData = settingData;
                return;
            }

            RestoreDefaultState();
        }

        public void SetValue<T>(SettingDataGeneric<T> setting, T newValue)
        {
            var storage = GetOrCreateStorage(setting);
            if (!EqualityComparer<T>.Default.Equals(storage.value, newValue))
            {
                storage.value = newValue;
                storage.OnChanged?.Invoke(newValue);
            }
        }

        public T GetValue<T>(SettingDataGeneric<T> setting)
        {
            return GetOrCreateStorage(setting).value;
        }

        public void Subscribe<T>(SettingDataGeneric<T> setting, Action<T> callback)
        {
            GetOrCreateStorage(setting).OnChanged += callback;
        }

        private SettingValue<T> GetOrCreateStorage<T>(SettingDataGeneric<T> setting)
        {
            if (!_currentData.TryGetValue(setting.SettingId, out ISettingValue storage))
            {
                T defaultValue = setting.GetDefaultValue();
                storage = new SettingValue<T> { value = defaultValue };
                _currentData.Add(setting.SettingId, storage);
            }
            return (SettingValue<T>)storage;
        }
    }
}