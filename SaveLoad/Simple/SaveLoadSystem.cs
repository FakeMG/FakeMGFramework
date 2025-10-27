using System;
using System.Collections.Generic;
using FakeMG.Framework.SaveLoad.Simple.Storages;
using UnityEngine;

namespace FakeMG.Framework.SaveLoad.Simple
{
    public class SaveLoadSystem : MonoBehaviour
    {
        [SerializeField] private StorageType _storageType = StorageType.PlayerPrefs;
        public static SaveLoadSystem Instance { get; private set; }

        private readonly Dictionary<string, Saveable> _saveables = new();
        private const string SAVE_KEY = "SaveFile";
        private ISaveStorage _storage;

        private enum StorageType
        {
            PlayerPrefs,
            JsonFile
        }

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            switch (_storageType)
            {
                case StorageType.PlayerPrefs:
                    _storage = new PlayerPrefsStorage();
                    break;
                case StorageType.JsonFile:
                    _storage = new JsonFileStorage();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #region Saveable Registration
        public void RegisterSaveable(Saveable saveable, string uniqueId)
        {
            if (string.IsNullOrEmpty(uniqueId))
            {
                Debug.LogError("Saveable component registered with invalid ID.");
                return;
            }

            if (_saveables.ContainsKey(uniqueId))
            {
                Debug.LogWarning($"Saveable with ID {uniqueId} already registered. Overwriting.");
                _saveables[uniqueId] = saveable;
            }
            else
            {
                _saveables.Add(uniqueId, saveable);
            }
        }

        public void UnregisterSaveable(string uniqueId)
        {
            _saveables.Remove(uniqueId);
        }
        #endregion

        public void SaveGame()
        {
            try
            {
                SaveProfile profile = CaptureProfile();
                _storage.Save(SAVE_KEY, profile);

                Debug.Log($"Game saved to {SAVE_KEY}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to manual save game: {e.Message}");
            }
        }

        public void LoadGame(string saveKey)
        {
            if (!_storage.FileExists(saveKey))
            {
                Debug.LogWarning($"No save file found for {saveKey}.");
                return;
            }

            try
            {
                var loadedProfile = _storage.Load(saveKey);
                ApplyProfile(loadedProfile);

                Debug.Log($"Game loaded from {saveKey}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load game from {saveKey}: {e.Message}");
            }
        }

        private SaveProfile CaptureProfile()
        {
            var profile = new SaveProfile();
            foreach (var kvp in _saveables)
            {
                profile.Data[kvp.Key] = kvp.Value.CaptureState();
            }

            return profile;
        }

        private void ApplyProfile(SaveProfile profile)
        {
            foreach (var kvp in profile.Data)
            {
                if (_saveables.TryGetValue(kvp.Key, out var savable))
                {
                    savable.RestoreState(kvp.Value);
                }
            }
        }
    }

    [Serializable]
    public class SaveProfile
    {
        public Dictionary<string, object> Data = new();
    }
}