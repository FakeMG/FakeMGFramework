using UnityEngine;

namespace FakeMG.Settings
{
    public abstract class SettingDataGeneric<T> : SettingDataSO
    {
        [SerializeField] private T _defaultValue;

        public T DefaultIndex => _defaultValue;

        public virtual T GetDefaultValue()
        {
            return _defaultValue;
        }
    }
}