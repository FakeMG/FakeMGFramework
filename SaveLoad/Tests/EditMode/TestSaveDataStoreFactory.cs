namespace FakeMG.SaveLoad.Tests
{
    internal sealed class TestSaveEnvironment : ISaveEnvironment
    {
        public string StorageRootPath { get; }
        public string ApplicationVersion { get; }

        public TestSaveEnvironment(string storageRootPath = "TestStorage", string applicationVersion = "1.0.0")
        {
            StorageRootPath = storageRootPath;
            ApplicationVersion = applicationVersion;
        }
    }

    internal sealed class TestSaveDataStoreFactory : ISaveDataStoreFactory
    {
        private readonly ISaveDataStore _saveDataStore;
        private readonly System.Collections.Generic.List<SaveFileProtectionSettings> _createdProtectionSettings = new();

        public TestSaveDataStoreFactory(ISaveDataStore saveDataStore)
        {
            _saveDataStore = saveDataStore;
        }

        public SaveFileProtectionSettings LastProtectionSettings { get; private set; }
        public System.Collections.Generic.IReadOnlyList<SaveFileProtectionSettings> CreatedProtectionSettings =>
            _createdProtectionSettings;

        public ISaveDataStore Create(ISaveDataStoreProfile profile)
        {
            SaveFileProtectionSettings protectionSettings = (SaveFileProtectionSettings)profile;
            LastProtectionSettings = protectionSettings;
            _createdProtectionSettings.Add(protectionSettings);
            return _saveDataStore;
        }
    }
}
