using System.Collections.Generic;

namespace FakeMG.SaveLoad
{
    /// <summary>
    /// Defines serialization and key-based file access required by save orchestration. Implementations
    /// do not own atomic replacement, backup rotation, flushing, or recovery policy.
    /// </summary>
    public interface ISaveDataStore
    {
        bool FileExists(string saveFilePath);
        bool DirectoryExists(string saveDirectoryPath);
        bool KeyExists(string key, string saveFilePath);
        SaveMetadata LoadMetadata(string saveFilePath);
        object LoadState(string key, string saveFilePath);
        string[] GetFiles(string saveDirectoryPath);
        string[] GetDirectories(string saveDirectoryPath);
        void CopyFile(string sourceFilePath, string destinationFilePath);
        void DeleteFile(string saveFilePath);
        void DeleteDirectory(string saveDirectoryPath);
        void DeleteKey(string key, string saveFilePath);
        void SaveMetadata(string saveFilePath, SaveMetadata metadata);
        void SaveState(string key, object state, string saveFilePath);
        void WriteSaveFile(
            string saveFilePath,
            SaveMetadata metadata,
            IReadOnlyDictionary<string, object> capturedStates);
    }
}
