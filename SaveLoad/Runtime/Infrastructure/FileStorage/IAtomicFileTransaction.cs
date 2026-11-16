using System;

namespace FakeMG.SaveLoad
{
    public interface IAtomicFileTransaction
    {
        void Commit(string canonicalFilePath, Action<string> writeTemporaryFile);
        void PromoteValidatedBackup(string backupFilePath, string canonicalFilePath);
    }
}
