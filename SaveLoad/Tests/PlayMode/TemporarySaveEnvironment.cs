namespace FakeMG.SaveLoad.Tests.PlayMode
{
    public sealed class TemporarySaveEnvironment : ISaveEnvironment
    {
        private const string APPLICATION_VERSION = "1.0.0";

        public string StorageRootPath { get; }
        public string ApplicationVersion => APPLICATION_VERSION;

        public TemporarySaveEnvironment(string storageRootPath)
        {
            StorageRootPath = storageRootPath;
        }
    }
}
