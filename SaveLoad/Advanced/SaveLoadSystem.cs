using System;
using System.Collections.Generic;
using System.Linq;
using FakeMG.Framework;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace FakeMG.SaveLoad.Advanced
{
    public class SaveLoadSystem : MonoBehaviour
    {
        [Header("Storage")]
        [Tooltip("Relative directory path for this save system. Leave empty to save in the root directory. Supports nested directories such as ProfileA/Slot1.")]
        [FormerlySerializedAs("_saveFolderPath")]
        [SerializeField] private string _saveDirectoryPath = string.Empty;
        [SerializeField] private SaveFileMode _saveFileMode = SaveFileMode.TimestampedFiles;
        [ShowIf(nameof(UsesFixedSaveFileMode))]
        [SerializeField] private string _fixedSaveFileName = SaveFileCatalog.DEFAULT_FIXED_SAVE_FILE_NAME;
        [Tooltip("Optional root used when collecting Saveable components. Leave empty to scan this object hierarchy.")]
        [SerializeField] private Transform _saveablesRoot;

        [SerializeField] private bool _enableAutoSave = true;
        [SerializeField] private int _maxAutoSaves = 5;
        [SerializeField] private float _autoSaveInterval = 300f;
        [SerializeField] private bool _enableDebug = true;

        [Header("Migration")]
        [Tooltip("Assign a MigrationRegistrySO to enable automatic save file migration on load.")]
        [SerializeField] private MigrationRegistrySO _migrationRegistry;

        private readonly Dictionary<string, Saveable> _saveables = new();
        private VersionMigrator _migrationRunner;
        private string _normalizedSaveDirectoryPath;
        private string _fixedSaveFilePath;

        private float _autoSaveTimer;

        public event Action OnLoadingComplete;

        private void Awake()
        {
            if (!TryInitializeStorageConfiguration())
            {
                enabled = false;
                return;
            }

            if (_enableAutoSave)
            {
                _autoSaveTimer = _autoSaveInterval;
            }

            if (_migrationRegistry)
            {
                _migrationRunner = new VersionMigrator(_migrationRegistry, _enableDebug);
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

            Transform saveableCollectionRoot = _saveablesRoot ? _saveablesRoot : transform;
            Saveable[] saveables = saveableCollectionRoot.GetComponentsInChildren<Saveable>(true);

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
        [Button("Save Game")]
        public void SaveGame()
        {
            try
            {
                DateTime now = DateTime.Now;
                string saveFilePath = GetManualSaveFilePath(now);
                SaveMetadata metadata = CreateMetadata(now, GetManualSaveKind());

                SaveToFile(saveFilePath, metadata);
                Echo.Log($"Game saved to {saveFilePath}", _enableDebug, this);
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
                DateTime now = DateTime.Now;
                string autoSaveFilePath = GetAutoSaveFilePath(now);
                SaveMetadata metadata = CreateMetadata(now, SaveFileKind.Auto);

                SaveToFile(autoSaveFilePath, metadata);
                ManageAutoSaveFiles();
                Echo.Log($"Auto-save created: {autoSaveFilePath}", _enableDebug, this);
            }
            catch (Exception e)
            {
                Echo.Error($"Failed to auto-save game: {e.Message}", _enableDebug, this);
            }
        }

        private void LoadMostRecentSave()
        {
            if (UsesFixedSaveFileMode())
            {
                LoadConfiguredFixedSave();
                return;
            }

            ManagedSaveFileInfo mostRecentSave = SaveFileCatalog.GetManagedSaveFiles(_normalizedSaveDirectoryPath)
                .OrderByDescending(saveFile => saveFile.Metadata.Timestamp)
                .FirstOrDefault();

            if (mostRecentSave != null)
            {
                LoadGame(mostRecentSave.SaveFilePath);
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

        public void LoadGame(string saveFilePath)
        {
            string normalizedSaveFilePath = SaveFileCatalog.NormalizeSaveFilePath(saveFilePath, _normalizedSaveDirectoryPath);

            if (!ES3.FileExists(normalizedSaveFilePath))
            {
                Echo.Warning($"No save file found for {normalizedSaveFilePath}.", _enableDebug, this);
                LoadDefaultData();
                return;
            }

            try
            {
                SaveMetadata metadata = ES3.Load(SaveFileCatalog.METADATA_KEY, normalizedSaveFilePath, new SaveMetadata());

                if (_migrationRunner != null && metadata.GameVersion != Application.version)
                {
                    bool migrationSucceeded = _migrationRunner.MigrateSaveFile(normalizedSaveFilePath, metadata.GameVersion);
                    if (!migrationSucceeded)
                    {
                        Echo.Error($"Migration failed for {normalizedSaveFilePath}. Loading aborted.", _enableDebug, this);
                        LoadDefaultData();
                        return;
                    }
                }

                foreach (var saveable in _saveables)
                {
                    if (ES3.KeyExists(saveable.Key, normalizedSaveFilePath))
                    {
                        object data = ES3.Load(saveable.Key, normalizedSaveFilePath);
                        saveable.Value.RestoreState(data);
                    }
                    else
                    {
                        saveable.Value.RestoreDefaultState();
                        Echo.Warning($"No data found for {saveable.Key} in {normalizedSaveFilePath}. Restored to default state.", _enableDebug, this);
                    }
                }

                Echo.Log($"Game loaded from {normalizedSaveFilePath}, saved at {metadata.Timestamp}", _enableDebug, this);
                OnLoadingComplete?.Invoke();
            }
            catch (Exception e)
            {
                Echo.Error($"Failed to load game from {normalizedSaveFilePath}: {e.Message}", _enableDebug, this);
            }
        }

        public void DeleteSave(string saveFilePath)
        {
            string normalizedSaveFilePath = SaveFileCatalog.NormalizeSaveFilePath(saveFilePath, _normalizedSaveDirectoryPath);

            if (ES3.FileExists(normalizedSaveFilePath))
            {
                ES3.DeleteFile(normalizedSaveFilePath);
                Echo.Log($"{normalizedSaveFilePath} deleted.", _enableDebug, this);
            }
        }

        private void ManageAutoSaveFiles()
        {
            ManagedSaveFileInfo[] autoSaveFiles = SaveFileCatalog.GetManagedSaveFiles(_normalizedSaveDirectoryPath)
                .Where(saveFile => saveFile.SaveKind == SaveFileKind.Auto)
                .OrderBy(saveFile => saveFile.Metadata.Timestamp)
                .ToArray();

            if (autoSaveFiles.Length > _maxAutoSaves)
            {
                for (int i = 0; i < autoSaveFiles.Length - _maxAutoSaves; i++)
                {
                    ES3.DeleteFile(autoSaveFiles[i].SaveFilePath);
                    Echo.Log($"Deleted old auto-save: {autoSaveFiles[i].SaveFilePath}", _enableDebug, this);
                }
            }
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

        private bool TryInitializeStorageConfiguration()
        {
            try
            {
                _normalizedSaveDirectoryPath = SaveFileCatalog.NormalizeSaveDirectoryPath(_saveDirectoryPath);
                _fixedSaveFilePath = UsesFixedSaveFileMode()
                    ? SaveFileCatalog.CreateFixedSaveFilePath(_normalizedSaveDirectoryPath, _fixedSaveFileName)
                    : null;
                return true;
            }
            catch (ArgumentException exception)
            {
                Echo.Error($"Save storage configuration is invalid on {name}: {exception.Message}", _enableDebug, this);
                return false;
            }
        }

        private string GetManualSaveFilePath(DateTime timestamp)
        {
            return UsesFixedSaveFileMode()
                ? _fixedSaveFilePath
                : SaveFileCatalog.CreateManualSaveFilePath(_normalizedSaveDirectoryPath, timestamp);
        }

        private string GetAutoSaveFilePath(DateTime timestamp)
        {
            return UsesFixedSaveFileMode()
                ? _fixedSaveFilePath
                : SaveFileCatalog.CreateAutoSaveFilePath(_normalizedSaveDirectoryPath, timestamp);
        }

        private SaveFileKind GetManualSaveKind()
        {
            return UsesFixedSaveFileMode()
                ? SaveFileKind.Fixed
                : SaveFileKind.Manual;
        }

        private SaveMetadata CreateMetadata(DateTime timestamp, SaveFileKind saveKind)
        {
            return new SaveMetadata
            {
                Timestamp = timestamp,
                GameVersion = Application.version,
                SaveKind = saveKind,
            };
        }

        private void SaveToFile(string saveFilePath, SaveMetadata metadata)
        {
            ES3.Save(SaveFileCatalog.METADATA_KEY, metadata, saveFilePath);

            foreach (var saveable in _saveables)
            {
                object data = saveable.Value.CaptureState();
                string saveableId = saveable.Key;
                ES3.Save(saveableId, data, saveFilePath);
            }
        }

        private void LoadConfiguredFixedSave()
        {
            if (!ES3.FileExists(_fixedSaveFilePath))
            {
                LoadDefaultData();
                return;
            }

            LoadGame(_fixedSaveFilePath);
        }

        private bool UsesFixedSaveFileMode()
        {
            return _saveFileMode == SaveFileMode.FixedFile;
        }
        #endregion
    }

    [Serializable]
    public class SaveMetadata
    {
        public DateTime Timestamp;
        public string GameVersion;
        public SaveFileKind SaveKind;
    }

    public enum SaveFileMode
    {
        TimestampedFiles = 0,
        FixedFile = 1,
    }

    public enum SaveFileKind
    {
        Unknown = 0,
        Manual = 1,
        Auto = 2,
        Fixed = 3,
    }
}