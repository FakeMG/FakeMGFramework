using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FakeMG.SaveLoad
{
    /// <summary>
    /// Adapts Easy Save 3 serialization and key/file discovery to ISaveDataStore. Durability,
    /// temporary-file handling, backup rotation, flushing, and recovery belong to another service.
    /// </summary>
    public sealed class Es3SaveDataStore : ISaveDataStore
    {
        #region Public Methods

        public bool FileExists(string saveFilePath)
        {
            return ES3.FileExists(saveFilePath);
        }

        public bool DirectoryExists(string saveDirectoryPath)
        {
            return string.IsNullOrEmpty(saveDirectoryPath) || ES3.DirectoryExists(saveDirectoryPath);
        }

        public bool KeyExists(string key, string saveFilePath)
        {
            return ES3.KeyExists(key, saveFilePath);
        }

        public SaveMetadata LoadMetadata(string saveFilePath)
        {
            return ES3.Load(SaveFileCatalog.METADATA_KEY, saveFilePath, new SaveMetadata());
        }

        public object LoadState(string key, string saveFilePath)
        {
            return ES3.Load(key, saveFilePath);
        }

        public string[] GetFiles(string saveDirectoryPath)
        {
            if (!string.IsNullOrEmpty(saveDirectoryPath))
            {
                return ES3.GetFiles(saveDirectoryPath);
            }

            if (!Directory.Exists(Application.persistentDataPath))
            {
                return System.Array.Empty<string>();
            }

            string[] filePaths = Directory.GetFiles(Application.persistentDataPath);
            for (int fileIndex = 0; fileIndex < filePaths.Length; fileIndex++)
            {
                filePaths[fileIndex] = Path.GetFileName(filePaths[fileIndex]);
            }

            return filePaths;
        }

        public string[] GetDirectories(string saveDirectoryPath)
        {
            if (!string.IsNullOrEmpty(saveDirectoryPath))
            {
                return ES3.GetDirectories(saveDirectoryPath);
            }

            if (!Directory.Exists(Application.persistentDataPath))
            {
                return System.Array.Empty<string>();
            }

            string[] directoryPaths = Directory.GetDirectories(Application.persistentDataPath);
            for (int directoryIndex = 0; directoryIndex < directoryPaths.Length; directoryIndex++)
            {
                directoryPaths[directoryIndex] = Path.GetFileName(directoryPaths[directoryIndex]);
            }

            return directoryPaths;
        }

        public void DeleteFile(string saveFilePath)
        {
            ES3.DeleteFile(saveFilePath);
        }

        public void DeleteKey(string key, string saveFilePath)
        {
            ES3.DeleteKey(key, saveFilePath);
        }

        public void SaveMetadata(string saveFilePath, SaveMetadata metadata)
        {
            ES3.Save(SaveFileCatalog.METADATA_KEY, metadata, saveFilePath);
        }

        public void SaveState(string key, object state, string saveFilePath)
        {
            ES3.Save(key, state, saveFilePath);
        }

        public void WriteSaveFile(
            string saveFilePath,
            SaveMetadata metadata,
            IReadOnlyDictionary<string, object> capturedStates)
        {
            SaveMetadata(saveFilePath, metadata);
            foreach (KeyValuePair<string, object> capturedState in capturedStates)
            {
                SaveState(capturedState.Key, capturedState.Value, saveFilePath);
            }
        }

        #endregion
    }
}
