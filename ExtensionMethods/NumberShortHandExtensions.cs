using System;
using System.Globalization;
using System.Numerics;
using FakeMG.Numbers;
using UnityEngine;

namespace FakeMG.Framework.ExtensionMethods
{
    public static class NumberShortHandExtensions
    {
        public static string ToShorthand(this BigInteger number, int decimalPlaces = 1)
        {
            return BigNumberParser.ToShorthand(number, decimalPlaces);
        }

        public static string ToShorthand(this int number, int decimalPlaces = 1)
        {
            return ((long)number).ToShorthand(decimalPlaces);
        }

        public static string ToShorthand(this long number, int decimalPlaces = 1)
        {
            if (number == 0)
                return "0";

            bool isNegative = number < 0;
            number = Math.Abs(number);

            string result;

            if (number >= 1000000000000000000)
            {
                double value = number / 1000000000000000000.0;
                result = FormatValue(value, decimalPlaces) + "Qn";
            }
            else if (number >= 1000000000000000)
            {
                double value = number / 1000000000000000.0;
                result = FormatValue(value, decimalPlaces) + "Qd";
            }
            else if (number >= 1000000000000)
            {
                double value = number / 1000000000000.0;
                result = FormatValue(value, decimalPlaces) + "T";
            }
            else if (number >= 1000000000)
            {
                double value = number / 1000000000.0;
                result = FormatValue(value, decimalPlaces) + "B";
            }
            else if (number >= 1000000)
            {
                double value = number / 1000000.0;
                result = FormatValue(value, decimalPlaces) + "M";
            }
            else if (number >= 1000)
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

        public static string ToShorthand(this float number, int decimalPlaces = 1)
        {
            return ((double)number).ToShorthand(decimalPlaces);
        }

        public static string ToShorthand(this double number, int decimalPlaces = 1)
        {
            if (number == 0)
                return "0";

            bool isNegative = number < 0;
            number = Math.Abs(number);

            string result;

            if (number >= 1000000000000000000)
            {
                double value = number / 1000000000000000000.0;
                result = FormatValue(value, decimalPlaces) + "Qn";
            }
            else if (number >= 1000000000000000)
            {
                double value = number / 1000000000000000.0;
                result = FormatValue(value, decimalPlaces) + "Qd";
            }
            else if (number >= 1000000000000)
            {
                double value = number / 1000000000000.0;
                result = FormatValue(value, decimalPlaces) + "T";
            }
            else if (number >= 1000000000)
            {
                double value = number / 1000000000.0;
                result = FormatValue(value, decimalPlaces) + "B";
            }
            else if (number >= 1000000)
            {
                double value = number / 1000000.0;
                result = FormatValue(value, decimalPlaces) + "M";
            }
            else if (number >= 1000)
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
            if (Math.Abs(value - Math.Round(value)) < 0.0001)
            {
                return Math.Round(value).ToString(CultureInfo.InvariantCulture);
            }

            string format = "F" + Mathf.Clamp(decimalPlaces, 0, 15);

            // Forced InvariantCulture to protect string formatting and the '.' check from regional machine settings
            string result = value.ToString(format, CultureInfo.InvariantCulture);

            if (result.Contains("."))
            {
                result = result.TrimEnd('0').TrimEnd('.');
            }

            return result;
        }
    }
}