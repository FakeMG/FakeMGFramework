using System;
using System.IO;
using FakeMG.SaveLoad;
using NUnit.Framework;

namespace FakeMG.SaveLoad.Tests
{
    /// <summary>
    /// Verifies durable replacement independently from the serializer, including retained backup and
    /// temporary-file cleanup under an injected test-only storage root.
    /// </summary>
    public sealed class AtomicFileTransactionTests
    {
        private string _temporaryStorageRootPath;

        [SetUp]
        public void SetUp()
        {
            _temporaryStorageRootPath = Path.Combine(
                Path.GetTempPath(),
                "FakeMGAtomicFileTransactionTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_temporaryStorageRootPath);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_temporaryStorageRootPath))
            {
                Directory.Delete(_temporaryStorageRootPath, true);
            }
        }

        [Test]
        public void Commit_ExistingCanonicalFile_ReplacesCurrentAndRetainsBackup()
        {
            var transaction = new AtomicFileTransaction(_temporaryStorageRootPath);
            const string RELATIVE_FILE_PATH = "Worlds/test/world.es3";

            transaction.Commit(RELATIVE_FILE_PATH, path => File.WriteAllText(path, "first"));
            transaction.Commit(RELATIVE_FILE_PATH, path => File.WriteAllText(path, "second"));

            string canonicalFilePath = Path.Combine(_temporaryStorageRootPath, RELATIVE_FILE_PATH);
            Assert.That(File.ReadAllText(canonicalFilePath), Is.EqualTo("second"));
            Assert.That(
                File.ReadAllText(AtomicFileTransactionPaths.GetBackupPath(canonicalFilePath)),
                Is.EqualTo("first"));
            Assert.That(
                File.Exists(AtomicFileTransactionPaths.GetTemporaryPath(canonicalFilePath)),
                Is.False);
        }
    }
}
