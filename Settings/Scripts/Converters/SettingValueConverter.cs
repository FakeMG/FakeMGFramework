using System;

namespace FakeMG.Settings.Converters
{
    public abstract class SettingValueConverter<T> : ISettingValueConverter
    {
        public Type ValueType => typeof(T);
        public abstract string TypeId { get; }

        public string Serialize(object value)
        {
            return SerializeTyped((T)value);
        }

        public bool TryDeserialize(string serializedValue, out object value)
        {
            bool wasSuccessful = TryDeserializeTyped(serializedValue, out T typedValue);
            value = typedValue;
            return wasSuccessful;
        }

        protected abstract string SerializeTyped(T value);
        protected abstract bool TryDeserializeTyped(string serializedValue, out T value);
    }
}