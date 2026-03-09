using System.Globalization;
using UnityEngine.Scripting;

namespace FakeMG.Settings.Converters
{
    [Preserve]
    public sealed class IntSettingValueConverter : SettingValueConverter<int>
    {
        public override string TypeId => "int";

        protected override string SerializeTyped(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        protected override bool TryDeserializeTyped(string serializedValue, out int value)
        {
            return int.TryParse(serializedValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }
    }

    [Preserve]
    public sealed class FloatSettingValueConverter : SettingValueConverter<float>
    {
        public override string TypeId => "float";

        protected override string SerializeTyped(float value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        protected override bool TryDeserializeTyped(string serializedValue, out float value)
        {
            return float.TryParse(serializedValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value);
        }
    }

    [Preserve]
    public sealed class BoolSettingValueConverter : SettingValueConverter<bool>
    {
        public override string TypeId => "bool";

        protected override string SerializeTyped(bool value)
        {
            return value.ToString();
        }

        protected override bool TryDeserializeTyped(string serializedValue, out bool value)
        {
            return bool.TryParse(serializedValue, out value);
        }
    }

    [Preserve]
    public sealed class StringSettingValueConverter : SettingValueConverter<string>
    {
        public override string TypeId => "string";

        protected override string SerializeTyped(string value)
        {
            return value ?? string.Empty;
        }

        protected override bool TryDeserializeTyped(string serializedValue, out string value)
        {
            value = serializedValue;
            return true;
        }
    }

    [Preserve]
    public sealed class DoubleSettingValueConverter : SettingValueConverter<double>
    {
        public override string TypeId => "double";

        protected override string SerializeTyped(double value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        protected override bool TryDeserializeTyped(string serializedValue, out double value)
        {
            return double.TryParse(serializedValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value);
        }
    }

    [Preserve]
    public sealed class LongSettingValueConverter : SettingValueConverter<long>
    {
        public override string TypeId => "long";

        protected override string SerializeTyped(long value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        protected override bool TryDeserializeTyped(string serializedValue, out long value)
        {
            return long.TryParse(serializedValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }
    }
}