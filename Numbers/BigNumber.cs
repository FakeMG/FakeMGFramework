using System;
using System.Globalization;
using System.Numerics;

namespace FakeMG.Framework
{
    /// <summary>
    /// Exact arbitrary-magnitude quantity backed by <see cref="BigInteger"/>.
    /// Parses designer-authored shorthand ("130Qn", "2.5M") and renders it back ("1.2K", "3.5M").
    /// Single source of truth for big-number parsing and formatting across the project.
    /// </summary>
    public readonly struct BigNumber : IEquatable<BigNumber>, IComparable<BigNumber>
    {
        private static readonly BigInteger THOUSAND = BigInteger.Pow(10, 3);
        private static readonly BigInteger MILLION = BigInteger.Pow(10, 6);
        private static readonly BigInteger BILLION = BigInteger.Pow(10, 9);
        private static readonly BigInteger TRILLION = BigInteger.Pow(10, 12);
        private static readonly BigInteger QUADRILLION = BigInteger.Pow(10, 15);
        private static readonly BigInteger QUINTILLION = BigInteger.Pow(10, 18);

        public static readonly BigNumber Zero = new(BigInteger.Zero);
        public static readonly BigNumber One = new(BigInteger.One);

        private readonly BigInteger _value;

        public BigNumber(BigInteger value)
        {
            _value = value;
        }

        public BigInteger Value => _value;

        #region Public Methods

        public static BigNumber ParseOrDefault(string text, BigNumber fallback)
        {
            return TryParse(text, out BigNumber value) ? value : fallback;
        }

        public static bool TryParse(string text, out BigNumber value)
        {
            value = Zero;

            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string normalizedText = text.Trim().Replace(",", string.Empty);
            string suffix = ExtractSuffix(normalizedText);
            string numberText = normalizedText[..^suffix.Length];
            if (!TryGetSuffixMultiplier(suffix, out BigInteger multiplier))
            {
                return false;
            }

            if (!TryParseNumber(numberText, multiplier, out BigInteger parsed))
            {
                return false;
            }

            value = new BigNumber(parsed);
            return true;
        }

        public string ToShorthand()
        {
            return ToShorthand(_value);
        }

        /// <summary>
        /// Parses shorthand into a <see cref="double"/>, preserving fractional digits
        /// (e.g. "10.5" stays 10.5). Use for continuous quantities like health that do
        /// not need exact arbitrary-magnitude storage.
        /// </summary>
        public static bool TryParseDouble(string text, out double value)
        {
            value = 0d;

            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string normalizedText = text.Trim().Replace(",", string.Empty);
            string suffix = ExtractSuffix(normalizedText);
            string numberText = normalizedText[..^suffix.Length];
            if (!TryGetSuffixMultiplier(suffix, out BigInteger multiplier))
            {
                return false;
            }

            if (!double.TryParse(numberText, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double number) || number < 0d)
            {
                return false;
            }

            value = number * (double)multiplier;
            return true;
        }

        public static double ParseDoubleOrDefault(string text, double fallback)
        {
            return TryParseDouble(text, out double value) ? value : fallback;
        }

        public int CompareTo(BigNumber other)
        {
            return _value.CompareTo(other._value);
        }

        public bool Equals(BigNumber other)
        {
            return _value.Equals(other._value);
        }

        public override bool Equals(object obj)
        {
            return obj is BigNumber other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return _value.ToString(CultureInfo.InvariantCulture);
        }

        #endregion

        #region Operators

        public static implicit operator BigNumber(int value) => new(value);
        public static implicit operator BigNumber(long value) => new(value);
        public static implicit operator BigNumber(BigInteger value) => new(value);
        public static explicit operator BigInteger(BigNumber value) => value._value;
        public static explicit operator double(BigNumber value) => (double)value._value;

        public static BigNumber operator +(BigNumber left, BigNumber right) => new(left._value + right._value);
        public static BigNumber operator -(BigNumber left, BigNumber right) => new(left._value - right._value);
        public static BigNumber operator *(BigNumber left, BigNumber right) => new(left._value * right._value);

        public static bool operator ==(BigNumber left, BigNumber right) => left._value == right._value;
        public static bool operator !=(BigNumber left, BigNumber right) => left._value != right._value;
        public static bool operator <(BigNumber left, BigNumber right) => left._value < right._value;
        public static bool operator >(BigNumber left, BigNumber right) => left._value > right._value;
        public static bool operator <=(BigNumber left, BigNumber right) => left._value <= right._value;
        public static bool operator >=(BigNumber left, BigNumber right) => left._value >= right._value;

        #endregion

        #region Private Methods

        private static string ExtractSuffix(string text)
        {
            int suffixStartIndex = text.Length;
            while (suffixStartIndex > 0 && char.IsLetter(text[suffixStartIndex - 1]))
            {
                suffixStartIndex--;
            }

            return text[suffixStartIndex..];
        }

        private static bool TryGetSuffixMultiplier(string suffix, out BigInteger multiplier)
        {
            switch (suffix.ToUpperInvariant())
            {
                case "":
                    multiplier = BigInteger.One;
                    return true;
                case "K":
                    multiplier = THOUSAND;
                    return true;
                case "M":
                    multiplier = MILLION;
                    return true;
                case "B":
                    multiplier = BILLION;
                    return true;
                case "T":
                    multiplier = TRILLION;
                    return true;
                case "QD":
                    multiplier = QUADRILLION;
                    return true;
                case "QN":
                    multiplier = QUINTILLION;
                    return true;
                default:
                    multiplier = BigInteger.One;
                    return false;
            }
        }

        private static bool TryParseNumber(string numberText, BigInteger multiplier, out BigInteger value)
        {
            value = BigInteger.Zero;

            if (string.IsNullOrWhiteSpace(numberText) || numberText.StartsWith("-", StringComparison.Ordinal))
            {
                return false;
            }

            string[] parts = numberText.Split('.');
            if (parts.Length > 2 || string.IsNullOrWhiteSpace(parts[0]))
            {
                return false;
            }

            if (!BigInteger.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out BigInteger whole))
            {
                return false;
            }

            value = whole * multiplier;
            if (parts.Length == 1 || string.IsNullOrWhiteSpace(parts[1]))
            {
                return true;
            }

            string fractionalText = parts[1];
            if (!BigInteger.TryParse(fractionalText, NumberStyles.None, CultureInfo.InvariantCulture, out BigInteger fractional))
            {
                return false;
            }

            BigInteger fractionalDivisor = BigInteger.Pow(10, fractionalText.Length);
            value += fractional * multiplier / fractionalDivisor;
            return true;
        }

