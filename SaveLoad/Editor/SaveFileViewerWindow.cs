using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FakeMG.Framework;
using FakeMG.SaveLoad.Advanced;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using DataViewMode = FakeMG.SaveLoad.Editor.SaveFileViewerDataViewMode;

namespace FakeMG.SaveLoad.Editor
{
    public sealed class SaveFileViewerWindow : EditorWindow
    {
        private const string KEY_RAW_EDITOR_CONTROL_NAME = "SaveFileViewer.KeyRawJson";
        private const string FILE_RAW_EDITOR_CONTROL_NAME = "SaveFileViewer.FileRawJson";

        private readonly SaveFileViewerAddKeyWorkflow _addKeyWorkflow = new();
        private readonly SaveFileViewerDataSession _dataSession = new();
        private static GUIStyle s_selectedButtonStyle;
        private static Texture2D s_selectedBackgroundTexture;
        private static bool? s_isProSkin;

        private const float LEFT_PANEL_MIN_WIDTH = 220f;
        private const float LEFT_PANEL_MAX_WIDTH = 500f;
        private const float SPLITTER_WIDTH = 4f;

        private float _leftPanelWidth = 280f;
        private bool _isResizingSplitter;
        private TreeViewState<int> _fileTreeState;
        private SaveFileViewerFileTreeView _fileTree;

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

        private Vector2 _keyListScroll;
        private Vector2 _dataEditorScroll;

        [MenuItem(FakeMGEditorMenus.SAVE_FILE_VIEWER)]
        private static void ShowWindow()
        {
            var window = GetWindow<SaveFileViewerWindow>("Save File Viewer");
            window.minSize = new Vector2(700, 400);
            window.Show();
        }

        private void OnEnable()
        {
            _addKeyWorkflow.Initialize();
            _fileTreeState ??= new TreeViewState<int>();
            _fileTree ??= new SaveFileViewerFileTreeView(_fileTreeState);
            _fileTree.FileSelected -= HandleFileSelected;
            _fileTree.FileSelected += HandleFileSelected;
            RefreshFileList();
        }

