using System.Collections.Generic;
using System.IO;

namespace FakeMG.SaveLoad
{
    /// <summary>
    /// Adapts Easy Save 3 serialization and key/file discovery to ISaveDataStore. Durability,
    /// temporary-file handling, backup rotation, flushing, and recovery belong to another service.
    /// </summary>
    public sealed class Es3SaveDataStore : ISaveDataStore
    {
        private readonly SaveFileProtectionSettings _protectionSettings;
        private readonly string _storageRootPath;

        public Es3SaveDataStore(SaveFileProtectionSettings protectionSettings, string storageRootPath)
        {
            _protectionSettings = protectionSettings ?? throw new System.ArgumentNullException(nameof(protectionSettings));
            _storageRootPath = storageRootPath ?? throw new System.ArgumentNullException(nameof(storageRootPath));
        }

        #region Public Methods

        public bool FileExists(string saveFilePath)
        {
            return ES3.FileExists(CreateSettings(saveFilePath));
        }

        public bool DirectoryExists(string saveDirectoryPath)
        {
            return string.IsNullOrEmpty(saveDirectoryPath) || ES3.DirectoryExists(CreateSettings(saveDirectoryPath));
        }

        public bool KeyExists(string key, string saveFilePath)
        {
            return ES3.KeyExists(key, CreateSettings(saveFilePath));
        }

        public SaveMetadata LoadMetadata(string saveFilePath)
        {
            return ES3.Load(SaveFileCatalog.METADATA_KEY, new SaveMetadata(), CreateSettings(saveFilePath));
        }

        public object LoadState(string key, string saveFilePath)
        {
            return ES3.Load(key, CreateSettings(saveFilePath));
        }

        public string[] GetFiles(string saveDirectoryPath)
        {
            if (!string.IsNullOrEmpty(saveDirectoryPath))
            {
                return ES3.GetFiles(CreateSettings(saveDirectoryPath));
            }

            if (!Directory.Exists(_storageRootPath))
            {
                return System.Array.Empty<string>();
            }

            string[] filePaths = Directory.GetFiles(_storageRootPath);
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
                return ES3.GetDirectories(CreateSettings(saveDirectoryPath));
            }

            if (!Directory.Exists(_storageRootPath))
            {
                return System.Array.Empty<string>();
            }

            string[] directoryPaths = Directory.GetDirectories(_storageRootPath);
            for (int directoryIndex = 0; directoryIndex < directoryPaths.Length; directoryIndex++)
            {
                directoryPaths[directoryIndex] = Path.GetFileName(directoryPaths[directoryIndex]);
            }

            return directoryPaths;
        }

        public void CopyFile(string sourceFilePath, string destinationFilePath)
        {
            ES3.CopyFile(CreateSettings(sourceFilePath), CreateSettings(destinationFilePath));
        }

        public void DeleteFile(string saveFilePath)
        {
            ES3.DeleteFile(CreateSettings(saveFilePath));
        }

        public void DeleteDirectory(string saveDirectoryPath)
        {
            ES3.DeleteDirectory(CreateSettings(saveDirectoryPath));
        }

        public void DeleteKey(string key, string saveFilePath)
        {
            ES3.DeleteKey(key, CreateSettings(saveFilePath));
        }

        public void SaveMetadata(string saveFilePath, SaveMetadata metadata)
        {
            ES3.Save(SaveFileCatalog.METADATA_KEY, metadata, CreateSettings(saveFilePath));
        }

        public void SaveState(string key, object state, string saveFilePath)
        {
            ES3.Save(key, state, CreateSettings(saveFilePath));
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

        #region Private Methods

        private ES3Settings CreateSettings(string saveFilePath)
        {
            string resolvedFilePath = Path.IsPathRooted(saveFilePath)
                ? saveFilePath
                : Path.Combine(_storageRootPath, saveFilePath);
            return Es3FileSettingsFactory.Create(resolvedFilePath, _protectionSettings);
        }

        #endregion
    }
}
