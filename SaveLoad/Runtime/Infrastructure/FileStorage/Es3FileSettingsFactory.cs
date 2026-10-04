namespace FakeMG.SaveLoad
{
    public static class Es3FileSettingsFactory
    {
        #region Public Methods

        public static ES3Settings Create(string saveFilePath, SaveFileProtectionSettings protectionSettings)
        {
            ES3Settings settings = new ES3Settings(saveFilePath)
            {
                encryptionType = protectionSettings.IsEncryptionEnabled
                    ? ES3.EncryptionType.AES
                    : ES3.EncryptionType.None,
                compressionType = protectionSettings.IsCompressionEnabled
                    ? ES3.CompressionType.Gzip
                    : ES3.CompressionType.None,
                encryptionPassword = protectionSettings.EncryptionPassword,
            };
            return settings;
        }

        #endregion
    }
}
