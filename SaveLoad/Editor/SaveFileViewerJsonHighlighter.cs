using System;
using System.Text;

namespace FakeMG.SaveLoad.Editor
{
    internal readonly struct SaveFileViewerJsonColors
    {
        public SaveFileViewerJsonColors(string keyHex, string stringHex, string numberHex, string keywordHex)
        {
            KeyHex = keyHex;
            StringHex = stringHex;
            NumberHex = numberHex;
            KeywordHex = keywordHex;
        }

        public string KeyHex { get; }

        public string StringHex { get; }

        public string NumberHex { get; }

        public string KeywordHex { get; }
    }

    /// <summary>
    /// Converts JSON text into IMGUI rich text with colored tokens. Tolerates invalid JSON because the input is
    /// whatever the user is currently typing. The visible characters of the output always match the input exactly,
    /// so the result can be drawn on top of an editable text area.
    /// </summary>
    internal sealed class SaveFileViewerJsonHighlighter
    {
        private const string NO_PARSE_OPEN_TAG = "<noparse>";
        private const string NO_PARSE_CLOSE_TAG = "</noparse>";
        // A zero-width space keeps a literal "</noparse>" inside user text from closing the escape early.
        private const string NEUTRALIZED_NO_PARSE_CLOSE_TAG = "<​/noparse>";

        private readonly StringBuilder _builder = new();

        #region Public Methods

        public string Highlight(string json, SaveFileViewerJsonColors colors)
        {
            _builder.Clear();
            int index = 0;
            int plainRunStartIndex = 0;

            while (index < json.Length)
            {
                char character = json[index];
                int tokenEndIndex;
                string colorHex;

                if (character == '"')
                {
                    tokenEndIndex = FindStringEnd(json, index);
                    colorHex = IsFollowedByColon(json, tokenEndIndex) ? colors.KeyHex : colors.StringHex;
                }
                else if (character == '-' || char.IsDigit(character))
                {
                    tokenEndIndex = FindNumberEnd(json, index);
                    colorHex = colors.NumberHex;
                }
                else if (char.IsLetter(character))
                {
                    tokenEndIndex = FindWordEnd(json, index);
                    colorHex = IsKeyword(json, index, tokenEndIndex) ? colors.KeywordHex : null;
                }
                else
                {
                    index++;
                    continue;
                }

                if (colorHex == null)
                {
                    index = tokenEndIndex;
                    continue;
                }

                AppendPlain(json, plainRunStartIndex, index);
                AppendColored(json, index, tokenEndIndex, colorHex);
                index = tokenEndIndex;
                plainRunStartIndex = index;
            }

            AppendPlain(json, plainRunStartIndex, json.Length);
            return _builder.ToString();
        }

        #endregion

        #region Private Methods

        private static int FindStringEnd(string json, int openingQuoteIndex)
        {
            int index = openingQuoteIndex + 1;

            while (index < json.Length)
            {
                char character = json[index];
                if (character == '\\')
                {
                    index += 2;
                    continue;
                }

                index++;
                if (character == '"' || character == '\n')
                {
                    break;
                }
            }

            return Math.Min(index, json.Length);
        }

        private static bool IsFollowedByColon(string json, int index)
        {
            while (index < json.Length && char.IsWhiteSpace(json[index]))
            {
                index++;
            }

            return index < json.Length && json[index] == ':';
        }

        private static int FindNumberEnd(string json, int startIndex)
        {
            int index = startIndex + 1;

            while (index < json.Length && IsNumberCharacter(json[index]))
            {
                index++;
            }

            return index;
        }

        private static bool IsNumberCharacter(char character)
        {
            return char.IsDigit(character) || character is '.' or 'e' or 'E' or '+' or '-';
        }

        private static int FindWordEnd(string json, int startIndex)
        {
            int index = startIndex + 1;

            while (index < json.Length && char.IsLetter(json[index]))
            {
                index++;
            }

            return index;
        }

        private static bool IsKeyword(string json, int startIndex, int endIndex)
        {
            int length = endIndex - startIndex;
            return (length == 4 && (string.CompareOrdinal(json, startIndex, "true", 0, 4) == 0
                                    || string.CompareOrdinal(json, startIndex, "null", 0, 4) == 0))
                   || (length == 5 && string.CompareOrdinal(json, startIndex, "false", 0, 5) == 0);
        }

        private void AppendColored(string json, int startIndex, int endIndex, string colorHex)
        {
            _builder.Append("<color=#").Append(colorHex).Append('>');
            AppendEscaped(json, startIndex, endIndex);
            _builder.Append("</color>");
        }

        private void AppendPlain(string json, int startIndex, int endIndex)
        {
            if (endIndex > startIndex)
            {
                AppendEscaped(json, startIndex, endIndex);
            }
        }

        private void AppendEscaped(string json, int startIndex, int endIndex)
        {
            if (json.IndexOf('<', startIndex, endIndex - startIndex) < 0)
            {
                _builder.Append(json, startIndex, endIndex - startIndex);
                return;
            }

            string segment = json.Substring(startIndex, endIndex - startIndex);
            _builder.Append(NO_PARSE_OPEN_TAG)
                .Append(segment.Replace(NO_PARSE_CLOSE_TAG, NEUTRALIZED_NO_PARSE_CLOSE_TAG))
                .Append(NO_PARSE_CLOSE_TAG);
        }

        #endregion
    }
}
