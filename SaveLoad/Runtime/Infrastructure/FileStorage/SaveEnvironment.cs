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
        public string StorageRootPath => Application.persistentDataPath;
        public string ApplicationVersion => Application.version;
    }
}
