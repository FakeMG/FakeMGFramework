using System;
using System.Globalization;
using UnityEngine;

namespace FakeMG.Framework.ExtensionMethods
{
    public static class NumberShortHandExtensions
    {
        /// <summary>
        /// Converts an integer to shorthand format (K, M, B, T)
        /// </summary>
        /// <param name="number">The number to convert</param>
        /// <param name="decimalPlaces">Number of decimal places to show (default: 1)</param>
        /// <returns>Formatted string with appropriate suffix</returns>
        public static string ToShorthand(this int number, int decimalPlaces = 1)
        {
            return ((long)number).ToShorthand(decimalPlaces);
        }

        /// <summary>
        /// Converts a long integer to shorthand format (K, M, B, T)
        /// </summary>
        /// <param name="number">The number to convert</param>
        /// <param name="decimalPlaces">Number of decimal places to show (default: 1)</param>
        /// <returns>Formatted string with appropriate suffix</returns>
        public static string ToShorthand(this long number, int decimalPlaces = 1)
        {
            if (number == 0)
                return "0";

            bool isNegative = number < 0;
            number = Math.Abs(number);

            string result;

            if (number >= 1000000000000) // Trillion
            {
                double value = number / 1000000000000.0;
                result = FormatValue(value, decimalPlaces) + "T";
            }
            else if (number >= 1000000000) // Billion
            {
                double value = number / 1000000000.0;
                result = FormatValue(value, decimalPlaces) + "B";
            }
            else if (number >= 1000000) // A Million
            {
                double value = number / 1000000.0;
                result = FormatValue(value, decimalPlaces) + "M";
            }
            else if (number >= 1000) // A Thousand
            {
                double value = number / 1000.0;
                result = FormatValue(value, decimalPlaces) + "K";
            }
            else
            {
                result = number.ToString();
            }

            return isNegative ? "-" + result : result;
        }

        /// <summary>
        /// Converts a float to shorthand format (K, M, B, T)
        /// </summary>
        /// <param name="number">The number to convert</param>
        /// <param name="decimalPlaces">Number of decimal places to show (default: 1)</param>
        /// <returns>Formatted string with appropriate suffix</returns>
        public static string ToShorthand(this float number, int decimalPlaces = 1)
        {
            return ((double)number).ToShorthand(decimalPlaces);
        }

        /// <summary>
        /// Converts a double to shorthand format (K, M, B, T)
        /// </summary>
        /// <param name="number">The number to convert</param>
        /// <param name="decimalPlaces">Number of decimal places to show (default: 1)</param>
        /// <returns>Formatted string with appropriate suffix</returns>
        public static string ToShorthand(this double number, int decimalPlaces = 1)
        {
            if (number == 0)
                return "0";

            bool isNegative = number < 0;
            number = Math.Abs(number);

            string result;

            if (number >= 1000000000000) // Trillion
            {
                double value = number / 1000000000000.0;
                result = FormatValue(value, decimalPlaces) + "T";
            }
            else if (number >= 1000000000) // Billion
            {
                double value = number / 1000000000.0;
                result = FormatValue(value, decimalPlaces) + "B";
            }
            else if (number >= 1000000) // A Million
            {
                double value = number / 1000000.0;
                result = FormatValue(value, decimalPlaces) + "M";
            }
            else if (number >= 1000) // A Thousand
            {
                double value = number / 1000.0;
                result = FormatValue(value, decimalPlaces) + "K";
            }
            else
            {
                result = FormatValue(number, decimalPlaces);
            }

            return isNegative ? "-" + result : result;
        }

        private static string FormatValue(double value, int decimalPlaces)
        {
            // Check if the value is a whole number
            if (Math.Abs(value - Math.Round(value)) < 0.0001)
            {
                return Math.Round(value).ToString(CultureInfo.InvariantCulture);
            }

            string format = "F" + Mathf.Clamp(decimalPlaces, 0, 15);
            string result = value.ToString(format);

            // Remove trailing zeros and decimal point if not needed
            if (result.Contains("."))
            {
                result = result.TrimEnd('0').TrimEnd('.');
            }

            return result;
        }
    }
}