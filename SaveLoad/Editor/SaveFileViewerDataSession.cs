using System;
using System.Linq;
using FakeMG.SaveLoad;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace FakeMG.SaveLoad.Editor
{
    internal enum SaveFileViewerDataViewMode
    {
        Typed,
        KeyRaw,
        FileRaw
    }

    internal readonly struct SaveFileViewerLoadResult
    {
        public SaveFileViewerLoadResult(
            SaveFileViewerDataViewMode currentDataViewMode,
            bool isTypedViewAvailable,
            object cachedKeyData,
            string cachedKeyRawJson,
            string cachedFullFileRawJson,
            string rawValidationErrorMessage)
        {
            CurrentDataViewMode = currentDataViewMode;
            IsTypedViewAvailable = isTypedViewAvailable;
            CachedKeyData = cachedKeyData;
            CachedKeyRawJson = cachedKeyRawJson;
            CachedFullFileRawJson = cachedFullFileRawJson;
            RawValidationErrorMessage = rawValidationErrorMessage;
        }

        public SaveFileViewerDataViewMode CurrentDataViewMode { get; }

        public bool IsTypedViewAvailable { get; }

        public object CachedKeyData { get; }

        public string CachedKeyRawJson { get; }

        public string CachedFullFileRawJson { get; }

        public string RawValidationErrorMessage { get; }
    }

    internal readonly struct SaveFileViewerSaveResult
    {
        public SaveFileViewerSaveResult(
            bool succeeded,
            string selectedKey,
            SaveFileViewerDataViewMode currentDataViewMode,
            bool isTypedViewAvailable,
            object cachedKeyData,
            string cachedKeyRawJson,
            string cachedFullFileRawJson,
            string rawValidationErrorMessage,
            string[] keys)
        {
            Succeeded = succeeded;
            SelectedKey = selectedKey;
            CurrentDataViewMode = currentDataViewMode;
            IsTypedViewAvailable = isTypedViewAvailable;
            CachedKeyData = cachedKeyData;
            CachedKeyRawJson = cachedKeyRawJson;
            CachedFullFileRawJson = cachedFullFileRawJson;
            RawValidationErrorMessage = rawValidationErrorMessage;
            Keys = keys;
        }

        public bool Succeeded { get; }

        public string SelectedKey { get; }

        public SaveFileViewerDataViewMode CurrentDataViewMode { get; }

        public bool IsTypedViewAvailable { get; }

        public object CachedKeyData { get; }

        public string CachedKeyRawJson { get; }

        public string CachedFullFileRawJson { get; }

        public string RawValidationErrorMessage { get; }

        public string[] Keys { get; }
    }

    internal sealed class SaveFileViewerDataSession
    {
        private readonly SaveFileViewerMutationService _mutationService = new();

        public SaveFileViewerLoadResult ReloadCurrentDataView(
            string selectedFilePath,
            string selectedKey,
            SaveFileViewerDataViewMode currentDataViewMode,
            SaveFileProtectionSettings protectionSettings)
        {
            SaveFileViewerLoadResult emptyResult = CreateLoadResult(currentDataViewMode);
            if (string.IsNullOrEmpty(selectedFilePath))
            {
                return emptyResult;
            }

            ES3Settings fileSettings = Es3FileSettingsFactory.Create(
                selectedFilePath,
                protectionSettings);

            if (!string.IsNullOrEmpty(selectedKey))
            {
                bool isTypedViewAvailable = TryLoadTypedKeyData(
                    selectedFilePath,
                    selectedKey,
                    fileSettings,
                    out object typedKeyData);
                if (currentDataViewMode == SaveFileViewerDataViewMode.Typed && isTypedViewAvailable)
                {
                    return new SaveFileViewerLoadResult(
                        currentDataViewMode,
                        true,
                        typedKeyData,
                        null,
                        null,
                        null);
                }
            }

            if (currentDataViewMode == SaveFileViewerDataViewMode.Typed)
            {
                if (string.IsNullOrEmpty(selectedKey))
                {
                    return emptyResult;
                }

                currentDataViewMode = SaveFileViewerDataViewMode.KeyRaw;
            }

            if (currentDataViewMode == SaveFileViewerDataViewMode.KeyRaw)
            {
                if (string.IsNullOrEmpty(selectedKey))
                {
                    return CreateLoadResult(currentDataViewMode);
                }

                return LoadKeyAsRawJson(
                    selectedFilePath,
                    selectedKey,
                    currentDataViewMode,
                    fileSettings);
            }

            return LoadFullFileAsRawJson(
                selectedFilePath,
                selectedKey,
                currentDataViewMode,
                fileSettings);
        }

        public SaveFileViewerSaveResult SaveCurrentData(
            string selectedFilePath,
            string selectedKey,
            SaveFileViewerDataViewMode currentDataViewMode,
            object cachedKeyData,
            string cachedKeyRawJson,
            string cachedFullFileRawJson,
            SaveFileProtectionSettings protectionSettings)
        {
            if (string.IsNullOrEmpty(selectedFilePath))
            {
                return CreateFailedSaveResult(
                    selectedKey,
                    currentDataViewMode,
                    cachedKeyData,
                    cachedKeyRawJson,
                    cachedFullFileRawJson,
                    null);
            }

            ES3Settings fileSettings = Es3FileSettingsFactory.Create(
                selectedFilePath,
                protectionSettings);

            try
            {
                SaveFileViewerSaveResult result = currentDataViewMode switch
                {
                    SaveFileViewerDataViewMode.Typed => SaveTypedKeyData(
                        selectedFilePath,
                        selectedKey,
                        cachedKeyData,
                        fileSettings,
                        protectionSettings),
                    SaveFileViewerDataViewMode.KeyRaw => SaveRawJsonKeyData(
                        selectedFilePath,
                        selectedKey,
                        currentDataViewMode,
                        cachedKeyRawJson,
                        fileSettings,
                        protectionSettings),
                    SaveFileViewerDataViewMode.FileRaw => SaveFullFileRawJson(
                        selectedFilePath,
                        selectedKey,
                        currentDataViewMode,
                        cachedFullFileRawJson,
                        fileSettings,
                        protectionSettings),
                    _ => CreateFailedSaveResult(selectedKey, currentDataViewMode, cachedKeyData, cachedKeyRawJson, cachedFullFileRawJson, null)
                };

                if (result.Succeeded)
                {
                    Debug.Log($"[SaveFileViewer] Saved {currentDataViewMode} changes to {selectedFilePath}");
                }

                return result;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SaveFileViewer] Failed to save changes to {selectedFilePath}: {exception}");
                return CreateFailedSaveResult(
                    selectedKey,
                    currentDataViewMode,
                    cachedKeyData,
                    cachedKeyRawJson,
                    cachedFullFileRawJson,
                    null);
            }
        }

        private static SaveFileViewerLoadResult LoadKeyAsRawJson(
            string selectedFilePath,
            string selectedKey,
            SaveFileViewerDataViewMode currentDataViewMode,
            ES3Settings fileSettings)
        {
            if (!TryLoadFullFileJson(selectedFilePath, fileSettings, out JObject rootObject, out string errorMessage))
            {
                Debug.LogError($"[SaveFileViewer] {errorMessage}");
                return new SaveFileViewerLoadResult(
                    currentDataViewMode,
                    false,
                    null,
                    null,
                    null,
                    errorMessage);
            }

            JProperty property = rootObject
                .Properties()
                .FirstOrDefault(candidate => string.Equals(candidate.Name, selectedKey, StringComparison.Ordinal));

            if (property == null)
            {
                string missingKeyMessage = $"Could not locate raw JSON for key '{selectedKey}'.";
                Debug.LogError($"[SaveFileViewer] {missingKeyMessage}");
                return new SaveFileViewerLoadResult(
                    currentDataViewMode,
                    false,
                    null,
                    null,
                    rootObject.ToString(Formatting.Indented),
                    missingKeyMessage);
            }

            return new SaveFileViewerLoadResult(
                currentDataViewMode,
                false,
                null,
                SaveFileViewerRawJsonPolicy.CreateSingleKeyRawJson(property),
                rootObject.ToString(Formatting.Indented),
                null);
        }

        private static SaveFileViewerLoadResult LoadFullFileAsRawJson(
            string selectedFilePath,
            string selectedKey,
            SaveFileViewerDataViewMode currentDataViewMode,
            ES3Settings fileSettings)
        {
            if (!TryLoadFullFileJson(selectedFilePath, fileSettings, out JObject rootObject, out string errorMessage))
            {
                Debug.LogError($"[SaveFileViewer] {errorMessage}");
                return new SaveFileViewerLoadResult(
                    currentDataViewMode,
                    false,
                    null,
                    null,
                    null,
                    errorMessage);
            }

            string cachedKeyRawJson = null;
            if (!string.IsNullOrEmpty(selectedKey))
            {
                JProperty property = rootObject
                    .Properties()
                    .FirstOrDefault(candidate => string.Equals(candidate.Name, selectedKey, StringComparison.Ordinal));
                cachedKeyRawJson = property == null
                    ? null
                    : SaveFileViewerRawJsonPolicy.CreateSingleKeyRawJson(property);
            }

            return new SaveFileViewerLoadResult(
                currentDataViewMode,
                false,
                null,
                cachedKeyRawJson,
                rootObject.ToString(Formatting.Indented),
                null);
        }

        private SaveFileViewerSaveResult SaveTypedKeyData(
            string selectedFilePath,
            string selectedKey,
            object cachedKeyData,
            ES3Settings fileSettings,
            SaveFileProtectionSettings protectionSettings)
        {
            if (cachedKeyData == null || string.IsNullOrEmpty(selectedKey))
            {
                return CreateFailedSaveResult(
                    selectedKey,
                    SaveFileViewerDataViewMode.Typed,
                    cachedKeyData,
                    null,
                    null,
                    null);
            }

            _mutationService.SaveKey(
                selectedFilePath,
                selectedKey,
                cachedKeyData,
                protectionSettings);

            return new SaveFileViewerSaveResult(
                true,
                selectedKey,
                SaveFileViewerDataViewMode.Typed,
                true,
                cachedKeyData,
                null,
                null,
                null,
                null);
        }

        private SaveFileViewerSaveResult SaveRawJsonKeyData(
            string selectedFilePath,
            string selectedKey,
            SaveFileViewerDataViewMode currentDataViewMode,
            string cachedKeyRawJson,
            ES3Settings fileSettings,
            SaveFileProtectionSettings protectionSettings)
        {
            if (string.IsNullOrEmpty(selectedKey))
            {
                return CreateFailedSaveResult(selectedKey, currentDataViewMode, null, cachedKeyRawJson, null, null);
            }

            if (!SaveFileViewerRawJsonPolicy.TryParseSingleKeyRawJson(cachedKeyRawJson, out string updatedKeyName, out JToken rawToken, out string errorMessage))
            {
                Debug.LogError($"[SaveFileViewer] {errorMessage}");
                return CreateFailedSaveResult(selectedKey, currentDataViewMode, null, cachedKeyRawJson, null, errorMessage);
            }

            if (!TryLoadFullFileJson(selectedFilePath, fileSettings, out JObject rootObject, out errorMessage))
            {
                Debug.LogError($"[SaveFileViewer] {errorMessage}");
                return CreateFailedSaveResult(selectedKey, currentDataViewMode, null, cachedKeyRawJson, null, errorMessage);
            }

            if (string.IsNullOrWhiteSpace(updatedKeyName))
            {
                const string EMPTY_KEY_MESSAGE = "The edited key name cannot be empty.";
                Debug.LogError($"[SaveFileViewer] {EMPTY_KEY_MESSAGE}");
                return CreateFailedSaveResult(selectedKey, currentDataViewMode, null, cachedKeyRawJson, null, EMPTY_KEY_MESSAGE);
            }

            if (selectedKey == SaveFileCatalog.METADATA_KEY && !string.Equals(updatedKeyName, selectedKey, StringComparison.Ordinal))
            {
                string reservedMetadataMessage = $"'{SaveFileCatalog.METADATA_KEY}' is reserved and cannot be renamed.";
                Debug.LogError($"[SaveFileViewer] {reservedMetadataMessage}");
                return CreateFailedSaveResult(selectedKey, currentDataViewMode, null, cachedKeyRawJson, null, reservedMetadataMessage);
            }

            if (selectedKey == SaveFileCatalog.METADATA_KEY
                && !SaveFileViewerRawJsonPolicy.TryValidateMetadataToken(rawToken, out errorMessage))
            {
                Debug.LogError($"[SaveFileViewer] {errorMessage}");
                return CreateFailedSaveResult(selectedKey, currentDataViewMode, null, cachedKeyRawJson, null, errorMessage);
            }

            if (!string.Equals(updatedKeyName, selectedKey, StringComparison.Ordinal)
                && string.Equals(updatedKeyName, SaveFileCatalog.METADATA_KEY, StringComparison.Ordinal))
            {
                string metadataReservationMessage = $"'{SaveFileCatalog.METADATA_KEY}' is reserved for save metadata.";
                Debug.LogError($"[SaveFileViewer] {metadataReservationMessage}");
                return CreateFailedSaveResult(selectedKey, currentDataViewMode, null, cachedKeyRawJson, null, metadataReservationMessage);
            }

            if (!string.Equals(updatedKeyName, selectedKey, StringComparison.Ordinal) && rootObject.Property(updatedKeyName) != null)
            {
                string duplicateKeyMessage = $"The key '{updatedKeyName}' already exists in this save file.";
                Debug.LogError($"[SaveFileViewer] {duplicateKeyMessage}");
                return CreateFailedSaveResult(selectedKey, currentDataViewMode, null, cachedKeyRawJson, null, duplicateKeyMessage);
            }

            if (!SaveFileViewerRawJsonPolicy.TryUpdateKeyPropertyPreservingOrder(rootObject, selectedKey, updatedKeyName, rawToken, out errorMessage))
            {
                Debug.LogError($"[SaveFileViewer] {errorMessage}");
                return CreateFailedSaveResult(selectedKey, currentDataViewMode, null, cachedKeyRawJson, null, errorMessage);
            }

            return PersistRawFile(
                selectedFilePath,
                updatedKeyName,
                currentDataViewMode,
                rootObject,
                rawToken,
                fileSettings,
                protectionSettings);
        }

        private SaveFileViewerSaveResult SaveFullFileRawJson(
            string selectedFilePath,
            string selectedKey,
            SaveFileViewerDataViewMode currentDataViewMode,
            string cachedFullFileRawJson,
            ES3Settings fileSettings,
            SaveFileProtectionSettings protectionSettings)
        {
            if (!SaveFileViewerRawJsonPolicy.TryParseFullFileJson(cachedFullFileRawJson, out JObject rootObject, out string errorMessage))
            {
                Debug.LogError($"[SaveFileViewer] {errorMessage}");
                return CreateFailedSaveResult(selectedKey, currentDataViewMode, null, null, cachedFullFileRawJson, errorMessage);
            }

            if (!SaveFileViewerRawJsonPolicy.TryValidateMetadataInRoot(rootObject, out errorMessage))
            {
                Debug.LogError($"[SaveFileViewer] {errorMessage}");
                return CreateFailedSaveResult(selectedKey, currentDataViewMode, null, null, cachedFullFileRawJson, errorMessage);
            }

            return PersistRawFile(
                selectedFilePath,
                selectedKey,
                currentDataViewMode,
                rootObject,
                null,
                fileSettings,
                protectionSettings);
        }

        private SaveFileViewerSaveResult PersistRawFile(
            string selectedFilePath,
            string selectedKey,
            SaveFileViewerDataViewMode currentDataViewMode,
            JObject rootObject,
            JToken updatedRawToken,
            ES3Settings fileSettings,
            SaveFileProtectionSettings protectionSettings)
        {
            string serializedJson = rootObject.ToString(Formatting.None);
            _mutationService.ReplaceRawJson(
                selectedFilePath,
                serializedJson,
                protectionSettings);

            string[] keys = LoadKeysFromSelectedFile(selectedFilePath, fileSettings);
            return RefreshSelectedKeyStateFromRoot(
                selectedFilePath,
                selectedKey,
                currentDataViewMode,
                rootObject,
                keys,
                updatedRawToken,
                fileSettings);
        }

        private static string[] LoadKeysFromSelectedFile(string selectedFilePath, ES3Settings fileSettings)
        {
            try
            {
                return ES3.GetKeys(fileSettings);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SaveFileViewer] Failed to refresh keys for {selectedFilePath}: {exception}");
                return Array.Empty<string>();
            }
        }

        private static SaveFileViewerSaveResult RefreshSelectedKeyStateFromRoot(
            string selectedFilePath,
            string selectedKey,
            SaveFileViewerDataViewMode currentDataViewMode,
            JObject rootObject,
            string[] keys,
            JToken updatedRawToken,
            ES3Settings fileSettings)
        {
            string cachedFullFileRawJson = rootObject.ToString(Formatting.Indented);

            if (string.IsNullOrEmpty(selectedKey))
            {
                return new SaveFileViewerSaveResult(
                    true,
                    null,
                    currentDataViewMode,
                    false,
                    null,
                    null,
                    cachedFullFileRawJson,
                    null,
                    keys);
            }

            JProperty property = rootObject
                .Properties()
                .FirstOrDefault(candidate => string.Equals(candidate.Name, selectedKey, StringComparison.Ordinal));

            if (property == null)
            {
                SaveFileViewerDataViewMode nextMode = currentDataViewMode == SaveFileViewerDataViewMode.FileRaw
                    ? currentDataViewMode
                    : SaveFileViewerDataViewMode.FileRaw;

                return new SaveFileViewerSaveResult(
                    true,
                    null,
                    nextMode,
                    false,
                    null,
                    null,
                    cachedFullFileRawJson,
                    null,
                    keys);
            }

            string cachedKeyRawJson = updatedRawToken == null
                ? SaveFileViewerRawJsonPolicy.CreateSingleKeyRawJson(property)
                : SaveFileViewerRawJsonPolicy.CreateSingleKeyRawJson(selectedKey, updatedRawToken);

            bool isTypedViewAvailable = TryLoadTypedKeyData(selectedFilePath, selectedKey, fileSettings, out object typedKeyData);
            object cachedKeyData = currentDataViewMode == SaveFileViewerDataViewMode.Typed && isTypedViewAvailable
                ? typedKeyData
                : null;

            return new SaveFileViewerSaveResult(
                true,
                selectedKey,
                currentDataViewMode,
                isTypedViewAvailable,
                cachedKeyData,
                cachedKeyRawJson,
                cachedFullFileRawJson,
                null,
                keys);
        }

        private static bool TryLoadTypedKeyData(
            string selectedFilePath,
            string selectedKey,
            ES3Settings fileSettings,
            out object typedKeyData)
        {
            typedKeyData = null;

            if (string.IsNullOrEmpty(selectedFilePath) || string.IsNullOrEmpty(selectedKey))
            {
                return false;
            }

            try
            {
                typedKeyData = ES3.Load(selectedKey, fileSettings);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryLoadFullFileJson(
            string selectedFilePath,
            ES3Settings fileSettings,
            out JObject rootObject,
            out string errorMessage)
        {
            rootObject = null;

            try
            {
                string rawJson = ES3.LoadRawString(fileSettings);
                return SaveFileViewerRawJsonPolicy.TryParseFullFileJson(rawJson, out rootObject, out errorMessage);
            }
            catch (Exception exception)
            {
                errorMessage = $"Failed to load raw JSON from {selectedFilePath}: {exception}";
                return false;
            }
        }

        private static SaveFileViewerLoadResult CreateLoadResult(SaveFileViewerDataViewMode currentDataViewMode)
        {
            return new SaveFileViewerLoadResult(currentDataViewMode, false, null, null, null, null);
        }

        private static SaveFileViewerSaveResult CreateFailedSaveResult(
            string selectedKey,
            SaveFileViewerDataViewMode currentDataViewMode,
            object cachedKeyData,
            string cachedKeyRawJson,
            string cachedFullFileRawJson,
            string rawValidationErrorMessage)
        {
            return new SaveFileViewerSaveResult(
                false,
                selectedKey,
                currentDataViewMode,
                false,
                cachedKeyData,
                cachedKeyRawJson,
                cachedFullFileRawJson,
                rawValidationErrorMessage,
                null);
        }
    }
}
