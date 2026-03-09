using UnityEngine;

namespace FakeMG.Settings
{
    public abstract class SettingDefinitionGenericSO<T> : SettingDefinitionSO
    {
        [SerializeField] private T _defaultValue;

        public T DefaultIndex => _defaultValue;

        public virtual T GetDefaultValue()
        {
            return _defaultValue;
        }
    }
}