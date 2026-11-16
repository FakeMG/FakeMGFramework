namespace FakeMG.SaveLoad
{
    public static class AtomicFileTransactionPaths
    {
        private const string BACKUP_SUFFIX = ".bak";
        private const string TEMPORARY_SUFFIX = ".tmp";
        private const string RECOVERY_TEMPORARY_SUFFIX = ".recovery.tmp";

        #region Public Methods

        public static string GetBackupPath(string canonicalFilePath)
        {
            return canonicalFilePath + BACKUP_SUFFIX;
        }

        public static string GetTemporaryPath(string canonicalFilePath)
        {
            return canonicalFilePath + TEMPORARY_SUFFIX;
        }

        public static string GetRecoveryTemporaryPath(string canonicalFilePath)
        {
            return canonicalFilePath + RECOVERY_TEMPORARY_SUFFIX;
        }

        #endregion
    }
}
