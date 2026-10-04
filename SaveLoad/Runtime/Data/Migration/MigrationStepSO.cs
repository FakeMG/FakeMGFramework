using System;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.SaveLoad
{
    /// <summary>
    /// Defines one ordered save-version transformation. Implementations receive the configured data
    /// store, so migration rules remain independent from the active serializer.
    /// </summary>
    public abstract class MigrationStepSO : ScriptableObject, ISaveMigrationStep
    {
        [Tooltip("The exact version accepted by this migration, for example 1.0.0.")]
        [SerializeField] private string _sourceVersion;

        [Tooltip("The version the save file becomes after this migration runs, for example 1.1.0.")]
        [SerializeField] private string _targetVersion;

        public string SourceVersion => _sourceVersion;
        public string TargetVersion => _targetVersion;

        #region Unity Lifecycle

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_sourceVersion))
            {
                Echo.Error($"[{name}] SourceVersion is required.");
            }
            else if (!Version.TryParse(_sourceVersion, out _))
            {
                Echo.Error($"[{name}] SourceVersion '{_sourceVersion}' is not a valid version format.");
            }

            if (string.IsNullOrWhiteSpace(_targetVersion))
            {
                Echo.Error($"[{name}] TargetVersion is required.");
            }
            else if (!Version.TryParse(_targetVersion, out _))
            {
                Echo.Error($"[{name}] TargetVersion '{_targetVersion}' is not a valid version format.");
            }

            if (Version.TryParse(_sourceVersion, out Version sourceVersion) &&
                Version.TryParse(_targetVersion, out Version targetVersion) &&
                targetVersion <= sourceVersion)
            {
                Echo.Error($"[{name}] TargetVersion must be newer than SourceVersion.");
            }
        }

        #endregion

        #region Public Methods

        public abstract void Migrate(ISaveDataStore saveDataStore, string saveFilePath);

        #endregion

        #region Protected Methods

        protected static void RenameKey(
            ISaveDataStore saveDataStore,
            string oldKey,
            string newKey,
            string saveFilePath)
        {
            if (!saveDataStore.KeyExists(oldKey, saveFilePath))
            {
                return;
            }

            object state = saveDataStore.LoadState(oldKey, saveFilePath);
            saveDataStore.SaveState(newKey, state, saveFilePath);
            saveDataStore.DeleteKey(oldKey, saveFilePath);
        }

        protected static void DeleteKey(
            ISaveDataStore saveDataStore,
            string key,
            string saveFilePath)
        {
            if (saveDataStore.KeyExists(key, saveFilePath))
            {
                saveDataStore.DeleteKey(key, saveFilePath);
            }
        }

        protected static void SetDefaultValue<T>(
            ISaveDataStore saveDataStore,
            string key,
            T defaultValue,
            string saveFilePath)
        {
            if (!saveDataStore.KeyExists(key, saveFilePath))
            {
                saveDataStore.SaveState(key, defaultValue, saveFilePath);
            }
        }

        #endregion
    }
}
