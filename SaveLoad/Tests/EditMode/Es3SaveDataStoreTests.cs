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
            var saveDataStore = new Es3SaveDataStore(SaveFileProtectionSettings.Plain, Path.GetTempPath());
            var metadata = new SaveMetadata
            {
                TimestampUtc = new DateTime(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc),
                ApplicationVersion = "1.0.0",
                SaveKind = SaveFileKind.GlobalDocument,
                OwnerId = "settings",
            };
            var capturedStates = new Dictionary<string, object>
            {
                ["Score"] = 42,
            };

            saveDataStore.WriteSaveFile(_temporaryFilePath, metadata, capturedStates);

            Assert.That(
                saveDataStore.LoadMetadata(_temporaryFilePath).ApplicationVersion,
                Is.EqualTo("1.0.0"));
            Assert.That(saveDataStore.LoadState("Score", _temporaryFilePath), Is.EqualTo(42));
            Assert.That(
                File.Exists(AtomicFileTransactionPaths.GetBackupPath(_temporaryFilePath)),
                Is.False);
        }

        [Test]
        public void WriteSaveFile_ReplacesCompletePayloadWithoutKeepingRemovedKeys()
        {
            var saveDataStore = new Es3SaveDataStore(SaveFileProtectionSettings.Plain, Path.GetTempPath());
            var metadata = new SaveMetadata
            {
                TimestampUtc = DateTime.UtcNow,
                ApplicationVersion = "1.0.0",
                SaveKind = SaveFileKind.GlobalDocument,
                OwnerId = "settings",
            };
            saveDataStore.WriteSaveFile(_temporaryFilePath, metadata, new Dictionary<string, object> { ["Removed"] = 1 });

            saveDataStore.WriteSaveFile(_temporaryFilePath, metadata, new Dictionary<string, object>
            {
                ["Score"] = 42,
                ["Mission"] = "claimed",
                ["Rewards"] = new[] { 100, 25 },
            });

            Assert.That(saveDataStore.KeyExists("Removed", _temporaryFilePath), Is.False);
            Assert.That(saveDataStore.LoadState("Score", _temporaryFilePath), Is.EqualTo(42));
            Assert.That(saveDataStore.LoadState("Mission", _temporaryFilePath), Is.EqualTo("claimed"));
            Assert.That(saveDataStore.LoadState("Rewards", _temporaryFilePath), Is.EqualTo(new[] { 100, 25 }));
        }
    }
}
