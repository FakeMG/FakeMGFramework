using System.Text;

namespace FakeMG.FakeMGFramework.ExtensionMethods {
    public static class StringExtensions {
        private static string SeparateNumberWithComma(this string number) {
            StringBuilder sb = new StringBuilder(number);
            int temp = 0;
            for (int i = sb.Length - 1; i >= 0; i--) {
                temp++;
                if (temp == 3 && i != 0) {
                    sb.Insert(i, ",");
                    temp = 0;
                }
            }

            return sb.ToString();
        }
        
        public static string SeparateNumberWithComma(this int number) {
            return number.ToString().SeparateNumberWithComma();
        }
        
        public static string SeparateNumberWithComma(this double number) {
            return number.ToString("F0").SeparateNumberWithComma();
        }
        
        public static string SeparateTextByUpperCase(this string text) {
            var result = "";
            for (var i = 0; i < text.Length; i++) {
                if (i != 0 && char.IsUpper(text[i])) {
                    result += " ";
                }

                result += text[i];
            }

            // first letter to upper case
            result = char.ToUpper(result[0]) + result.Substring(1);
            return result;
        }
    }
}