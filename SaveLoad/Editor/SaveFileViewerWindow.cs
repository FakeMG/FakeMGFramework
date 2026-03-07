using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FakeMG.Framework;
using FakeMG.SaveLoad.Advanced;
using UnityEditor;
using UnityEngine;

namespace FakeMG.SaveLoad.Editor
{
    public sealed class SaveFileViewerWindow : EditorWindow
    {
        private const float LEFT_PANEL_MIN_WIDTH = 220f;
        private const float LEFT_PANEL_MAX_WIDTH = 500f;
        private const float SPLITTER_WIDTH = 4f;

        private float _leftPanelWidth = 280f;
        private bool _isResizingSplitter;

        private List<ManagedSaveFileInfo> _fileEntries = new();
        private string _selectedFilePath;
        private string[] _keys = Array.Empty<string>();
        private string _selectedKey;
        private object _cachedKeyData;
        private bool _isDirty;

        private bool _isRawJsonMode;
        private string _cachedRawJson;
        private string _fullFileRawJson;

        private Vector2 _fileListScroll;
        private Vector2 _keyListScroll;
        private Vector2 _dataEditorScroll;

        private string _newKeyName = string.Empty;

        [MenuItem(FakeMGEditorMenus.SAVE_FILE_VIEWER)]
        private static void ShowWindow()
        {
            var window = GetWindow<SaveFileViewerWindow>("Save File Viewer");
            window.minSize = new Vector2(700, 400);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshFileList();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            DrawLeftPanel();
            DrawSplitter();
            DrawRightPanel();
            EditorGUILayout.EndHorizontal();

            HandleSplitterDrag();
        }

        #region Left Panel — File List

        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(_leftPanelWidth));

            DrawFileListToolbar();
            DrawFileListEntries();

