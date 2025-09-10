using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FakeMG.Framework.SaveLoad.Advanced
{
    public class SaveLoadSystem : Singleton<SaveLoadSystem>
    {
        [SerializeField] private bool enableAutoSave = true;
        [SerializeField] private int maxAutoSaves = 5;
        [SerializeField] private float autoSaveInterval = 300f;

        private readonly Dictionary<string, Saveable> _saveables = new();

        private float _autoSaveTimer;

        private const string MANUAL_SAVE_KEY = "ManualSave_";
        private const string AUTO_SAVE_KEY = "AutoSave_";
        public const string METADATA_KEY = "Metadata";

        public event Action OnLoadingComplete;

        private void Start()
        {
            if (enableAutoSave)
            {
                _autoSaveTimer = autoSaveInterval;
            }

            RefreshSaveables();
            LoadMostRecentSave();
        }

        private void Update()
        {
            if (!enableAutoSave) return;

            _autoSaveTimer -= Time.deltaTime;
            if (_autoSaveTimer <= 0)
            {
                AutoSaveGame();
                _autoSaveTimer = autoSaveInterval;
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
                    Debug.LogError($"Saveable component on {saveable.name} has invalid ID.");
                    continue;
                }

                if (_saveables.ContainsKey(uniqueId))
                {
                    Debug.LogWarning($"Duplicate Saveable ID {uniqueId} found on {saveable.name}. Overwriting.");
                }

                _saveables[uniqueId] = saveable;
            }

            Debug.Log($"Registered {_saveables.Count} Saveable components.");
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
                    isAutoSave = false,
                    gameVersion = Application.version,
                };

                ES3.Save(METADATA_KEY, metadata, manualSaveKey);

                foreach (var saveable in _saveables)
                {
                    var data = saveable.Value.CaptureState();
                    var saveableID = saveable.Key;
                    ES3.Save(saveableID, data, manualSaveKey);
                }

                Debug.Log($"Game saved to {manualSaveKey}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to manual save game: {e.Message}");
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
                    isAutoSave = true,
                    gameVersion = Application.version
                };

                // Save metadata
                ES3.Save(METADATA_KEY, metadata, autoSaveKey);
                ManageAutoSaveFiles();
                Debug.Log($"Auto-save created: {autoSaveKey}");

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
                Debug.LogError($"Failed to auto-save game: {e.Message}");
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

                    mostRecentSaveKey = metadata.isAutoSave
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
            Debug.Log("Initialized default data for all Saveables - no existing save found.");
        }

        public void LoadGame(string saveKey)
        {
            if (!ES3.FileExists(saveKey))
            {
                Debug.LogWarning($"No save file found for {saveKey}.");
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
                        Debug.LogWarning($"No data found for {saveable.Key} in {saveKey}.");
                    }
                }

                Debug.Log($"Game loaded from {saveKey}, saved at {metadata.Timestamp}");
                OnLoadingComplete?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load game from {saveKey}: {e.Message}");
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
                Debug.Log($"{saveKey} deleted.");
            }
        }

        private void ManageAutoSaveFiles()
        {
            string[] autoSaveFiles = ES3.GetFiles()
                .Where(f => f.StartsWith($"{AUTO_SAVE_KEY}"))
                .OrderBy(f => ES3.Load<SaveMetadata>(METADATA_KEY, f).Timestamp)
                .ToArray();

            if (autoSaveFiles.Length > maxAutoSaves)
            {
                for (int i = 0; i < autoSaveFiles.Length - maxAutoSaves; i++)
                {
                    ES3.DeleteFile(autoSaveFiles[i]);
                    Debug.Log($"Deleted old auto-save: {autoSaveFiles[i]}");
                }
            }
        }
        #endregion

        #region Auto-Save Configuration
        public void TriggerAutoSave()
        {
            if (enableAutoSave)
            {
                AutoSaveGame();
            }
        }

        public void SetAutoSaveInterval(float interval)
        {
            autoSaveInterval = Mathf.Max(30f, interval);
            _autoSaveTimer = autoSaveInterval;
        }

        public void SetAutoSaveEnabled(bool autoSaveEnabled)
        {
            enableAutoSave = autoSaveEnabled;
            if (autoSaveEnabled)
            {
                _autoSaveTimer = autoSaveInterval;
            }
        }
        #endregion
    }

    [Serializable]
    public class SaveMetadata
    {
        public DateTime Timestamp;
        public bool isAutoSave;
        public string gameVersion;
    }
}