using System;

namespace FakeMG.Settings.Converters
{
    public interface ISettingValueConverter
    {
        Type ValueType { get; }
        string TypeId { get; }
        string Serialize(object value);
        bool TryDeserialize(string serializedValue, out object value);
    }
}