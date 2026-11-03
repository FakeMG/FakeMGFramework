using UnityEditor;
using UnityEngine;

namespace FakeMG.SaveLoad.Editor
{
    /// <summary>
    /// Editable JSON text area that shows syntax highlighting while not being edited. IMGUI cannot edit rich text
    /// directly, so the unfocused text area draws transparent text and the highlighted copy is painted over it with an
    /// identical style, laying out the same glyphs in the same places. While focused, it is a plain text area: IMGUI
    /// draws the caret in the text color, and re-highlighting on every keystroke makes typing laggy.
    /// </summary>
    internal sealed class SaveFileViewerJsonTextArea
    {
        // Rich text roughly triples the string size; past this the extra text generation cost makes typing laggy.
        private const int MAX_HIGHLIGHTED_JSON_LENGTH = 100_000;

        private static readonly SaveFileViewerJsonColors DARK_SKIN_COLORS = new("9CDCFE", "CE9178", "B5CEA8", "569CD6");
        private static readonly SaveFileViewerJsonColors LIGHT_SKIN_COLORS = new("0451A5", "A31515", "098658", "0000FF");

        private readonly SaveFileViewerJsonHighlighter _highlighter = new();
        private readonly GUIContent _highlightedContent = new();
        private GUIStyle _transparentInputStyle;
        private GUIStyle _highlightOverlayStyle;
        private bool? _isProSkin;
        private string _highlightedSourceJson;

        #region Public Methods

        public string Draw(string json, string controlName)
        {
            bool isFocused = GUI.GetNameOfFocusedControl() == controlName;
            GUI.SetNextControlName(controlName);

            if (isFocused || json.Length > MAX_HIGHLIGHTED_JSON_LENGTH)
            {
                return EditorGUILayout.TextArea(json, GUILayout.ExpandHeight(true));
            }

            EnsureStyles();
            string editedJson = EditorGUILayout.TextArea(json, _transparentInputStyle, GUILayout.ExpandHeight(true));

            if (Event.current.type == EventType.Repaint)
            {
                Rect textAreaRect = GUILayoutUtility.GetLastRect();
                _highlightOverlayStyle.Draw(textAreaRect, GetHighlightedContent(editedJson), false, false, false, false);
            }

            return editedJson;
        }

        #endregion

        #region Private Methods

        private void EnsureStyles()
        {
            bool isProSkin = EditorGUIUtility.isProSkin;
            if (_transparentInputStyle != null && _isProSkin == isProSkin)
            {
                return;
            }

            _transparentInputStyle = new GUIStyle(EditorStyles.textArea) { richText = false };
            SetTextColor(_transparentInputStyle, Color.clear);

            _highlightOverlayStyle = new GUIStyle(EditorStyles.textArea) { richText = true };
            SetTextColor(_highlightOverlayStyle, EditorStyles.textArea.normal.textColor);
            ClearBackgrounds(_highlightOverlayStyle);

            _isProSkin = isProSkin;
            _highlightedSourceJson = null;
        }

        private GUIContent GetHighlightedContent(string json)
        {
            if (!ReferenceEquals(json, _highlightedSourceJson))
            {
                SaveFileViewerJsonColors colors = EditorGUIUtility.isProSkin ? DARK_SKIN_COLORS : LIGHT_SKIN_COLORS;
                _highlightedContent.text = _highlighter.Highlight(json, colors);
                _highlightedSourceJson = json;
            }

            return _highlightedContent;
        }

        private static void SetTextColor(GUIStyle style, Color textColor)
        {
            foreach (GUIStyleState state in GetStates(style))
            {
                state.textColor = textColor;
            }
        }

        private static void ClearBackgrounds(GUIStyle style)
        {
            foreach (GUIStyleState state in GetStates(style))
            {
                state.background = null;
                state.scaledBackgrounds = null;
            }
        }

        private static GUIStyleState[] GetStates(GUIStyle style)
        {
            return new[]
            {
                style.normal, style.hover, style.active, style.focused,
                style.onNormal, style.onHover, style.onActive, style.onFocused
            };
        }

        #endregion
    }
}
