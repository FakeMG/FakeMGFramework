using System;
using System.Collections.Generic;
using System.IO;
using FakeMG.SaveLoad;
using NUnit.Framework;

namespace FakeMG.SaveLoad.Tests
{
    /// <summary>
    /// Verifies that the ES3 adapter serializes save content without owning atomic replacement or
    /// backup files.
    /// </summary>
    public sealed class Es3SaveDataStoreTests
    {
        private string _temporaryFilePath;

        [SetUp]
        public void SetUp()
        {
            _temporaryFilePath = Path.Combine(
                Path.GetTempPath(),
                $"FakeMGEs3SaveDataStoreTests_{Guid.NewGuid():N}.es3");
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_temporaryFilePath))
            {
                File.Delete(_temporaryFilePath);
            }
        }

        [Test]
        public void WriteSaveFile_MetadataAndState_CanBeReadBack()
        {
            var saveDataStore = new Es3SaveDataStore();
            var metadata = new SaveMetadata
            {
                TimestampUtc = new DateTime(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc),
                GameVersion = "1.0.0",
                SaveKind = SaveFileKind.Fixed,
            };
            var capturedStates = new Dictionary<string, object>
            {
                ["Score"] = 42,
            };

            saveDataStore.WriteSaveFile(_temporaryFilePath, metadata, capturedStates);

            Assert.That(
                saveDataStore.LoadMetadata(_temporaryFilePath).GameVersion,
                Is.EqualTo("1.0.0"));
            Assert.That(saveDataStore.LoadState("Score", _temporaryFilePath), Is.EqualTo(42));
            Assert.That(
                File.Exists(AtomicFileTransactionPaths.GetBackupPath(_temporaryFilePath)),
                Is.False);
        }
    }
}
