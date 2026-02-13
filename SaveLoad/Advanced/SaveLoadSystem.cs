using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FakeMG.Framework.SaveLoad.Advanced
{
    public class SaveLoadSystem : Singleton<SaveLoadSystem>
    {
        [SerializeField] private bool _enableAutoSave = true;
        [SerializeField] private int _maxAutoSaves = 5;
        [SerializeField] private float _autoSaveInterval = 300f;
        [SerializeField] private bool _enableDebug = true;

        private readonly Dictionary<string, Saveable> _saveables = new();

        private float _autoSaveTimer;

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
                string manualSaveKey = $"{MANUAL_SAVE_KEY}{now.Ticks}";
                SaveMetadata metadata = new SaveMetadata
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
                string autoSaveKey = $"{AUTO_SAVE_KEY}{now.Ticks}";
                SaveMetadata metadata = new SaveMetadata
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
                        ? $"{AUTO_SAVE_KEY}{metadata.Timestamp.Ticks}"
                        : $"{MANUAL_SAVE_KEY}{metadata.Timestamp.Ticks}";
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
            if (!ES3.FileExists(saveKey))
            {
                Echo.Warning($"No save file found for {saveKey}.", _enableDebug, this);
                LoadDefaultData();
                return;
            }

            try
            {
                SaveMetadata metadata = ES3.Load(METADATA_KEY, saveKey, new SaveMetadata());

                // TODO: Implement migration logic
                // if (metadata.gameVersion != Application.version)
                // {
                //     VersionMigrator.MigrateSaveData(saveKey, metadata.gameVersion);
                // }

                foreach (var saveable in _saveables)
                {
                    if (ES3.KeyExists(saveable.Key, saveKey))
                    {
                        object data = ES3.Load(saveable.Key, saveKey);
                        saveable.Value.RestoreState(data);
                    }
                    else
                    {
                        Echo.Warning($"No data found for {saveable.Key} in {saveKey}.", _enableDebug, this);
                    }
                }

                Echo.Log($"Game loaded from {saveKey}, saved at {metadata.Timestamp}", _enableDebug, this);
                OnLoadingComplete?.Invoke();
            }
            catch (Exception e)
            {
                Echo.Error($"Failed to load game from {saveKey}: {e.Message}", _enableDebug, this);
            }
        }

        public List<SaveMetadata> GetAllSaveMetadata()
        {
            List<SaveMetadata> metadata = new();

            string[] saveFiles = ES3.GetFiles()
                .Where(f => f.StartsWith(MANUAL_SAVE_KEY) || f.StartsWith(AUTO_SAVE_KEY)).ToArray();

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
            if (ES3.FileExists(saveKey))
            {
                ES3.DeleteFile(saveKey);
                Echo.Log($"{saveKey} deleted.", _enableDebug, this);
            }
        }

        private void ManageAutoSaveFiles()
        {
            string[] autoSaveFiles = ES3.GetFiles()
                .Where(f => f.StartsWith($"{AUTO_SAVE_KEY}"))
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