using System;
using System.Collections.Generic;
using System.Linq;
using FakeMG.SaveLoad.Advanced;
using UnityEditor;
using UnityEngine;

namespace FakeMG.SaveLoad.Editor
{
    internal readonly struct SaveFileViewerAddKeyRequest
    {
        public SaveFileViewerAddKeyRequest(string keyName, object initialValue)
        {
            KeyName = keyName;
            InitialValue = initialValue;
        }

        public string KeyName { get; }

        public object InitialValue { get; }
    }

    internal sealed class SaveFileViewerAddKeyWorkflow
    {
        private const int MAX_TYPE_MATCHES = 200;
        private const string NEW_KEY_PREVIEW_PATH = "new-key-preview";

        private static readonly SaveFileViewerTypeCatalog TypeCatalog = SaveFileViewerTypeCatalog.Default;

        private string _newKeyName = string.Empty;
        private string _newKeyTypeSearch = typeof(string).FullName;
        private int _newKeyTypeMatchIndex;
        private string[] _newKeyTypeMatchLabels = Array.Empty<string>();
        private List<Type> _newKeyTypeMatches = new();
        private Type _exactNewKeyTypeMatch;
        private Type _selectedNewKeyType = typeof(string);
        private object _newKeyInitialValue;
        private bool _isNewKeyInitialValueExpanded;

        public void Initialize()
        {
            RefreshNewKeyTypeMatches();
            ResetNewKeyInitialValue();
        }

        public void CompleteAdd()
        {
            _newKeyName = string.Empty;
            ResetNewKeyInitialValue();
        }

        public bool Draw(HashSet<string> keyLookup, out SaveFileViewerAddKeyRequest addKeyRequest)
        {
            addKeyRequest = default;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Add Key", EditorStyles.boldLabel);

            _newKeyName = EditorGUILayout.TextField("Key", _newKeyName);
            DrawTypeSelector();

            DrawInitialValueEditor();

            string trimmedKeyName = _newKeyName.Trim();
            bool isReservedKey = trimmedKeyName == SaveFileCatalog.METADATA_KEY;
            bool keyExists = !string.IsNullOrWhiteSpace(trimmedKeyName) && keyLookup.Contains(trimmedKeyName);

            DrawValidationMessage(trimmedKeyName, isReservedKey, keyExists);

            bool canAddKey = !string.IsNullOrWhiteSpace(trimmedKeyName)
                && !isReservedKey
                && !keyExists
                && _selectedNewKeyType != null;

            EditorGUI.BeginDisabledGroup(!canAddKey);
            bool shouldAddKey = GUILayout.Button("Add Key");
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();

            if (!shouldAddKey)
            {
                return false;
            }

            addKeyRequest = new SaveFileViewerAddKeyRequest(trimmedKeyName, _newKeyInitialValue);
            return true;
        }

        private void DrawTypeSelector()
        {
            EditorGUI.BeginChangeCheck();
            _newKeyTypeSearch = EditorGUILayout.TextField("Type", _newKeyTypeSearch);
            if (EditorGUI.EndChangeCheck())
            {
                RefreshNewKeyTypeMatches();
            }

            if (_exactNewKeyTypeMatch != null)
            {
                EditorGUILayout.LabelField("Resolved Type", TypeCatalog.GetDisplayName(_exactNewKeyTypeMatch));
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
                ApplySelectedNewKeyType(_newKeyTypeMatches[_newKeyTypeMatchIndex]);
            }

            EditorGUILayout.LabelField("Selected Type", TypeCatalog.GetDisplayName(_selectedNewKeyType));
        }

        private void DrawInitialValueEditor()
        {
            if (_selectedNewKeyType == null)
            {
                EditorGUILayout.HelpBox(
                    "Enter an exact runtime type name or search for a creatable type to initialize the new key.",
                    MessageType.Info);
                return;
            }

            _isNewKeyInitialValueExpanded = EditorGUILayout.Foldout(
                _isNewKeyInitialValueExpanded,
                "Initial Value",
                true);

            if (!_isNewKeyInitialValueExpanded)
            {
                return;
            }

            EditorGUI.indentLevel++;
            ReflectionDataDrawer.DrawRootValue(_selectedNewKeyType, ref _newKeyInitialValue, NEW_KEY_PREVIEW_PATH);
            EditorGUI.indentLevel--;
        }

        private static void DrawValidationMessage(string trimmedKeyName, bool isReservedKey, bool keyExists)
        {
            if (isReservedKey)
            {
                EditorGUILayout.HelpBox($"'{SaveFileCatalog.METADATA_KEY}' is reserved for save metadata.", MessageType.Warning);
                return;
            }

            if (keyExists)
            {
                EditorGUILayout.HelpBox($"The key '{trimmedKeyName}' already exists in this save file.", MessageType.Warning);
            }
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

        private void RefreshNewKeyTypeMatches()
        {
            string search = _newKeyTypeSearch?.Trim() ?? string.Empty;
            _exactNewKeyTypeMatch = TypeCatalog.ResolveSupportedType(search);
            if (_exactNewKeyTypeMatch != null)
            {
                _newKeyTypeMatches = new List<Type> { _exactNewKeyTypeMatch };
                _newKeyTypeMatchLabels = new[] { TypeCatalog.GetDisplayName(_exactNewKeyTypeMatch) };
                _newKeyTypeMatchIndex = 0;
                ApplySelectedNewKeyType(_exactNewKeyTypeMatch);
                return;
            }

            _newKeyTypeMatches = TypeCatalog.GetMatches(search, MAX_TYPE_MATCHES);
            _newKeyTypeMatchLabels = _newKeyTypeMatches
                .Select(TypeCatalog.GetDisplayName)
                .ToArray();

            if (_newKeyTypeMatches.Count == 0)
            {
                _newKeyTypeMatchIndex = 0;
                ApplySelectedNewKeyType(null);
                return;
            }

            int selectedIndex = _selectedNewKeyType == null
                ? -1
                : _newKeyTypeMatches.IndexOf(_selectedNewKeyType);
            _newKeyTypeMatchIndex = selectedIndex >= 0 ? selectedIndex : 0;
            ApplySelectedNewKeyType(_newKeyTypeMatches[_newKeyTypeMatchIndex]);
        }

        private void ApplySelectedNewKeyType(Type nextType)
        {
            if (_selectedNewKeyType == nextType)
            {
                return;
            }

            _selectedNewKeyType = nextType;
            _isNewKeyInitialValueExpanded = false;
            ResetNewKeyInitialValue();
        }
    }
}