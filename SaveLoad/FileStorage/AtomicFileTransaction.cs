using System;
using System.IO;

namespace FakeMG.SaveLoad
{
    /// <summary>
    /// Owns crash-resistant file durability. It flushes complete temporary payloads, atomically
    /// replaces canonical files while retaining one backup, and promotes only validated backups.
    /// </summary>
    public sealed class AtomicFileTransaction : IAtomicFileTransaction
    {
        private readonly string _storageRootPath;

        public AtomicFileTransaction(ISaveEnvironment saveEnvironment)
        {
            _storageRootPath = saveEnvironment?.StorageRootPath ?? throw new ArgumentNullException(nameof(saveEnvironment));
        }

        public AtomicFileTransaction(string storageRootPath)
        {
            _storageRootPath = storageRootPath ?? throw new ArgumentNullException(nameof(storageRootPath));
        }

        #region Public Methods

        public void Commit(string canonicalFilePath, Action<string> writeTemporaryFile)
        {
            string absoluteCanonicalFilePath = GetAbsolutePath(canonicalFilePath);
            Directory.CreateDirectory(Path.GetDirectoryName(absoluteCanonicalFilePath));
            string temporaryFilePath = AtomicFileTransactionPaths.GetTemporaryPath(absoluteCanonicalFilePath);
            DeleteFileIfPresent(temporaryFilePath);

            try
            {
                writeTemporaryFile(temporaryFilePath);
                FlushFileToDisk(temporaryFilePath);
                ReplaceCanonical(temporaryFilePath, absoluteCanonicalFilePath);
            }
            catch
            {
                DeleteFileIfPresent(temporaryFilePath);
                throw;
            }
        }

        public void PromoteValidatedBackup(string backupFilePath, string canonicalFilePath)
        {
            string absoluteBackupFilePath = GetAbsolutePath(backupFilePath);
            string absoluteCanonicalFilePath = GetAbsolutePath(canonicalFilePath);
            string recoveryFilePath = AtomicFileTransactionPaths.GetRecoveryTemporaryPath(absoluteCanonicalFilePath);
            File.Copy(absoluteBackupFilePath, recoveryFilePath, true);
            FlushFileToDisk(recoveryFilePath);
            if (File.Exists(absoluteCanonicalFilePath))
            {
                File.Replace(recoveryFilePath, absoluteCanonicalFilePath, null, true);
                return;
            }

            File.Move(recoveryFilePath, absoluteCanonicalFilePath);
        }

        #endregion

        #region Private Methods

        private static void FlushFileToDisk(string absoluteFilePath)
        {
            using var stream = new FileStream(absoluteFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            stream.Flush(true);
        }

        private static void ReplaceCanonical(string temporaryFilePath, string canonicalFilePath)
        {
            string backupFilePath = AtomicFileTransactionPaths.GetBackupPath(canonicalFilePath);
            if (!File.Exists(canonicalFilePath))
            {
                File.Move(temporaryFilePath, canonicalFilePath);
                return;
            }

            DeleteFileIfPresent(backupFilePath);
            File.Replace(temporaryFilePath, canonicalFilePath, backupFilePath, true);
        }

        private static void DeleteFileIfPresent(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        private string GetAbsolutePath(string filePath)
        {
            return Path.IsPathRooted(filePath)
                ? filePath
                : Path.Combine(_storageRootPath, filePath);
        }

        #endregion
    }
}
