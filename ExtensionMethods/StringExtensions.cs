using System;
using System.Text;
using UnityEngine;

namespace FakeMG.FakeMGFramework.ExtensionMethods
{
    public static class StringExtensions
    {
        private static string SeparateNumberWithComma(this string number)
        {
            StringBuilder sb = new StringBuilder(number);
            int temp = 0;
            for (int i = sb.Length - 1; i >= 0; i--)
            {
                temp++;
                if (temp == 3 && i != 0)
                {
                    sb.Insert(i, ",");
                    temp = 0;
                }
            }

            return sb.ToString();
        }

        public static string SeparateNumberWithComma(this int number)
        {
            return number.ToString().SeparateNumberWithComma();
        }

        public static string SeparateNumberWithComma(this double number)
        {
            return number.ToString("F0").SeparateNumberWithComma();
        }

        public static string SeparateTextByUpperCase(this string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            var result = new StringBuilder();

            for (var i = 0; i < text.Length; i++)
            {
                var currentChar = text[i];

                if (i > 0 && char.IsUpper(currentChar))
                {
                    var previousChar = text[i - 1];
                    bool shouldAddSpace = !char.IsWhiteSpace(previousChar);

                    if (char.IsUpper(previousChar))
                    {
                        bool isNotLastChar = i + 1 < text.Length;
                        if (isNotLastChar)
                        {
                            var nextChar = text[i + 1];
                            if (char.IsUpper(nextChar))
                            {
                                shouldAddSpace = false;
                            }
                        }
                        else
                        {
                            shouldAddSpace = false;
                        }
                    }

                    if (shouldAddSpace)
                    {
                        result.Append(' ');
                    }
                }

                result.Append(currentChar);
            }

            var resultString = result.ToString();
            if (resultString.Length > 0)
            {
                resultString = char.ToUpper(resultString[0]) + resultString.Substring(1);
            }

            return resultString;
        }
    }
}