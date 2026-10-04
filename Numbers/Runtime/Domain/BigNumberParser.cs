using System;
using System.Globalization;
using System.Numerics;

namespace FakeMG.Numbers
{
    public static class BigNumberParser
    {
        private static readonly BigInteger THOUSAND = BigInteger.Pow(10, 3);
        private static readonly BigInteger MILLION = BigInteger.Pow(10, 6);
        private static readonly BigInteger BILLION = BigInteger.Pow(10, 9);
        private static readonly BigInteger TRILLION = BigInteger.Pow(10, 12);
        private static readonly BigInteger QUADRILLION = BigInteger.Pow(10, 15);
        private static readonly BigInteger QUINTILLION = BigInteger.Pow(10, 18);
        private static readonly BigInteger SEXTILLION = BigInteger.Pow(10, 21);
        private static readonly BigInteger SEPTILLION = BigInteger.Pow(10, 24);

        #region Public Methods

        public static BigInteger ParseOrDefault(string text, BigInteger fallback)
        {
            return TryParse(text, out BigInteger value) ? value : fallback;
        }

        public static bool TryParse(string text, out BigInteger value)
        {
            value = BigInteger.Zero;

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

            return TryParseNumber(numberText, multiplier, out value);
        }

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

        public static string ToShorthand(BigInteger number, int decimalPlaces = 1)
        {
            if (number < BigInteger.Zero)
            {
                return "-" + ToShorthand(-number, decimalPlaces);
            }

            if (number >= SEPTILLION) return FormatShorthand(number, SEPTILLION, "Sp", decimalPlaces);
            if (number >= SEXTILLION) return FormatShorthand(number, SEXTILLION, "Sx", decimalPlaces);
            if (number >= QUINTILLION) return FormatShorthand(number, QUINTILLION, "Qn", decimalPlaces);
            if (number >= QUADRILLION) return FormatShorthand(number, QUADRILLION, "Qd", decimalPlaces);
            if (number >= TRILLION) return FormatShorthand(number, TRILLION, "T", decimalPlaces);
            if (number >= BILLION) return FormatShorthand(number, BILLION, "B", decimalPlaces);
            if (number >= MILLION) return FormatShorthand(number, MILLION, "M", decimalPlaces);
            if (number >= THOUSAND) return FormatShorthand(number, THOUSAND, "K", decimalPlaces);

            return number.ToString(CultureInfo.InvariantCulture);
        }

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
                case "": multiplier = BigInteger.One; return true;
                case "K": multiplier = THOUSAND; return true;
                case "M": multiplier = MILLION; return true;
                case "B": multiplier = BILLION; return true;
                case "T": multiplier = TRILLION; return true;
                case "QD": multiplier = QUADRILLION; return true;
                case "QN": multiplier = QUINTILLION; return true;
                case "SX": multiplier = SEXTILLION; return true;
                case "SP": multiplier = SEPTILLION; return true;
                default: multiplier = BigInteger.One; return false;
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
                return value > BigInteger.Zero;
            }

            string fractionalText = parts[1];
            if (!BigInteger.TryParse(fractionalText, NumberStyles.None, CultureInfo.InvariantCulture, out BigInteger fractional))
            {
                return false;
            }

            BigInteger fractionalDivisor = BigInteger.Pow(10, fractionalText.Length);
            value += fractional * multiplier / fractionalDivisor;
            return value > BigInteger.Zero;
        }

        private static string FormatShorthand(BigInteger number, BigInteger divisor, string suffix, int decimalPlaces)
        {
            BigInteger whole = BigInteger.DivRem(number, divisor, out BigInteger remainder);

            if (decimalPlaces <= 0 || remainder == BigInteger.Zero)
            {
                return whole.ToString(CultureInfo.InvariantCulture) + suffix;
            }

            BigInteger scale = BigInteger.Pow(10, decimalPlaces);
            BigInteger fractionalDigits = remainder * scale / divisor;
            if (fractionalDigits == BigInteger.Zero)
            {
                return whole.ToString(CultureInfo.InvariantCulture) + suffix;
            }

            string fractionalText = fractionalDigits.ToString(CultureInfo.InvariantCulture)
                .PadLeft(decimalPlaces, '0')
                .TrimEnd('0');

            return whole.ToString(CultureInfo.InvariantCulture) + "." + fractionalText + suffix;
        }

        #endregion
    }
}
