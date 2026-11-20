using System;
using System.Collections.Generic;
using System.Globalization;

namespace FakeMG.Settings
{
    /// <summary>Converts setting values to and from the text the settings save document stores.</summary>
    public static class SettingValueText
    {
        private delegate bool TextParser(string text, out object value);

        private static readonly Dictionary<Type, TextParser> PARSERS = new()
        {
            [typeof(string)] = ParseString,
            [typeof(bool)] = ParseBool,
            [typeof(int)] = ParseInt,
            [typeof(long)] = ParseLong,
            [typeof(float)] = ParseFloat,
            [typeof(double)] = ParseDouble
        };

        #region Public Methods

        public static bool CanConvert(Type valueType)
        {
            return valueType.IsEnum || PARSERS.ContainsKey(valueType);
        }

        public static string Format<T>(T value)
        {
            return value is IFormattable formattable ? formattable.ToString(null, CultureInfo.InvariantCulture) : value?.ToString() ?? string.Empty;
        }

        public static bool TryParse<T>(string text, out T value)
        {
            Type valueType = typeof(T);
            bool isParsed = valueType.IsEnum ? TryParseEnum(valueType, text, out object parsedValue) : PARSERS[valueType](text, out parsedValue);
            value = isParsed ? (T)parsedValue : default;
            return isParsed;
        }

        #endregion

        #region Private Methods

        private static bool TryParseEnum(Type enumType, string text, out object value)
        {
            return Enum.TryParse(enumType, text, ignoreCase: false, out value) && Enum.IsDefined(enumType, value);
        }

        private static bool ParseString(string text, out object value)
        {
            value = text;
            return text != null;
        }

        private static bool ParseBool(string text, out object value)
        {
            bool isParsed = bool.TryParse(text, out bool parsedValue);
            value = parsedValue;
            return isParsed;
        }

        private static bool ParseInt(string text, out object value)
        {
            bool isParsed = int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedValue);
            value = parsedValue;
            return isParsed;
        }

        private static bool ParseLong(string text, out object value)
        {
            bool isParsed = long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsedValue);
            value = parsedValue;
            return isParsed;
        }

        private static bool ParseFloat(string text, out object value)
        {
            bool isParsed = float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedValue);
            value = parsedValue;
            return isParsed;
        }

        private static bool ParseDouble(string text, out object value)
        {
            bool isParsed = double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedValue);
            value = parsedValue;
            return isParsed;
        }

        #endregion
    }
}
