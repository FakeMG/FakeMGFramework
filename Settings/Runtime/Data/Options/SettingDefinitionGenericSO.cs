using UnityEngine;

namespace FakeMG.Settings
{
    public abstract class SettingDefinitionGenericSO<T> : SettingDefinitionSO
    {
        //TODO: if GetDefaultValue is overridden, prevent editing the default value in the inspector. This is to prevent confusion about which default value is used.
        [SerializeField] private T _defaultValue;

        public T DefaultIndex => _defaultValue;

        public virtual T GetDefaultValue()
        {
            return _defaultValue;
        }
    }
}