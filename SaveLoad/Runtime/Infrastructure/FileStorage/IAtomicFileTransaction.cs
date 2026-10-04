using System;

namespace FakeMG.SaveLoad
{
    /// <summary>
    /// Defines durable file commit and validated-backup promotion independently from serialization.
    /// The supplied writer creates the complete temporary payload before replacement begins.
    /// </summary>
    public interface IAtomicFileTransaction
    {
        void Commit(string canonicalFilePath, Action<string> writeTemporaryFile);
        void PromoteValidatedBackup(string backupFilePath, string canonicalFilePath);
    }
}
