using System;

namespace FakeMG.SaveLoad
{
    /// <summary>
    /// Signals that structurally readable persisted data is incompatible and must not be replaced by
    /// defaults. The coordinator may still try the retained backup before aborting the load.
    /// </summary>
    public sealed class SaveLoadRejectedException : Exception
    {
        public SaveLoadRejectedException(string message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Describes one save operation without exposing the SaveLoadSystem implementation. External
    /// payload participants use its stable path and save kind to coordinate their own transactions.
    /// </summary>
    public readonly struct SaveOperationContext
    {
        public string SaveDirectoryPath { get; }
        public string SaveFilePath { get; }
        public SaveFileKind SaveKind { get; }
        public DateTime TimestampUtc { get; }

        public SaveOperationContext(
            string saveDirectoryPath, string saveFilePath, SaveFileKind saveKind, DateTime timestampUtc)
        {
            SaveDirectoryPath = saveDirectoryPath;
            SaveFilePath = saveFilePath;
            SaveKind = saveKind;
            TimestampUtc = timestampUtc;
        }
    }

    /// <summary>
    /// Describes the metadata file that has just been restored. Participants use this after their
    /// Saveable state is available to validate or prepare external payload repositories.
    /// </summary>
    public readonly struct LoadOperationContext
    {
        public string SaveDirectoryPath { get; }
        public string SaveFilePath { get; }

        public LoadOperationContext(string saveDirectoryPath, string saveFilePath)
        {
            SaveDirectoryPath = saveDirectoryPath;
            SaveFilePath = saveFilePath;
        }
    }
}
