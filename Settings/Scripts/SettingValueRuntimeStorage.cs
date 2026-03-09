using System;

namespace FakeMG.Settings
{
    public class SettingValueRuntimeStorage<T> : ISettingValueRuntimeStorage
    {
        public T value;
        public Action<SettingDefinitionGenericSO<T>, T> OnChanged;

        public Type ValueType => typeof(T);

        public object GetValue()
        {
            return value;
        }
    }
}