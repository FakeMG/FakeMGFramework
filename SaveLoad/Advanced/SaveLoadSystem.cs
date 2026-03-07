using System;
using System.Collections.Generic;
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

        [Header("Migration")]
        [Tooltip("Assign a MigrationRegistrySO to enable automatic save file migration on load.")]
        [SerializeField] private MigrationRegistrySO _migrationRegistry;

        private readonly Dictionary<string, Saveable> _saveables = new();
        private VersionMigrator _migrationRunner;

        private float _autoSaveTimer;

        public event Action OnLoadingComplete;

        private void Awake()
        {
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
                string manualSavePath = SaveFileCatalog.CreateManualSavePath(now);
                SaveMetadata metadata = new()
                {
                    Timestamp = now,
                    IsAutoSave = false,
                    GameVersion = Application.version,
                };

                ES3.Save(SaveFileCatalog.METADATA_KEY, metadata, manualSavePath);

                foreach (var saveable in _saveables)
                {
                    var data = saveable.Value.CaptureState();
                    var saveableID = saveable.Key;
                    ES3.Save(saveableID, data, manualSavePath);
                }

                Echo.Log($"Game saved to {manualSavePath}", _enableDebug, this);
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
                // Avoid out of sync between path and metadata
                DateTime now = DateTime.Now;
                string autoSavePath = SaveFileCatalog.CreateAutoSavePath(now);
                SaveMetadata metadata = new()
                {
                    Timestamp = now,
                    IsAutoSave = true,
                    GameVersion = Application.version
                };

                // Save metadata
                ES3.Save(SaveFileCatalog.METADATA_KEY, metadata, autoSavePath);
                ManageAutoSaveFiles();
                Echo.Log($"Auto-save created: {autoSavePath}", _enableDebug, this);

                foreach (var saveable in _saveables)
                {
                    var saveableID = saveable.Key;
                    var data = saveable.Value.CaptureState();
                    if (data == null) continue;
                    ES3.Save(saveableID, data, autoSavePath);
                }
            }
            catch (Exception e)
            {
                Echo.Error($"Failed to auto-save game: {e.Message}", _enableDebug, this);
            }
        }

        private void LoadMostRecentSave()
        {
            ManagedSaveFileInfo mostRecentSave = SaveFileCatalog.GetManagedSaveFiles()
                .OrderByDescending(saveFile => saveFile.Metadata.Timestamp)
                .FirstOrDefault();

            if (mostRecentSave != null)
            {
                LoadGame(mostRecentSave.FilePath);
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

        public void LoadGame(string savePath)
        {
            string normalizedSavePath = SaveFileCatalog.NormalizeSavePath(savePath);

            if (!ES3.FileExists(normalizedSavePath))
            {
                Echo.Warning($"No save file found for {normalizedSavePath}.", _enableDebug, this);
                LoadDefaultData();
                return;
            }

            try
            {
                SaveMetadata metadata = ES3.Load(SaveFileCatalog.METADATA_KEY, normalizedSavePath, new SaveMetadata());

                if (_migrationRunner != null && metadata.GameVersion != Application.version)
                {
                    bool migrationSucceeded = _migrationRunner.MigrateSaveFile(normalizedSavePath, metadata.GameVersion);
                    if (!migrationSucceeded)
                    {
                        Echo.Error($"Migration failed for {normalizedSavePath}. Loading aborted.", _enableDebug, this);
                        LoadDefaultData();
                        return;
                    }
                }

                foreach (var saveable in _saveables)
                {
                    if (ES3.KeyExists(saveable.Key, normalizedSavePath))
                    {
                        object data = ES3.Load(saveable.Key, normalizedSavePath);
                        saveable.Value.RestoreState(data);
                    }
                    else
                    {
                        saveable.Value.RestoreDefaultState();
                        Echo.Warning($"No data found for {saveable.Key} in {normalizedSavePath}. Restored to default state.", _enableDebug, this);
                    }
                }

                Echo.Log($"Game loaded from {normalizedSavePath}, saved at {metadata.Timestamp}", _enableDebug, this);
                OnLoadingComplete?.Invoke();
            }
            catch (Exception e)
            {
                Echo.Error($"Failed to load game from {normalizedSavePath}: {e.Message}", _enableDebug, this);
            }
        }

        public void DeleteSave(string savePath)
        {
            string normalizedSavePath = SaveFileCatalog.NormalizeSavePath(savePath);

            if (ES3.FileExists(normalizedSavePath))
            {
                ES3.DeleteFile(normalizedSavePath);
                Echo.Log($"{normalizedSavePath} deleted.", _enableDebug, this);
            }
        }

        private void ManageAutoSaveFiles()
        {
            ManagedSaveFileInfo[] autoSaveFiles = SaveFileCatalog.GetManagedSaveFiles()
                .Where(saveFile => saveFile.Metadata.IsAutoSave)
                .OrderBy(saveFile => saveFile.Metadata.Timestamp)
                .ToArray();

            if (autoSaveFiles.Length > _maxAutoSaves)
            {
                for (int i = 0; i < autoSaveFiles.Length - _maxAutoSaves; i++)
                {
                    ES3.DeleteFile(autoSaveFiles[i].FilePath);
                    Echo.Log($"Deleted old auto-save: {autoSaveFiles[i].FilePath}", _enableDebug, this);
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