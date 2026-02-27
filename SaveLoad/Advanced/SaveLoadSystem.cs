using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.SaveLoad.Advanced
{
    public class SaveLoadSystem : MonoBehaviour
    {
        [SerializeField] private bool _enableAutoSave = true;
        [SerializeField] private int _maxAutoSaves = 5;
        [SerializeField] private float _autoSaveInterval = 300f;
        [SerializeField] private bool _enableDebug = true;

        private readonly Dictionary<string, Saveable> _saveables = new();

        private float _autoSaveTimer;

        private const string SAVE_FOLDER = "Saves/";

        private const string MANUAL_SAVE_KEY = "ManualSave_";
        private const string AUTO_SAVE_KEY = "AutoSave_";
        public const string METADATA_KEY = "Metadata";

        public event Action OnLoadingComplete;

        private void Start()
        {
            if (_enableAutoSave)
            {
                _autoSaveTimer = _autoSaveInterval;
            }

            RefreshSaveables();
            LoadMostRecentSave();
        }

        private void Update()
        {
            if (!_enableAutoSave) return;

            _autoSaveTimer -= Time.deltaTime;
            if (_autoSaveTimer <= 0)
            {
                AutoSaveGame();
                _autoSaveTimer = _autoSaveInterval;
            }
        }

        #region Saveable Registration
        public void RefreshSaveables()
        {
            _saveables.Clear();

            Saveable[] saveables = GetComponentsInChildren<Saveable>(true);

            foreach (var saveable in saveables)
            {
                string uniqueId = saveable.GetUniqueId();

                if (string.IsNullOrEmpty(uniqueId))
                {
                    Echo.Error($"Saveable component on {saveable.name} has invalid ID.", _enableDebug, this);
                    continue;
                }

                if (_saveables.ContainsKey(uniqueId))
                {
                    Echo.Warning($"Duplicate Saveable ID {uniqueId} found on {saveable.name}. Overwriting.", _enableDebug, this);
                }

                _saveables[uniqueId] = saveable;
            }

            Echo.Log($"Registered {_saveables.Count} Saveable components.", _enableDebug, this);
        }
        #endregion

        #region Save/Load Logic
        public void SaveGame()
        {
            try
            {
                DateTime now = DateTime.Now;
                string manualSaveKey = CreateManualSavePath(now);
                SaveMetadata metadata = new()
                {
                    Timestamp = now,
                    IsAutoSave = false,
                    GameVersion = Application.version,
                };

                ES3.Save(METADATA_KEY, metadata, manualSaveKey);

                foreach (var saveable in _saveables)
                {
                    var data = saveable.Value.CaptureState();
                    var saveableID = saveable.Key;
                    ES3.Save(saveableID, data, manualSaveKey);
                }

                Echo.Log($"Game saved to {manualSaveKey}", _enableDebug, this);
            }
            catch (Exception e)
            {
                Echo.Error($"Failed to manual save game: {e.Message}", _enableDebug, this);
            }
        }

        private void AutoSaveGame()
        {
            try
            {
                // Avoid out of sync between key and metadata
                DateTime now = DateTime.Now;
                string autoSaveKey = CreateAutoSavePath(now);
                SaveMetadata metadata = new()
                {
                    Timestamp = now,
                    IsAutoSave = true,
                    GameVersion = Application.version
                };

                // Save metadata
                ES3.Save(METADATA_KEY, metadata, autoSaveKey);
                ManageAutoSaveFiles();
                Echo.Log($"Auto-save created: {autoSaveKey}", _enableDebug, this);

                foreach (var saveable in _saveables)
                {
                    var saveableID = saveable.Key;
                    var data = saveable.Value.CaptureState();
                    if (data == null) continue;
                    ES3.Save(saveableID, data, autoSaveKey);
                }
            }
            catch (Exception e)
            {
                Echo.Error($"Failed to auto-save game: {e.Message}", _enableDebug, this);
            }
        }

        private void LoadMostRecentSave()
        {
            var allMetadata = GetAllSaveMetadata();
            SaveMetadata mostRecentMetaData = null;
            string mostRecentSaveKey = null;

            foreach (var metadata in allMetadata)
            {
                if (mostRecentMetaData == null || metadata.Timestamp > mostRecentMetaData.Timestamp)
                {
                    mostRecentMetaData = metadata;

                    mostRecentSaveKey = metadata.IsAutoSave
                        ? CreateAutoSavePath(metadata.Timestamp)
                        : CreateManualSavePath(metadata.Timestamp);
                }
            }

            if (mostRecentMetaData != null)
            {
                LoadGame(mostRecentSaveKey);
            }
            else
            {
                LoadDefaultData();
            }
        }

        private void LoadDefaultData()
        {
            foreach (var saveable in _saveables)
            {
                saveable.Value.RestoreDefaultState();
            }

            OnLoadingComplete?.Invoke();
            Echo.Log("Initialized default data for all Saveables - no existing save found.", _enableDebug, this);
        }

        public void LoadGame(string saveKey)
        {
            string normalizedSaveKey = NormalizeSavePath(saveKey);

            if (!ES3.FileExists(normalizedSaveKey))
            {
                Echo.Warning($"No save file found for {normalizedSaveKey}.", _enableDebug, this);
                LoadDefaultData();
                return;
            }

            try
            {
                SaveMetadata metadata = ES3.Load(METADATA_KEY, normalizedSaveKey, new SaveMetadata());

                // TODO: Implement migration logic
                // if (metadata.gameVersion != Application.version)
                // {
                //     VersionMigrator.MigrateSaveData(saveKey, metadata.gameVersion);
                // }

                foreach (var saveable in _saveables)
                {
                    if (ES3.KeyExists(saveable.Key, normalizedSaveKey))
                    {
                        object data = ES3.Load(saveable.Key, normalizedSaveKey);
                        saveable.Value.RestoreState(data);
                    }
                    else
                    {
                        Echo.Warning($"No data found for {saveable.Key} in {normalizedSaveKey}.", _enableDebug, this);
                    }
                }

                Echo.Log($"Game loaded from {normalizedSaveKey}, saved at {metadata.Timestamp}", _enableDebug, this);
                OnLoadingComplete?.Invoke();
            }
            catch (Exception e)
            {
                Echo.Error($"Failed to load game from {normalizedSaveKey}: {e.Message}", _enableDebug, this);
            }
        }

        public List<SaveMetadata> GetAllSaveMetadata()
        {
            List<SaveMetadata> metadata = new();

            string[] saveFiles = GetFilesInSaveFolder()
                .Where(IsManagedSaveFile)
                .ToArray();

            foreach (string file in saveFiles)
            {
                if (ES3.KeyExists(METADATA_KEY, file))
                {
                    metadata.Add(ES3.Load(METADATA_KEY, file, new SaveMetadata()));
                }
            }

            return metadata;
        }

        public void DeleteSave(string saveKey)
        {
            string normalizedSaveKey = NormalizeSavePath(saveKey);

            if (ES3.FileExists(normalizedSaveKey))
            {
                ES3.DeleteFile(normalizedSaveKey);
                Echo.Log($"{normalizedSaveKey} deleted.", _enableDebug, this);
            }
        }

        private void ManageAutoSaveFiles()
        {
            string[] autoSaveFiles = GetFilesInSaveFolder()
                .Where(IsAutoSaveFile)
                .OrderBy(f => ES3.Load<SaveMetadata>(METADATA_KEY, f).Timestamp)
                .ToArray();

            if (autoSaveFiles.Length > _maxAutoSaves)
            {
                for (int i = 0; i < autoSaveFiles.Length - _maxAutoSaves; i++)
                {
                    ES3.DeleteFile(autoSaveFiles[i]);
                    Echo.Log($"Deleted old auto-save: {autoSaveFiles[i]}", _enableDebug, this);
                }
            }
        }

        private static string[] GetFilesInSaveFolder()
        {
            if (!ES3.DirectoryExists(SAVE_FOLDER))
            {
                return Array.Empty<string>();
            }

            string[] saveFiles = ES3.GetFiles(SAVE_FOLDER);
            for (int i = 0; i < saveFiles.Length; i++)
            {
                saveFiles[i] = NormalizeSavePath(saveFiles[i]);
            }

            return saveFiles;
        }

        private static string CreateManualSavePath(DateTime timestamp)
        {
            string manualSaveFileName = $"{MANUAL_SAVE_KEY}{timestamp.Ticks}";
            return NormalizeSavePath(manualSaveFileName);
        }

        private static string CreateAutoSavePath(DateTime timestamp)
        {
            string autoSaveFileName = $"{AUTO_SAVE_KEY}{timestamp.Ticks}";
            return NormalizeSavePath(autoSaveFileName);
        }

        private static string NormalizeSavePath(string saveKey)
        {
            if (string.IsNullOrWhiteSpace(saveKey))
            {
                return saveKey;
            }

            string normalizedKey = saveKey.Replace("\\", "/");
            if (normalizedKey.StartsWith(SAVE_FOLDER, StringComparison.Ordinal))
            {
                return normalizedKey;
            }

            return $"{SAVE_FOLDER}{normalizedKey}";
        }

        private static bool IsManagedSaveFile(string filePath)
        {
            string fileName = GetSaveFileName(filePath);

            return fileName.StartsWith(MANUAL_SAVE_KEY, StringComparison.Ordinal)
                   || fileName.StartsWith(AUTO_SAVE_KEY, StringComparison.Ordinal);
        }

        private static bool IsAutoSaveFile(string filePath)
        {
            string fileName = GetSaveFileName(filePath);
            return fileName.StartsWith(AUTO_SAVE_KEY, StringComparison.Ordinal);
        }

        private static string GetSaveFileName(string filePath)
        {
            string normalizedPath = filePath.Replace("\\", "/");
            return Path.GetFileNameWithoutExtension(normalizedPath);
        }
        #endregion

        #region Auto-Save Configuration
        public void TriggerAutoSave()
        {
            if (_enableAutoSave)
            {
                AutoSaveGame();
            }
        }

        public void SetAutoSaveInterval(float interval)
        {
            _autoSaveInterval = Mathf.Max(30f, interval);
            _autoSaveTimer = _autoSaveInterval;
        }

        public void SetAutoSaveEnabled(bool autoSaveEnabled)
        {
            _enableAutoSave = autoSaveEnabled;
            if (autoSaveEnabled)
            {
                _autoSaveTimer = _autoSaveInterval;
            }
        }
        #endregion
    }

    [Serializable]
    public class SaveMetadata
    {
        public DateTime Timestamp;
        public bool IsAutoSave;
        public string GameVersion;
    }
}