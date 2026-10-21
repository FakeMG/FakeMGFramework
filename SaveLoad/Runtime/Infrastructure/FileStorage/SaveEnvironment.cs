using UnityEngine;

namespace FakeMG.SaveLoad
{
    public interface ISaveEnvironment
    {
        string StorageRootPath { get; }
        string ApplicationVersion { get; }
    }

    public sealed class UnitySaveEnvironment : ISaveEnvironment
    {
        // Capture Unity properties at startup so storage workers never call Unity's main-thread-only getters.
        public string StorageRootPath { get; } = Application.persistentDataPath;
        public string ApplicationVersion { get; } = Application.version;
    }
}
