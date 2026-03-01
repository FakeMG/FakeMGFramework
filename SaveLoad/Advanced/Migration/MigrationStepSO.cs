using System;
using UnityEngine;

namespace FakeMG.SaveLoad.Advanced
{
    /// <summary>
    /// Base class for a single version migration step.
    /// Subclass this for each new version that requires save data changes.
    /// </summary>
    public abstract class MigrationStepSO : ScriptableObject
    {
        [Tooltip("The version the save file becomes AFTER this migration runs (e.g., 1.1.0).")]
        [SerializeField] private string _targetVersion;

        public string TargetVersion => _targetVersion;

        public Version ParsedTargetVersion => Version.Parse(_targetVersion);

        /// <summary>
        /// Performs the migration on the save file at the given path.
        /// Use ES3 APIs to manipulate keys and data directly.
        /// </summary>
        public abstract void Migrate(string savePath);

        #region Utility Methods

        protected static void RenameKey(string oldKey, string newKey, string savePath)
        {
            if (!ES3.KeyExists(oldKey, savePath)) return;

            object data = ES3.Load(oldKey, savePath);
            ES3.Save(newKey, data, savePath);
            ES3.DeleteKey(oldKey, savePath);
        }

        protected static void DeleteKey(string key, string savePath)
        {
            if (ES3.KeyExists(key, savePath))
            {
                ES3.DeleteKey(key, savePath);
            }
        }

        protected static void SetDefaultValue<T>(string key, T defaultValue, string savePath)
        {
            if (!ES3.KeyExists(key, savePath))
            {
                ES3.Save(key, defaultValue, savePath);
            }
        }

        #endregion

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_targetVersion)) return;

            if (!Version.TryParse(_targetVersion, out _))
            {
                Debug.LogError($"[{name}] TargetVersion '{_targetVersion}' is not a valid version format (expected Major.Minor.Patch).", this);
            }
        }
    }
}