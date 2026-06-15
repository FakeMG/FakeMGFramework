using System;
using System.Globalization;
using System.Numerics;
using UnityEngine;

namespace FakeMG.Framework.ExtensionMethods
{
    /// <summary>
    /// Provides extension methods to convert various numeric types into localized, clean shorthand string formats (e.g., 1.5K, 2.3M).
    /// </summary>
    public static class NumberShortHandExtensions
    {
        /// <summary>
        /// Converts an arbitrary-magnitude integer to a shorthand format string via <see cref="BigNumberParser"/>.
        /// </summary>
        /// <param name="number">The large integer to format.</param>
        /// <returns>A shorthand string representation (e.g., K, M, B, T, Qd, Qn).</returns>
        public static string ToShorthand(this BigInteger number, int decimalPlaces = 1)
        {
            return BigNumberParser.ToShorthand(number, decimalPlaces);
        }

        /// <summary>
        /// Converts a 32-bit signed integer to shorthand format (K, M, B, T, Qd, Qn).
        /// </summary>
        /// <param name="number">The integer to convert.</param>
        /// <param name="decimalPlaces">The maximum number of decimal places to show (default: 1).</param>
        /// <returns>A formatted string with the appropriate scale suffix.</returns>
        public static string ToShorthand(this int number, int decimalPlaces = 1)
        {
            return ((long)number).ToShorthand(decimalPlaces);
        }

        /// <summary>
        /// Converts a 64-bit signed integer to shorthand format (K, M, B, T, Qd, Qn).
        /// </summary>
        /// <param name="number">The long integer to convert.</param>
        /// <param name="decimalPlaces">The maximum number of decimal places to show (default: 1).</param>
        /// <returns>A formatted string with the appropriate scale suffix.</returns>
        public static string ToShorthand(this long number, int decimalPlaces = 1)
        {
            if (number == 0)
                return "0";

            bool isNegative = number < 0;
            number = Math.Abs(number);

            string result;

            if (number >= 1000000000000000000) // Quintillion
            {
                double value = number / 1000000000000000000.0;
                result = FormatValue(value, decimalPlaces) + "Qn";
            }
            else if (number >= 1000000000000000) // Quadrillion
            {
                double value = number / 1000000000000000.0;
                result = FormatValue(value, decimalPlaces) + "Qd";
            }
            else if (number >= 1000000000000) // Trillion
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
        /// Converts a single-precision floating-point number to shorthand format (K, M, B, T, Qd, Qn).
        /// </summary>
        /// <param name="number">The float to convert.</param>
        /// <param name="decimalPlaces">The maximum number of decimal places to show (default: 1).</param>
        /// <returns>A formatted string with the appropriate scale suffix.</returns>
        public static string ToShorthand(this float number, int decimalPlaces = 1)
        {
            return ((double)number).ToShorthand(decimalPlaces);
        }

        /// <summary>
        /// Converts a double-precision floating-point number to shorthand format (K, M, B, T, Qd, Qn).
        /// </summary>
        /// <param name="number">The double to convert.</param>
        /// <param name="decimalPlaces">The maximum number of decimal places to show (default: 1).</param>
        /// <returns>A formatted string with the appropriate scale suffix.</returns>
        public static string ToShorthand(this double number, int decimalPlaces = 1)
        {
            if (number == 0)
                return "0";

            bool isNegative = number < 0;
            number = Math.Abs(number);

            string result;

            if (number >= 1000000000000000000) // Quintillion
            {
                double value = number / 1000000000000000000.0;
                result = FormatValue(value, decimalPlaces) + "Qn";
            }
            else if (number >= 1000000000000000) // Quadrillion
            {
                double value = number / 1000000000000000.0;
                result = FormatValue(value, decimalPlaces) + "Qd";
            }
            else if (number >= 1000000000000) // Trillion
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

        /// <summary>
        /// Formats a double value to a string using InvariantCulture, trimming unneeded trailing zeros and decimal points.
        /// </summary>
        /// <param name="value">The parsed value to stringify.</param>
        /// <param name="decimalPlaces">The maximum precision allowed.</param>
        /// <returns>A cleaned string representation of the double.</returns>
        private static string FormatValue(double value, int decimalPlaces)
        {
            // Check if the value is a whole number
            if (Math.Abs(value - Math.Round(value)) < 0.0001)
            {
                return Math.Round(value).ToString(CultureInfo.InvariantCulture);
            }

            string format = "F" + Mathf.Clamp(decimalPlaces, 0, 15);

            // Forced InvariantCulture to protect string formatting and the '.' check from regional machine settings
            string result = value.ToString(format, CultureInfo.InvariantCulture);

            // Remove trailing zeros and decimal point if not needed
            if (result.Contains("."))
            {
                result = result.TrimEnd('0').TrimEnd('.');
            }

            return result;
        }
    }
}