        private void OnDisable()
        {
            if (_fileTree != null)
            {
                _fileTree.FileSelected -= HandleFileSelected;
            }
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

            ManagedSaveFileInfo selectedEntry = GetSelectedFileEntry();
            EditorGUI.BeginDisabledGroup(selectedEntry == null);

            if (GUILayout.Button("Delete Selected", EditorStyles.toolbarButton))
            {
                DeleteFile(selectedEntry);
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawFileListEntries()
        {
            if (_fileEntries.Count == 0)
            {
                EditorGUILayout.HelpBox("No save files found.", MessageType.Info);
                return;
            }

            Rect treeRect = GUILayoutUtility.GetRect(0f, 100000f, 0f, 100000f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            _fileTree.OnGUI(treeRect);
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

            EditorGUILayout.LabelField(SaveFileCatalog.GetRelativeSavePath(_selectedFilePath), EditorStyles.boldLabel);
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
            if (_addKeyWorkflow.Draw(_keyLookup, out SaveFileViewerAddKeyRequest addKeyRequest))
            {
                AddNewKey(addKeyRequest);
            }
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
            bool isMetadataKey = _selectedKey == SaveFileCatalog.METADATA_KEY;
            string message;
            MessageType messageType;

            if (isMetadataKey)
            {
                message = "Editing the metadata entry as raw JSON. Metadata key and field names are locked; only metadata values can be changed.";
                messageType = MessageType.Warning;
            }
            else
            {
                message = _isTypedViewAvailable
                    ? "Editing the selected key entry as raw JSON. Update the property name to rename the key. Changes are validated before saving."
                    : "Typed editing is not available for this key. Editing the selected key entry as raw JSON instead.";
                messageType = _isTypedViewAvailable
                    ? MessageType.Info
                    : MessageType.Warning;
            }

            DrawRawJsonEditor(
                _cachedKeyRawJson,
                message,
                messageType,
                KEY_RAW_EDITOR_CONTROL_NAME,
                UpdateCachedKeyRawJson);
        }

        private void DrawFullFileRawJsonEditor()
        {
            string message = "Editing the entire save file as raw JSON. The metadata entry is required and its field names cannot be added, removed, or renamed.";
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
            ResetViewerSelectionState();

            _fileEntries = SaveFileCatalog.GetManagedSaveFiles();

            _fileEntries = _fileEntries
                .OrderBy(entry => entry.RelativeFolderPath, StringComparer.Ordinal)
                .ThenByDescending(entry => entry.Metadata.Timestamp)
                .ToList();

            _fileTree?.SetEntries(_fileEntries);

            Repaint();
        }

        private void SelectFile(string filePath)
        {
            if (!TryBeginViewTransition())
            {
                return;
            }

            _selectedFilePath = filePath;
            _selectedKey = null;
            ResetLoadedDataState();

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
            _fileTree?.SetSelectedFile(_selectedFilePath);
            Repaint();
        }

        private void HandleFileSelected(string filePath)
        {
            if (_selectedFilePath == filePath)
            {
                return;
            }

            SelectFile(filePath);

            if (_selectedFilePath != filePath)
            {
                _fileTree?.SetSelectedFile(_selectedFilePath);
            }
        }

        private void SelectKey(string key)
        {
            if (!TryBeginViewTransition())
            {
                return;
            }

            _selectedKey = key;
            ResetLoadedDataState();
            ReloadCurrentDataView();
            Repaint();
        }

        private void ReloadCurrentDataView()
        {
            ResetLoadedDataState();

            SaveFileViewerLoadResult loadResult = _dataSession.ReloadCurrentDataView(
                _selectedFilePath,
                _selectedKey,
                _currentDataViewMode);

            _currentDataViewMode = loadResult.CurrentDataViewMode;
            _isTypedViewAvailable = loadResult.IsTypedViewAvailable;
            _cachedKeyData = loadResult.CachedKeyData;
            _cachedKeyRawJson = loadResult.CachedKeyRawJson;
            _cachedFullFileRawJson = loadResult.CachedFullFileRawJson;
            _rawValidationErrorMessage = loadResult.RawValidationErrorMessage;
        }

        private void SaveCurrentKeyData()
        {
            SaveFileViewerSaveResult saveResult = _dataSession.SaveCurrentData(
                _selectedFilePath,
                _selectedKey,
                _currentDataViewMode,
                _cachedKeyData,
                _cachedKeyRawJson,
                _cachedFullFileRawJson);

            _selectedKey = saveResult.SelectedKey;
            _currentDataViewMode = saveResult.CurrentDataViewMode;
            _isTypedViewAvailable = saveResult.IsTypedViewAvailable;
            _cachedKeyData = saveResult.CachedKeyData;
            _cachedKeyRawJson = saveResult.CachedKeyRawJson;
            _cachedFullFileRawJson = saveResult.CachedFullFileRawJson;
            _rawValidationErrorMessage = saveResult.RawValidationErrorMessage;

            if (saveResult.Keys != null)
            {
                _keys = saveResult.Keys;
                RebuildKeyLookup();
            }

            if (!saveResult.Succeeded)
            {
                return;
            }

            _isDirty = false;
        }

        private void DeleteFile(ManagedSaveFileInfo entry)
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Delete Save File",
                $"Are you sure you want to delete '{entry.FileName}'?\nThis cannot be undone.",
                "Delete",
                "Cancel");

            if (!confirmed)
            {
                return;
            }

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
            if (key == SaveFileCatalog.METADATA_KEY)
            {
                EditorUtility.DisplayDialog(
                    "Protected Key",
                    $"'{SaveFileCatalog.METADATA_KEY}' is required metadata and cannot be deleted.",
                    "OK");
                return;
            }

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

        private void AddNewKey(SaveFileViewerAddKeyRequest addKeyRequest)
        {
            string keyName = addKeyRequest.KeyName;

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
                ES3.Save(keyName, addKeyRequest.InitialValue, _selectedFilePath);
                _addKeyWorkflow.CompleteAdd();
                Debug.Log($"[SaveFileViewer] Added key '{keyName}' to {_selectedFilePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveFileViewer] Failed to add key '{keyName}': {e.Message}");
            }

            SelectFile(_selectedFilePath);
            SelectKey(keyName);
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

        private bool HasCurrentViewTarget()
        {
            return _currentDataViewMode switch
            {
                DataViewMode.Typed => !string.IsNullOrEmpty(_selectedFilePath) && !string.IsNullOrEmpty(_selectedKey),
                DataViewMode.KeyRaw => !string.IsNullOrEmpty(_selectedFilePath) && !string.IsNullOrEmpty(_selectedKey),
                DataViewMode.FileRaw => !string.IsNullOrEmpty(_selectedFilePath),
                _ => false
            };
        }

        private bool CanSaveCurrentView()
        {
            return HasCurrentViewTarget();
        }

        private bool CanReloadCurrentView()
        {
            return HasCurrentViewTarget();
        }

        private ManagedSaveFileInfo GetSelectedFileEntry()
        {
            return _fileEntries.FirstOrDefault(entry => entry.FilePath == _selectedFilePath);
        }

        private void ResetViewerSelectionState()
        {
            _selectedFilePath = null;
            _selectedKey = null;
            _keys = Array.Empty<string>();
            _keyLookup.Clear();
            _currentDataViewMode = DataViewMode.Typed;
            _fileTree?.ClearFileSelection();
            ResetLoadedDataState();
            ReflectionDataDrawer.ClearExpandedState();
        }

        private void ResetLoadedDataState()
        {
            _isDirty = false;
            _cachedKeyData = null;
            _isTypedViewAvailable = false;
            _cachedKeyRawJson = null;
            _cachedFullFileRawJson = null;
            _rawValidationErrorMessage = null;
        }

        private bool TryBeginViewTransition()
        {
            if (_isDirty && !ConfirmDiscardChanges())
            {
                return false;
            }

            ResetRawJsonEditorFocus();
            ReflectionDataDrawer.ClearExpandedState();
            return true;
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

            if (!TryBeginViewTransition())
            {
                return;
            }

            _currentDataViewMode = nextMode;
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

        private static GUIStyle GetSelectedButtonStyle()
        {
            EnsureSelectedStyles();
            return s_selectedButtonStyle;
        }

        private static void EnsureSelectedStyles()
        {
            bool isProSkin = EditorGUIUtility.isProSkin;
            if (s_selectedButtonStyle != null
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