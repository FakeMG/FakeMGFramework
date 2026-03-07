using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FakeMG.Framework;
using FakeMG.SaveLoad.Advanced;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace FakeMG.SaveLoad.Editor
{
    public sealed class SaveFileViewerWindow : EditorWindow
    {
        private enum DataViewMode
        {
            Typed,
            KeyRaw,
            FileRaw
        }

        private const int MAX_TYPE_MATCHES = 200;
        private const string NEW_KEY_PREVIEW_PATH = "new-key-preview";
        private const string KEY_RAW_EDITOR_CONTROL_NAME = "SaveFileViewer.KeyRawJson";
        private const string FILE_RAW_EDITOR_CONTROL_NAME = "SaveFileViewer.FileRawJson";

        private static readonly List<Type> CREATABLE_NEW_KEY_TYPES = BuildCreatableNewKeyTypes();

        private static GUIStyle s_selectedEntryStyle;
        private static GUIStyle s_selectedButtonStyle;
        private static Texture2D s_selectedBackgroundTexture;
        private static bool? s_isProSkin;

        private const float LEFT_PANEL_MIN_WIDTH = 220f;
        private const float LEFT_PANEL_MAX_WIDTH = 500f;
        private const float SPLITTER_WIDTH = 4f;

        private float _leftPanelWidth = 280f;
        private bool _isResizingSplitter;

        private List<ManagedSaveFileInfo> _fileEntries = new();
        private string _selectedFilePath;
        private string[] _keys = Array.Empty<string>();
        private readonly HashSet<string> _keyLookup = new(StringComparer.Ordinal);
        private string _selectedKey;
        private object _cachedKeyData;
        private bool _isDirty;

        private DataViewMode _currentDataViewMode = DataViewMode.Typed;
        private bool _isTypedViewAvailable;
        private string _cachedKeyRawJson;
        private string _cachedFullFileRawJson;
        private string _rawValidationErrorMessage;

        private Vector2 _fileListScroll;
        private Vector2 _keyListScroll;
        private Vector2 _dataEditorScroll;

        private string _newKeyName = string.Empty;
        private string _newKeyTypeSearch = typeof(string).FullName;
        private int _newKeyTypeMatchIndex;
        private string[] _newKeyTypeMatchLabels = Array.Empty<string>();
        private List<Type> _newKeyTypeMatches = new();
        private Type _exactNewKeyTypeMatch;
        private Type _selectedNewKeyType = typeof(string);
        private object _newKeyInitialValue;
        private bool _isNewKeyInitialValueExpanded;

        [MenuItem(FakeMGEditorMenus.SAVE_FILE_VIEWER)]
        private static void ShowWindow()
        {
            var window = GetWindow<SaveFileViewerWindow>("Save File Viewer");
            window.minSize = new Vector2(700, 400);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshNewKeyTypeMatches();
            ResetNewKeyInitialValue();
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
                ? GetSelectedEntryStyle()
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
                ? GetSelectedButtonStyle()
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
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Add Key", EditorStyles.boldLabel);

            _newKeyName = EditorGUILayout.TextField("Key", _newKeyName);
            DrawNewKeyTypeSelector();

            if (_selectedNewKeyType == null)
            {
                EditorGUILayout.HelpBox(
                    "Enter an exact runtime type name or search for a creatable type to initialize the new key.",
                    MessageType.Info);
            }
            else
            {
                _isNewKeyInitialValueExpanded = EditorGUILayout.Foldout(
                    _isNewKeyInitialValueExpanded,
                    "Initial Value",
                    true);

                if (_isNewKeyInitialValueExpanded)
                {
                    EditorGUI.indentLevel++;
                    ReflectionDataDrawer.DrawRootValue(_selectedNewKeyType, ref _newKeyInitialValue, NEW_KEY_PREVIEW_PATH);
                    EditorGUI.indentLevel--;
                }
            }

            string trimmedKeyName = _newKeyName.Trim();
            bool isReservedKey = trimmedKeyName == SaveFileCatalog.METADATA_KEY;
            bool keyExists = !string.IsNullOrWhiteSpace(trimmedKeyName) && _keyLookup.Contains(trimmedKeyName);

            if (isReservedKey)
            {
                EditorGUILayout.HelpBox($"'{SaveFileCatalog.METADATA_KEY}' is reserved for save metadata.", MessageType.Warning);
            }
            else if (keyExists)
            {
                EditorGUILayout.HelpBox($"The key '{trimmedKeyName}' already exists in this save file.", MessageType.Warning);
            }

            bool canAddKey = !string.IsNullOrWhiteSpace(trimmedKeyName)
                && !isReservedKey
                && !keyExists
                && _selectedNewKeyType != null;
            EditorGUI.BeginDisabledGroup(!canAddKey);
            if (GUILayout.Button("Add Key"))
            {
                AddNewKey(trimmedKeyName, _newKeyInitialValue);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();
        }

        private void DrawDataEditor()
        {
            EditorGUILayout.LabelField(GetDataEditorTitle(), EditorStyles.boldLabel);
            DrawDataEditorToolbar();

            if (!CanRenderCurrentDataView())
            {
                EditorGUILayout.HelpBox("Select a key to view and edit its data.", MessageType.Info);
                return;
            }

            _dataEditorScroll = EditorGUILayout.BeginScrollView(_dataEditorScroll);

            if (_currentDataViewMode == DataViewMode.KeyRaw)
            {
                DrawKeyRawJsonEditor();
            }
            else if (_currentDataViewMode == DataViewMode.FileRaw)
            {
                DrawFullFileRawJsonEditor();
            }
            else if (_cachedKeyData == null)
            {
                EditorGUILayout.HelpBox("Data is null or could not be loaded.", MessageType.Warning);
            }
            else
            {
                Type valueType = _cachedKeyData.GetType();
                bool modified = ReflectionDataDrawer.DrawRootValue(valueType, ref _cachedKeyData, $"root.{_selectedKey}");
                if (modified)
                {
                    _isDirty = true;
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawKeyRawJsonEditor()
        {
            string message = _isTypedViewAvailable
                ? "Editing the selected key entry as raw JSON. Update the property name to rename the key. Changes are validated before saving."
                : "Typed editing is not available for this key. Editing the selected key entry as raw JSON instead.";
            MessageType messageType = _isTypedViewAvailable
                ? MessageType.Info
                : MessageType.Warning;

            DrawRawJsonEditor(
                _cachedKeyRawJson,
                message,
                messageType,
                KEY_RAW_EDITOR_CONTROL_NAME,
                UpdateCachedKeyRawJson);
        }

        private void DrawFullFileRawJsonEditor()
        {
            string message = "Editing the entire save file as raw JSON. Changes are validated before saving.";
            DrawRawJsonEditor(
                _cachedFullFileRawJson,
                message,
                MessageType.Warning,
                FILE_RAW_EDITOR_CONTROL_NAME,
                UpdateCachedFullFileRawJson);
        }

        private void DrawRawJsonEditor(
            string rawJson,
            string helpMessage,
            MessageType helpMessageType,
            string controlName,
            Action<string> applyEditedJson)
        {
            EditorGUILayout.HelpBox(helpMessage, helpMessageType);

            if (!string.IsNullOrEmpty(_rawValidationErrorMessage))
            {
                EditorGUILayout.HelpBox(_rawValidationErrorMessage, MessageType.Error);
            }

            if (rawJson == null)
            {
                EditorGUILayout.HelpBox("Could not load raw JSON for the current selection.", MessageType.Error);
                return;
            }

            GUI.SetNextControlName(controlName);
            EditorGUI.BeginChangeCheck();
            string editedJson = EditorGUILayout.TextArea(rawJson, GUILayout.ExpandHeight(true));
            if (EditorGUI.EndChangeCheck())
            {
                applyEditedJson(editedJson);
                _isDirty = true;
                _rawValidationErrorMessage = null;
            }
        }

        private void DrawDataEditorToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            EditorGUI.BeginDisabledGroup(!_isDirty || !CanSaveCurrentView());
            if (GUILayout.Button("Save Changes", EditorStyles.toolbarButton))
            {
                SaveCurrentKeyData();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!CanReloadCurrentView());
            if (GUILayout.Button("Revert", EditorStyles.toolbarButton))
            {
                ReloadCurrentDataView();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(8f);
            DrawDataViewButton("Typed", DataViewMode.Typed, !string.IsNullOrEmpty(_selectedKey) && _isTypedViewAvailable);
            DrawDataViewButton("Key Raw", DataViewMode.KeyRaw, !string.IsNullOrEmpty(_selectedKey));
            DrawDataViewButton("File Raw", DataViewMode.FileRaw, !string.IsNullOrEmpty(_selectedFilePath));

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
            _keyLookup.Clear();
            _isDirty = false;
            _currentDataViewMode = DataViewMode.Typed;
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

            ResetRawJsonEditorFocus();
            _selectedFilePath = filePath;
            _selectedKey = null;
            _cachedKeyData = null;
            _isDirty = false;
            ClearRawJsonState();
            ReflectionDataDrawer.ClearExpandedState();

            try
            {
                _keys = ES3.GetKeys(filePath);
                RebuildKeyLookup();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveFileViewer] Failed to load keys from {filePath}: {e.Message}");
                _keys = Array.Empty<string>();
                _keyLookup.Clear();
            }

            ReloadCurrentDataView();
            Repaint();
        }

        private void SelectKey(string key)
        {
            if (_isDirty && !ConfirmDiscardChanges())
                return;

            ResetRawJsonEditorFocus();
            _selectedKey = key;
            ReflectionDataDrawer.ClearExpandedState();
            ReloadCurrentDataView();
            Repaint();
        }

        private void ReloadCurrentDataView()
        {
            _isDirty = false;
            _cachedKeyData = null;
            ClearRawJsonState();
            _isTypedViewAvailable = false;

            if (string.IsNullOrEmpty(_selectedFilePath))
                return;

            if (!string.IsNullOrEmpty(_selectedKey))
            {
                _isTypedViewAvailable = TryLoadTypedKeyData(out object typedKeyData);
                if (_currentDataViewMode == DataViewMode.Typed && _isTypedViewAvailable)
                {
                    _cachedKeyData = typedKeyData;
                    return;
                }
            }

            if (_currentDataViewMode == DataViewMode.Typed)
            {
                if (string.IsNullOrEmpty(_selectedKey))
                    return;

                _currentDataViewMode = DataViewMode.KeyRaw;
            }

            if (_currentDataViewMode == DataViewMode.KeyRaw)
            {
                if (string.IsNullOrEmpty(_selectedKey))
                    return;

                LoadKeyAsRawJson();
                return;
            }

            LoadFullFileAsRawJson();
        }

        private void LoadKeyAsRawJson()
        {
            if (!TryLoadFullFileJson(out JObject rootObject, out string errorMessage))
            {
                _cachedKeyRawJson = null;
                _rawValidationErrorMessage = errorMessage;
                Debug.LogError($"[SaveFileViewer] {errorMessage}");
                return;
            }

            _cachedFullFileRawJson = rootObject.ToString(Formatting.Indented);

            JProperty property = rootObject
                .Properties()
                .FirstOrDefault(candidate => string.Equals(candidate.Name, _selectedKey, StringComparison.Ordinal));

            if (property == null)
            {
                _cachedKeyRawJson = null;
                _rawValidationErrorMessage = $"Could not locate raw JSON for key '{_selectedKey}'.";
                Debug.LogError($"[SaveFileViewer] {_rawValidationErrorMessage}");
                return;
            }

            _cachedKeyRawJson = CreateSingleKeyRawJson(property);
        }

        private void LoadFullFileAsRawJson()
        {
            if (!TryLoadFullFileJson(out JObject rootObject, out string errorMessage))
            {
                _cachedFullFileRawJson = null;
                _rawValidationErrorMessage = errorMessage;
                Debug.LogError($"[SaveFileViewer] {errorMessage}");
                return;
            }

            _cachedFullFileRawJson = rootObject.ToString(Formatting.Indented);

            if (!string.IsNullOrEmpty(_selectedKey))
            {
                JProperty property = rootObject
                    .Properties()
                    .FirstOrDefault(candidate => string.Equals(candidate.Name, _selectedKey, StringComparison.Ordinal));
                _cachedKeyRawJson = property == null ? null : CreateSingleKeyRawJson(property);
            }
        }

        private void ClearRawJsonState()
        {
            _cachedKeyRawJson = null;
            _cachedFullFileRawJson = null;
            _rawValidationErrorMessage = null;
        }

        private void SaveCurrentKeyData()
        {
            if (string.IsNullOrEmpty(_selectedFilePath))
                return;

            try
            {
                switch (_currentDataViewMode)
                {
                    case DataViewMode.Typed:
                        if (_cachedKeyData == null || string.IsNullOrEmpty(_selectedKey))
                            return;

                        ES3.Save(_selectedKey, _cachedKeyData, _selectedFilePath);
                        break;
                    case DataViewMode.KeyRaw:
                        if (string.IsNullOrEmpty(_selectedKey))
                            return;

                        SaveRawJsonKeyData();
                        break;
                    case DataViewMode.FileRaw:
                        SaveFullFileRawJson();
                        break;
                }

                _isDirty = false;
                Debug.Log($"[SaveFileViewer] Saved {_currentDataViewMode} changes to {_selectedFilePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveFileViewer] Failed to save changes to {_selectedFilePath}: {e.Message}");
            }
        }

        private void SaveRawJsonKeyData()
        {
            if (!TryParseSingleKeyRawJson(_cachedKeyRawJson, out string updatedKeyName, out JToken rawToken, out string errorMessage))
            {
                _rawValidationErrorMessage = errorMessage;
                Debug.LogError($"[SaveFileViewer] {errorMessage}");
                return;
            }

            if (!TryLoadFullFileJson(out JObject rootObject, out errorMessage))
            {
                _rawValidationErrorMessage = errorMessage;
                Debug.LogError($"[SaveFileViewer] {errorMessage}");
                return;
            }

            if (string.IsNullOrWhiteSpace(updatedKeyName))
            {
                _rawValidationErrorMessage = "The edited key name cannot be empty.";
                Debug.LogError($"[SaveFileViewer] {_rawValidationErrorMessage}");
                return;
            }

            if (_selectedKey == SaveFileCatalog.METADATA_KEY && !string.Equals(updatedKeyName, _selectedKey, StringComparison.Ordinal))
            {
                _rawValidationErrorMessage = $"'{SaveFileCatalog.METADATA_KEY}' is reserved and cannot be renamed.";
                Debug.LogError($"[SaveFileViewer] {_rawValidationErrorMessage}");
                return;
            }

            if (!string.Equals(updatedKeyName, _selectedKey, StringComparison.Ordinal)
                && string.Equals(updatedKeyName, SaveFileCatalog.METADATA_KEY, StringComparison.Ordinal))
            {
                _rawValidationErrorMessage = $"'{SaveFileCatalog.METADATA_KEY}' is reserved for save metadata.";
                Debug.LogError($"[SaveFileViewer] {_rawValidationErrorMessage}");
                return;
            }

            if (!string.Equals(updatedKeyName, _selectedKey, StringComparison.Ordinal) && rootObject.Property(updatedKeyName) != null)
            {
                _rawValidationErrorMessage = $"The key '{updatedKeyName}' already exists in this save file.";
                Debug.LogError($"[SaveFileViewer] {_rawValidationErrorMessage}");
                return;
            }

            if (!TryUpdateKeyPropertyPreservingOrder(rootObject, _selectedKey, updatedKeyName, rawToken, out errorMessage))
            {
                _rawValidationErrorMessage = errorMessage;
                Debug.LogError($"[SaveFileViewer] {errorMessage}");
                return;
            }

            _selectedKey = updatedKeyName;
            PersistRawFile(rootObject);
            _cachedKeyRawJson = CreateSingleKeyRawJson(_selectedKey, rawToken);
        }

        private void SaveFullFileRawJson()
        {
            if (!TryParseFullFileJson(_cachedFullFileRawJson, out JObject rootObject, out string errorMessage))
            {
                _rawValidationErrorMessage = errorMessage;
                Debug.LogError($"[SaveFileViewer] {errorMessage}");
                return;
            }

            PersistRawFile(rootObject);
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
            if (keyName == SaveFileCatalog.METADATA_KEY)
            {
                EditorUtility.DisplayDialog(
                    "Reserved Key",
                    $"'{SaveFileCatalog.METADATA_KEY}' is reserved for save metadata.",
                    "OK");
                return;
            }

            if (_keyLookup.Contains(keyName) || ES3.KeyExists(keyName, _selectedFilePath))
            {
                EditorUtility.DisplayDialog(
                    "Key Already Exists",
                    $"The key '{keyName}' already exists in this save file.",
                    "OK");
                return;
            }

            try
            {
                ES3.Save(keyName, _newKeyInitialValue, _selectedFilePath);
                _newKeyName = string.Empty;
                ResetNewKeyInitialValue();
                Debug.Log($"[SaveFileViewer] Added key '{keyName}' to {_selectedFilePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveFileViewer] Failed to add key '{keyName}': {e.Message}");
            }

            SelectFile(_selectedFilePath);
            SelectKey(keyName);
        }

        private void AddNewKey(string keyName, object value)
        {
            _newKeyInitialValue = value;
            AddNewKey(keyName);
        }

        private void ResetNewKeyInitialValue()
        {
            if (_selectedNewKeyType == null)
            {
                _newKeyInitialValue = null;
                return;
            }

            _newKeyInitialValue = ReflectionDataDrawer.CreateDefaultValue(_selectedNewKeyType);
        }

        private void DrawNewKeyTypeSelector()
        {
            EditorGUI.BeginChangeCheck();
            _newKeyTypeSearch = EditorGUILayout.TextField("Type", _newKeyTypeSearch);
            if (EditorGUI.EndChangeCheck())
            {
                RefreshNewKeyTypeMatches();
            }

            if (_exactNewKeyTypeMatch != null)
            {
                EditorGUILayout.LabelField("Resolved Type", GetTypeDisplayName(_exactNewKeyTypeMatch));
                return;
            }

            if (_newKeyTypeMatches.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No creatable runtime types match the current search. Use a full type name such as FakeMG.SaveLoad.TestData or narrow the search.",
                    MessageType.Warning);
                return;
            }

            _newKeyTypeMatchIndex = Mathf.Clamp(_newKeyTypeMatchIndex, 0, _newKeyTypeMatches.Count - 1);
            int nextIndex = EditorGUILayout.Popup("Matches", _newKeyTypeMatchIndex, _newKeyTypeMatchLabels);
            if (nextIndex != _newKeyTypeMatchIndex)
            {
                _newKeyTypeMatchIndex = nextIndex;
                ApplySelectedNewKeyType(_newKeyTypeMatches[_newKeyTypeMatchIndex], false);
            }

            EditorGUILayout.LabelField("Selected Type", GetTypeDisplayName(_selectedNewKeyType));
        }

        private void RefreshNewKeyTypeMatches()
        {
            string search = _newKeyTypeSearch?.Trim() ?? string.Empty;
            _exactNewKeyTypeMatch = ResolveSupportedType(search);
            if (_exactNewKeyTypeMatch != null)
            {
                _newKeyTypeMatches = new List<Type> { _exactNewKeyTypeMatch };
                _newKeyTypeMatchLabels = new[] { GetTypeDisplayName(_exactNewKeyTypeMatch) };
                _newKeyTypeMatchIndex = 0;
                ApplySelectedNewKeyType(_exactNewKeyTypeMatch, false);
                return;
            }

            IEnumerable<Type> matches = CREATABLE_NEW_KEY_TYPES;
            if (!string.IsNullOrWhiteSpace(search))
            {
                matches = matches.Where(type =>
                    type.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || type.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            _newKeyTypeMatches = matches
                .Take(MAX_TYPE_MATCHES)
                .ToList();

            _newKeyTypeMatchLabels = _newKeyTypeMatches
                .Select(GetTypeDisplayName)
                .ToArray();

            if (_newKeyTypeMatches.Count == 0)
            {
                _newKeyTypeMatchIndex = 0;
                ApplySelectedNewKeyType(null, false);
                return;
            }

            int selectedIndex = _selectedNewKeyType == null
                ? -1
                : _newKeyTypeMatches.IndexOf(_selectedNewKeyType);
            _newKeyTypeMatchIndex = selectedIndex >= 0 ? selectedIndex : 0;
            ApplySelectedNewKeyType(_newKeyTypeMatches[_newKeyTypeMatchIndex], false);
        }

        private void ApplySelectedNewKeyType(Type nextType, bool updateSearchText)
        {
            if (_selectedNewKeyType == nextType)
                return;

            _selectedNewKeyType = nextType;
            _isNewKeyInitialValueExpanded = false;
            if (updateSearchText && nextType != null)
            {
                _newKeyTypeSearch = nextType.FullName;
                RefreshNewKeyTypeMatches();
            }

            ResetNewKeyInitialValue();
        }

        private static Type ResolveSupportedType(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                return null;

            for (int i = 0; i < CREATABLE_NEW_KEY_TYPES.Count; i++)
            {
                Type candidate = CREATABLE_NEW_KEY_TYPES[i];
                if (string.Equals(candidate.FullName, typeName, StringComparison.Ordinal)
                    || string.Equals(candidate.AssemblyQualifiedName, typeName, StringComparison.Ordinal)
                    || string.Equals(candidate.Name, typeName, StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static string GetTypeDisplayName(Type type)
        {
            return type == null ? string.Empty : $"{type.FullName} ({type.Assembly.GetName().Name})";
        }

        private static List<Type> BuildCreatableNewKeyTypes()
        {
            List<Type> types = new();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Assembly assembly = assemblies[i];
                if (assembly.IsDynamic || IsEditorAssembly(assembly))
                    continue;

                Type[] assemblyTypes = GetLoadableTypes(assembly);
                for (int typeIndex = 0; typeIndex < assemblyTypes.Length; typeIndex++)
                {
                    Type type = assemblyTypes[typeIndex];
                    if (!IsSupportedNewKeyType(type))
                        continue;

                    types.Add(type);
                }
            }

            return types
                .Distinct()
                .OrderBy(type => type.FullName)
                .ToList();
        }

        private static Type[] GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(type => type != null).ToArray();
            }
        }

        private static bool IsEditorAssembly(Assembly assembly)
        {
            string assemblyName = assembly.GetName().Name;
            return assemblyName.Contains("Editor", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSupportedNewKeyType(Type type)
        {
            if (type == null)
                return false;

            if (type.IsAbstract || type.IsInterface)
                return false;

            if (type.ContainsGenericParameters)
                return false;

            if (type.IsPointer || type.IsByRef)
                return false;

            if (typeof(Delegate).IsAssignableFrom(type))
                return false;

            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                return false;

            if (type.FullName == null)
                return false;

            if (type.FullName.StartsWith("<", StringComparison.Ordinal))
                return false;

            return CanCreateDefaultValue(type);
        }

        private static bool CanCreateDefaultValue(Type type)
        {
            if (type == typeof(string))
                return true;

            if (type.IsArray)
                return type.GetElementType() != null;

            if (type.IsValueType)
                return true;

            return type.GetConstructor(Type.EmptyTypes) != null;
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

        private bool TryLoadTypedKeyData(out object typedKeyData)
        {
            typedKeyData = null;

            if (string.IsNullOrEmpty(_selectedFilePath) || string.IsNullOrEmpty(_selectedKey))
                return false;

            try
            {
                typedKeyData = ES3.Load(_selectedKey, _selectedFilePath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool TryLoadFullFileJson(out JObject rootObject, out string errorMessage)
        {
            rootObject = null;

            try
            {
                string rawJson = ES3.LoadRawString(_selectedFilePath);
                return TryParseFullFileJson(rawJson, out rootObject, out errorMessage);
            }
            catch (Exception exception)
            {
                errorMessage = $"Failed to load raw JSON from {_selectedFilePath}: {exception.Message}";
                return false;
            }
        }

        private static bool TryParseFullFileJson(string rawJson, out JObject rootObject, out string errorMessage)
        {
            rootObject = null;

            if (!TryParseJsonToken(rawJson, out JToken rootToken, out errorMessage))
                return false;

            rootObject = rootToken as JObject;
            if (rootObject != null)
                return true;

            errorMessage = "The save file must contain a JSON object at the root.";
            return false;
        }

        private static bool TryParseJsonToken(string rawJson, out JToken parsedToken, out string errorMessage)
        {
            parsedToken = null;

            if (string.IsNullOrWhiteSpace(rawJson))
            {
                errorMessage = "Raw JSON cannot be empty.";
                return false;
            }

            try
            {
                parsedToken = JToken.Parse(rawJson);
                errorMessage = null;
                return true;
            }
            catch (JsonReaderException exception)
            {
                errorMessage = $"Invalid JSON: {exception.Message}";
                return false;
            }
        }

        private static bool TryParseSingleKeyRawJson(
            string rawJson,
            out string keyName,
            out JToken valueToken,
            out string errorMessage)
        {
            keyName = null;
            valueToken = null;

            if (!TryParseFullFileJson(rawJson, out JObject keyObject, out errorMessage))
                return false;

            List<JProperty> properties = keyObject.Properties().ToList();
            if (properties.Count != 1)
            {
                errorMessage = "Key Raw must contain exactly one JSON property.";
                return false;
            }

            JProperty property = properties[0];
            keyName = property.Name;
            valueToken = property.Value?.DeepClone() ?? JValue.CreateNull();
            errorMessage = null;
            return true;
        }

        private static string CreateSingleKeyRawJson(JProperty property)
        {
            return CreateSingleKeyRawJson(property.Name, property.Value);
        }

        private static string CreateSingleKeyRawJson(string keyName, JToken valueToken)
        {
            JObject keyObject = new(new JProperty(keyName, valueToken?.DeepClone() ?? JValue.CreateNull()));
            return keyObject.ToString(Formatting.Indented);
        }

        private static bool TryUpdateKeyPropertyPreservingOrder(
            JObject rootObject,
            string currentKeyName,
            string updatedKeyName,
            JToken rawToken,
            out string errorMessage)
        {
            JProperty existingProperty = rootObject.Property(currentKeyName);
            if (existingProperty == null)
            {
                errorMessage = $"Could not locate key '{currentKeyName}' in the save file.";
                return false;
            }

            JProperty replacementProperty = new(
                updatedKeyName,
                rawToken?.DeepClone() ?? JValue.CreateNull());

            existingProperty.Replace(replacementProperty);
            errorMessage = null;
            return true;
        }

        private void PersistRawFile(JObject rootObject)
        {
            string serializedJson = rootObject.ToString(Formatting.None);
            ES3.SaveRaw(serializedJson, _selectedFilePath);

            _cachedFullFileRawJson = rootObject.ToString(Formatting.Indented);
            _rawValidationErrorMessage = null;

            ReloadKeysFromSelectedFile();
            RefreshSelectedKeyStateFromRoot(rootObject);
        }

        private void ReloadKeysFromSelectedFile()
        {
            try
            {
                _keys = ES3.GetKeys(_selectedFilePath);
                RebuildKeyLookup();
            }
            catch (Exception exception)
            {
                _keys = Array.Empty<string>();
                _keyLookup.Clear();
                Debug.LogError($"[SaveFileViewer] Failed to refresh keys for {_selectedFilePath}: {exception.Message}");
            }
        }

        private void RefreshSelectedKeyStateFromRoot(JObject rootObject)
        {
            if (string.IsNullOrEmpty(_selectedKey))
            {
                _isTypedViewAvailable = false;
                _cachedKeyData = null;
                _cachedKeyRawJson = null;
                return;
            }

            JProperty property = rootObject
                .Properties()
                .FirstOrDefault(candidate => string.Equals(candidate.Name, _selectedKey, StringComparison.Ordinal));

            if (property == null)
            {
                _selectedKey = null;
                _cachedKeyData = null;
                _cachedKeyRawJson = null;
                _isTypedViewAvailable = false;

                if (_currentDataViewMode != DataViewMode.FileRaw)
                {
                    _currentDataViewMode = DataViewMode.FileRaw;
                }

                return;
            }

            _cachedKeyRawJson = CreateSingleKeyRawJson(property);
            _isTypedViewAvailable = TryLoadTypedKeyData(out object typedKeyData);
            _cachedKeyData = _currentDataViewMode == DataViewMode.Typed && _isTypedViewAvailable
                ? typedKeyData
                : null;
        }

        #endregion

        #region GUI Helpers

        private string GetDataEditorTitle()
        {
            if (_currentDataViewMode == DataViewMode.FileRaw)
            {
                return $"Editing File: {Path.GetFileName(_selectedFilePath)}";
            }

            if (string.IsNullOrEmpty(_selectedKey))
            {
                return "Edit Save Data";
            }

            if (_currentDataViewMode == DataViewMode.KeyRaw)
            {
                return $"Editing Raw Key: {_selectedKey}";
            }

            return $"Editing: {_selectedKey}";
        }

        private bool CanRenderCurrentDataView()
        {
            return _currentDataViewMode == DataViewMode.FileRaw || !string.IsNullOrEmpty(_selectedKey);
        }

        private bool CanSaveCurrentView()
        {
            return _currentDataViewMode switch
            {
                DataViewMode.Typed => !string.IsNullOrEmpty(_selectedFilePath) && !string.IsNullOrEmpty(_selectedKey),
                DataViewMode.KeyRaw => !string.IsNullOrEmpty(_selectedFilePath) && !string.IsNullOrEmpty(_selectedKey),
                DataViewMode.FileRaw => !string.IsNullOrEmpty(_selectedFilePath),
                _ => false
            };
        }

        private bool CanReloadCurrentView()
        {
            return _currentDataViewMode switch
            {
                DataViewMode.Typed => !string.IsNullOrEmpty(_selectedFilePath) && !string.IsNullOrEmpty(_selectedKey),
                DataViewMode.KeyRaw => !string.IsNullOrEmpty(_selectedFilePath) && !string.IsNullOrEmpty(_selectedKey),
                DataViewMode.FileRaw => !string.IsNullOrEmpty(_selectedFilePath),
                _ => false
            };
        }

        private void DrawDataViewButton(string label, DataViewMode targetMode, bool isEnabled)
        {
            bool isActive = _currentDataViewMode == targetMode;

            EditorGUI.BeginDisabledGroup(!isEnabled);
            bool nextIsActive = GUILayout.Toggle(isActive, label, EditorStyles.toolbarButton);
            EditorGUI.EndDisabledGroup();

            if (nextIsActive && !isActive)
            {
                SwitchDataViewMode(targetMode);
            }
        }

        private void SwitchDataViewMode(DataViewMode nextMode)
        {
            if (_currentDataViewMode == nextMode)
                return;

            if (_isDirty && !ConfirmDiscardChanges())
                return;

            ResetRawJsonEditorFocus();
            _currentDataViewMode = nextMode;
            ReflectionDataDrawer.ClearExpandedState();
            ReloadCurrentDataView();
            Repaint();
        }

        private void ResetRawJsonEditorFocus()
        {
            EditorGUIUtility.editingTextField = false;
            GUIUtility.keyboardControl = 0;
            GUI.FocusControl(string.Empty);
        }

        private void UpdateCachedKeyRawJson(string editedJson)
        {
            _cachedKeyRawJson = editedJson;
        }

        private void UpdateCachedFullFileRawJson(string editedJson)
        {
            _cachedFullFileRawJson = editedJson;
        }

        private void RebuildKeyLookup()
        {
            _keyLookup.Clear();

            for (int i = 0; i < _keys.Length; i++)
            {
                _keyLookup.Add(_keys[i]);
            }
        }

        private static GUIStyle GetSelectedEntryStyle()
        {
            EnsureSelectedStyles();
            return s_selectedEntryStyle;
        }

        private static GUIStyle GetSelectedButtonStyle()
        {
            EnsureSelectedStyles();
            return s_selectedButtonStyle;
        }

        private static void EnsureSelectedStyles()
        {
            bool isProSkin = EditorGUIUtility.isProSkin;
            if (s_selectedEntryStyle != null
                && s_selectedButtonStyle != null
                && s_selectedBackgroundTexture != null
                && s_isProSkin == isProSkin)
            {
                return;
            }

            if (s_selectedBackgroundTexture)
            {
                DestroyImmediate(s_selectedBackgroundTexture);
            }

            Color highlight = isProSkin
                ? new Color(0.24f, 0.37f, 0.59f, 1f)
                : new Color(0.58f, 0.75f, 1f, 1f);

            s_selectedBackgroundTexture = MakeSolidTexture(highlight);

            s_selectedEntryStyle = new GUIStyle(EditorStyles.helpBox);
            s_selectedEntryStyle.normal.background = s_selectedBackgroundTexture;

            s_selectedButtonStyle = new GUIStyle(GUI.skin.button);
            s_selectedButtonStyle.normal.background = s_selectedBackgroundTexture;
            s_selectedButtonStyle.fontStyle = FontStyle.Bold;
            s_isProSkin = isProSkin;
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