            EditorGUILayout.EndVertical();
        }

        private void DrawFileListToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
            {
                RefreshFileList();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawFileListEntries()
        {
            _fileListScroll = EditorGUILayout.BeginScrollView(_fileListScroll);

            if (_fileEntries.Count == 0)
            {
                EditorGUILayout.HelpBox("No save files found.", MessageType.Info);
            }

            for (int i = 0; i < _fileEntries.Count; i++)
            {
                DrawFileEntry(_fileEntries[i]);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawFileEntry(ManagedSaveFileInfo entry)
        {
            bool isSelected = _selectedFilePath == entry.FilePath;
            GUIStyle style = isSelected
                ? CreateSelectedStyle()
                : EditorStyles.helpBox;

            EditorGUILayout.BeginVertical(style);

            EditorGUILayout.BeginHorizontal();

            string badge = entry.Metadata.IsAutoSave ? "[Auto]" : "[Manual]";
            EditorGUILayout.LabelField($"{badge} {entry.FileName}", EditorStyles.boldLabel);

            if (GUILayout.Button("X", GUILayout.Width(22), GUILayout.Height(18)))
            {
                DeleteFile(entry);
            }

            EditorGUILayout.EndHorizontal();

            string gameVersion = string.IsNullOrWhiteSpace(entry.Metadata.GameVersion)
                ? "Unknown"
                : entry.Metadata.GameVersion;
            EditorGUILayout.LabelField($"Version: {gameVersion}    {entry.Metadata.Timestamp:yyyy-MM-dd HH:mm:ss}", EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();

            Rect entryRect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.MouseDown && entryRect.Contains(Event.current.mousePosition))
            {
                SelectFile(entry.FilePath);
                Event.current.Use();
            }
        }

        #endregion

        #region Splitter

        private void DrawSplitter()
        {
            Rect splitterRect = EditorGUILayout.BeginVertical(GUILayout.Width(SPLITTER_WIDTH));
            GUILayout.Box(string.Empty,
                GUILayout.Width(SPLITTER_WIDTH),
                GUILayout.ExpandHeight(true));
            EditorGUILayout.EndVertical();

            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);

            if (Event.current.type == EventType.MouseDown && splitterRect.Contains(Event.current.mousePosition))
            {
                _isResizingSplitter = true;
                Event.current.Use();
            }
        }

        private void HandleSplitterDrag()
        {
            if (!_isResizingSplitter) return;

            if (Event.current.type == EventType.MouseDrag)
            {
                _leftPanelWidth = Mathf.Clamp(
                    Event.current.mousePosition.x,
                    LEFT_PANEL_MIN_WIDTH,
                    LEFT_PANEL_MAX_WIDTH);
                Repaint();
            }

            if (Event.current.type == EventType.MouseUp)
            {
                _isResizingSplitter = false;
            }
        }

        #endregion

        #region Right Panel — Key List + Data Editor

        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical();

            if (string.IsNullOrEmpty(_selectedFilePath))
            {
                EditorGUILayout.HelpBox("Select a save file to view its contents.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.LabelField(Path.GetFileName(_selectedFilePath), EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            DrawKeyList();
            DrawDataEditor();

            EditorGUILayout.EndVertical();
        }

        private void DrawKeyList()
        {
            EditorGUILayout.LabelField("Keys", EditorStyles.boldLabel);

            _keyListScroll = EditorGUILayout.BeginScrollView(
                _keyListScroll, GUILayout.MaxHeight(150));

            foreach (string key in _keys)
            {
                DrawKeyRow(key);
            }

            EditorGUILayout.EndScrollView();

            DrawAddKeyRow();
            EditorGUILayout.Space(4);
        }

        private void DrawKeyRow(string key)
        {
            bool isSelected = _selectedKey == key;

            EditorGUILayout.BeginHorizontal();

            GUIStyle buttonStyle = isSelected
                ? CreateSelectedButtonStyle()
                : GUI.skin.button;

            if (GUILayout.Button(key, buttonStyle))
            {
                SelectKey(key);
            }

            bool isMetadataKey = key == SaveFileCatalog.METADATA_KEY;
            EditorGUI.BeginDisabledGroup(isMetadataKey);

            if (GUILayout.Button("X", GUILayout.Width(22)))
            {
                DeleteKey(key);
            }

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAddKeyRow()
        {
            EditorGUILayout.BeginHorizontal();
            _newKeyName = EditorGUILayout.TextField(_newKeyName);

            EditorGUI.BeginDisabledGroup(string.IsNullOrWhiteSpace(_newKeyName));
            if (GUILayout.Button("Add Key", GUILayout.Width(70)))
            {
                AddNewKey(_newKeyName.Trim());
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawDataEditor()
        {
            if (string.IsNullOrEmpty(_selectedKey))
            {
                EditorGUILayout.HelpBox("Select a key to view and edit its data.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"Editing: {_selectedKey}", EditorStyles.boldLabel);
            DrawDataEditorToolbar();

            _dataEditorScroll = EditorGUILayout.BeginScrollView(_dataEditorScroll);

            if (_isRawJsonMode)
            {
                DrawRawJsonEditor();
            }
            else if (_cachedKeyData == null)
            {
                EditorGUILayout.HelpBox("Data is null or could not be loaded.", MessageType.Warning);
            }
            else
            {
                bool modified = ReflectionDataDrawer.DrawObject(_cachedKeyData);
                if (modified)
                {
                    _isDirty = true;
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawRawJsonEditor()
        {
            EditorGUILayout.HelpBox(
                "Typed editing is not available for this key (type could not be resolved). Showing raw JSON data.",
                MessageType.Warning);

            if (string.IsNullOrEmpty(_cachedRawJson))
            {
                EditorGUILayout.HelpBox("Could not extract raw JSON for this key.", MessageType.Error);
                return;
            }

            EditorGUI.BeginChangeCheck();
            _cachedRawJson = EditorGUILayout.TextArea(_cachedRawJson, GUILayout.ExpandHeight(true));
            if (EditorGUI.EndChangeCheck())
            {
                _isDirty = true;
            }
        }

        private void DrawDataEditorToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            EditorGUI.BeginDisabledGroup(!_isDirty);
            if (GUILayout.Button("Save Changes", EditorStyles.toolbarButton))
            {
                SaveCurrentKeyData();
            }
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Revert", EditorStyles.toolbarButton))
            {
                ReloadCurrentKeyData();
            }

            GUILayout.FlexibleSpace();

            if (_isDirty)
            {
                GUILayout.Label("* Unsaved Changes", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Actions

        private void RefreshFileList()
        {
            _fileEntries.Clear();
            _selectedFilePath = null;
            _selectedKey = null;
            _cachedKeyData = null;
            _keys = Array.Empty<string>();
            _isDirty = false;
            ClearRawJsonState();
            ReflectionDataDrawer.ClearExpandedState();

            _fileEntries = SaveFileCatalog.GetManagedSaveFiles();

            _fileEntries = _fileEntries
                .OrderByDescending(entry => entry.Metadata.Timestamp)
                .ToList();

            Repaint();
        }

        private void SelectFile(string filePath)
        {
            if (_isDirty && !ConfirmDiscardChanges())
                return;

            _selectedFilePath = filePath;
            _selectedKey = null;
            _cachedKeyData = null;
            _isDirty = false;
            ClearRawJsonState();
            ReflectionDataDrawer.ClearExpandedState();

            try
            {
                _keys = ES3.GetKeys(filePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveFileViewer] Failed to load keys from {filePath}: {e.Message}");
                _keys = Array.Empty<string>();
            }

            Repaint();
        }

        private void SelectKey(string key)
        {
            if (_isDirty && !ConfirmDiscardChanges())
                return;

            _selectedKey = key;
            ReflectionDataDrawer.ClearExpandedState();
            ReloadCurrentKeyData();
            Repaint();
        }

        private void ReloadCurrentKeyData()
        {
            _isDirty = false;
            _cachedKeyData = null;
            ClearRawJsonState();

            if (string.IsNullOrEmpty(_selectedFilePath) || string.IsNullOrEmpty(_selectedKey))
                return;

            try
            {
                _cachedKeyData = ES3.Load(_selectedKey, _selectedFilePath);
            }
            catch (Exception)
            {
                LoadKeyAsRawJson();
            }
        }

        private void LoadKeyAsRawJson()
        {
            _isRawJsonMode = true;

            try
            {
                _fullFileRawJson = ES3.LoadRawString(_selectedFilePath);
                _cachedRawJson = ExtractKeyRawJson(_fullFileRawJson, _selectedKey);

                if (_cachedRawJson == null)
                {
                    Debug.LogError($"[SaveFileViewer] Could not extract raw JSON for key '{_selectedKey}'");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveFileViewer] Failed to load raw JSON from {_selectedFilePath}: {e.Message}");
                _cachedRawJson = null;
            }
        }

        private void ClearRawJsonState()
        {
            _isRawJsonMode = false;
            _cachedRawJson = null;
            _fullFileRawJson = null;
        }

        private void SaveCurrentKeyData()
        {
            if (string.IsNullOrEmpty(_selectedFilePath) || string.IsNullOrEmpty(_selectedKey))
                return;

            try
            {
                if (_isRawJsonMode)
                {
                    SaveRawJsonKeyData();
                }
                else if (_cachedKeyData != null)
                {
                    ES3.Save(_selectedKey, _cachedKeyData, _selectedFilePath);
                }

                _isDirty = false;
                Debug.Log($"[SaveFileViewer] Saved key '{_selectedKey}' to {_selectedFilePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveFileViewer] Failed to save key '{_selectedKey}': {e.Message}");
            }
        }

        private void SaveRawJsonKeyData()
        {
            if (string.IsNullOrEmpty(_fullFileRawJson) || string.IsNullOrEmpty(_cachedRawJson))
                return;

            string updatedJson = ReplaceKeyRawJson(_fullFileRawJson, _selectedKey, _cachedRawJson);

            if (updatedJson == null)
            {
                Debug.LogError($"[SaveFileViewer] Failed to replace raw JSON for key '{_selectedKey}'");
                return;
            }

            ES3.SaveRaw(updatedJson, _selectedFilePath);
            _fullFileRawJson = updatedJson;
        }

        private void DeleteFile(ManagedSaveFileInfo entry)
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Delete Save File",
                $"Are you sure you want to delete '{entry.FileName}'?\nThis cannot be undone.",
                "Delete",
                "Cancel");

            if (!confirmed) return;

            try
            {
                ES3.DeleteFile(entry.FilePath);
                Debug.Log($"[SaveFileViewer] Deleted {entry.FilePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveFileViewer] Failed to delete {entry.FilePath}: {e.Message}");
            }

            RefreshFileList();
        }

        private void DeleteKey(string key)
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Delete Key",
                $"Are you sure you want to delete key '{key}' from this save file?\nThis cannot be undone.",
                "Delete",
                "Cancel");

            if (!confirmed) return;

            try
            {
                ES3.DeleteKey(key, _selectedFilePath);
                Debug.Log($"[SaveFileViewer] Deleted key '{key}' from {_selectedFilePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveFileViewer] Failed to delete key '{key}': {e.Message}");
            }

            if (_selectedKey == key)
            {
                _selectedKey = null;
                _cachedKeyData = null;
                _isDirty = false;
            }

            SelectFile(_selectedFilePath);
        }

        private void AddNewKey(string keyName)
        {
            if (ES3.KeyExists(keyName, _selectedFilePath))
            {
                EditorUtility.DisplayDialog(
                    "Key Already Exists",
                    $"The key '{keyName}' already exists in this save file.",
                    "OK");
                return;
            }

            try
            {
                // Save an empty string as a default placeholder value
                ES3.Save(keyName, string.Empty, _selectedFilePath);
                _newKeyName = string.Empty;
                Debug.Log($"[SaveFileViewer] Added key '{keyName}' to {_selectedFilePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveFileViewer] Failed to add key '{keyName}': {e.Message}");
            }

            SelectFile(_selectedFilePath);
        }

        private bool ConfirmDiscardChanges()
        {
            return EditorUtility.DisplayDialog(
                "Unsaved Changes",
                "You have unsaved changes. Discard them?",
                "Discard",
                "Cancel");
        }

        #endregion
        #region Raw JSON Helpers

        private static string ExtractKeyRawJson(string fullJson, string key)
        {
            int valueStart = FindKeyValueStart(fullJson, key);
            if (valueStart < 0)
                return null;

            int valueEnd = FindBalancedBraceEnd(fullJson, valueStart);
            if (valueEnd < 0)
                return null;

            return fullJson.Substring(valueStart, valueEnd - valueStart + 1);
        }

        private static string ReplaceKeyRawJson(string fullJson, string key, string newValue)
        {
            int valueStart = FindKeyValueStart(fullJson, key);
            if (valueStart < 0)
                return null;

            int valueEnd = FindBalancedBraceEnd(fullJson, valueStart);
            if (valueEnd < 0)
                return null;

            return fullJson.Substring(0, valueStart) + newValue + fullJson.Substring(valueEnd + 1);
        }

        private static int FindKeyValueStart(string json, string key)
        {
            string escapedKey = key.Replace("\\", "\\\\").Replace("\"", "\\\"");
            string searchToken = $"\"{escapedKey}\"";

            int keyPos = json.IndexOf(searchToken, StringComparison.Ordinal);
            if (keyPos < 0)
                return -1;

            int colonPos = json.IndexOf(':', keyPos + searchToken.Length);
            if (colonPos < 0)
                return -1;

            int braceStart = json.IndexOf('{', colonPos + 1);
            return braceStart;
        }

        private static int FindBalancedBraceEnd(string json, int openBraceIndex)
        {
            int depth = 0;
            bool inString = false;
            bool escaped = false;

            for (int i = openBraceIndex; i < json.Length; i++)
            {
                char c = json[i];

                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (c == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (c == '"')
                {
                    inString = !inString;
                    continue;
                }

                if (inString)
                    continue;

                if (c == '{')
                    depth++;
                else if (c == '}')
                    depth--;

                if (depth == 0)
                    return i;
            }

            return -1;
        }

        #endregion

        #region GUI Helpers

        private static GUIStyle CreateSelectedStyle()
        {
            GUIStyle style = new(EditorStyles.helpBox);
            Color highlight = EditorGUIUtility.isProSkin
                ? new Color(0.24f, 0.37f, 0.59f, 1f)
                : new Color(0.58f, 0.75f, 1f, 1f);
            Texture2D tex = MakeSolidTexture(highlight);
            style.normal.background = tex;
            return style;
        }

        private static GUIStyle CreateSelectedButtonStyle()
        {
            GUIStyle style = new(GUI.skin.button);
            Color highlight = EditorGUIUtility.isProSkin
                ? new Color(0.24f, 0.37f, 0.59f, 1f)
                : new Color(0.58f, 0.75f, 1f, 1f);
            Texture2D tex = MakeSolidTexture(highlight);
            style.normal.background = tex;
            style.fontStyle = FontStyle.Bold;
            return style;
        }

        private static Texture2D MakeSolidTexture(Color color)
        {
            Texture2D tex = new(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        #endregion
    }
}