        private static string ToShorthand(BigInteger number)
        {
            if (number < BigInteger.Zero)
            {
                return "-" + ToShorthand(-number);
            }

            if (number >= QUINTILLION)
            {
                return FormatShorthand(number, QUINTILLION, "Qn");
            }

            if (number >= QUADRILLION)
            {
                return FormatShorthand(number, QUADRILLION, "Qd");
            }

            if (number >= TRILLION)
            {
                return FormatShorthand(number, TRILLION, "T");
            }

            if (number >= BILLION)
            {
                return FormatShorthand(number, BILLION, "B");
            }

            if (number >= MILLION)
            {
                return FormatShorthand(number, MILLION, "M");
            }

            if (number >= THOUSAND)
            {
                return FormatShorthand(number, THOUSAND, "K");
            }

            return number.ToString(CultureInfo.InvariantCulture);
        }

        private static string FormatShorthand(BigInteger number, BigInteger divisor, string suffix)
        {
            BigInteger whole = BigInteger.DivRem(number, divisor, out BigInteger remainder);
            BigInteger firstDecimal = remainder * 10 / divisor;
            if (firstDecimal == BigInteger.Zero)
            {
                return whole.ToString(CultureInfo.InvariantCulture) + suffix;
            }

            return whole.ToString(CultureInfo.InvariantCulture) + "." + firstDecimal.ToString(CultureInfo.InvariantCulture) + suffix;
        }

        #endregion
    }
}
