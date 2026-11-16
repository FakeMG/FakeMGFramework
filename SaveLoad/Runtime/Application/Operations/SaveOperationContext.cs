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
