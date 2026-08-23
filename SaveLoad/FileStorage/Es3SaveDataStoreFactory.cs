namespace FakeMG.SaveLoad
{
    public sealed class Es3SaveDataStoreFactory : ISaveDataStoreFactory
    {
        private readonly ISaveEnvironment _saveEnvironment;

        public Es3SaveDataStoreFactory(ISaveEnvironment saveEnvironment)
        {
            _saveEnvironment = saveEnvironment;
        }

        #region Public Methods

        public ISaveDataStore Create(ISaveDataStoreProfile profile)
        {
            if (profile is not SaveFileProtectionSettings protectionSettings)
            {
                throw new System.ArgumentException(
                    $"Unsupported save data store profile '{profile?.GetType().Name ?? "missing"}'.",
                    nameof(profile));
            }

            return new Es3SaveDataStore(protectionSettings, _saveEnvironment.StorageRootPath);
        }

        #endregion
    }
}
