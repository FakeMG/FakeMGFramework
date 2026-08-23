using System;
using FakeMG.SaveLoad;
using NSubstitute;
using NUnit.Framework;

namespace FakeMG.SaveLoad.Tests
{
    /// <summary>
    /// Verifies that managed-file discovery uses the storage boundary and therefore does not require
    /// ES3 behavior inside the catalog itself.
    /// </summary>
    public sealed class SaveFileCatalogTests
    {
        [Test]
        public void GetManagedSaveFiles_ConfiguredStore_ReturnsStoreMetadata()
        {
            ISaveDataStore saveDataStore = Substitute.For<ISaveDataStore>();
            var metadata = new SaveMetadata
            {
                TimestampUtc = new DateTime(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc),
                ApplicationVersion = "development",
                SaveKind = SaveFileKind.GlobalDocument,
                OwnerId = "settings",
            };
            saveDataStore.DirectoryExists(string.Empty).Returns(true);
            saveDataStore.GetFiles(string.Empty).Returns(new[] { "settings.json" });
            saveDataStore.FileExists("settings.json").Returns(true);
            saveDataStore.KeyExists(SaveFileCatalog.METADATA_KEY, "settings.json").Returns(true);
            saveDataStore.LoadMetadata("settings.json").Returns(metadata);
            var saveFileCatalog = new SaveFileCatalog(saveDataStore);

            var managedFiles = saveFileCatalog.GetManagedSaveFiles();

            Assert.That(managedFiles, Has.Count.EqualTo(1));
            Assert.That(managedFiles[0].OwnerId, Is.EqualTo(metadata.OwnerId));
            Assert.That(managedFiles[0].SaveKind, Is.EqualTo(metadata.SaveKind));
            Assert.That(managedFiles[0].TimestampUtc, Is.EqualTo(metadata.TimestampUtc));
            Assert.That(managedFiles[0].ApplicationVersion, Is.EqualTo(metadata.ApplicationVersion));
            saveDataStore.Received(1).LoadMetadata("settings.json");
        }

        [Test]
        public void GetManagedSaveFiles_LegacyAndTransactionFiles_ExcludesBoth()
        {
            ISaveDataStore saveDataStore = Substitute.For<ISaveDataStore>();
            saveDataStore.DirectoryExists(string.Empty).Returns(true);
            saveDataStore.GetFiles(string.Empty).Returns(new[]
            {
                "Settings",
                "settings.json.bak",
                "settings.json.tmp",
            });
            var saveFileCatalog = new SaveFileCatalog(saveDataStore);

            var managedFiles = saveFileCatalog.GetManagedSaveFiles();

            Assert.That(managedFiles, Is.Empty);
        }

        [Test]
        public void DiscoverManagedSaveFiles_FutureVersion_ReturnsUnsupportedVersionDiagnostic()
        {
            ISaveDataStore saveDataStore = Substitute.For<ISaveDataStore>();
            saveDataStore.DirectoryExists(string.Empty).Returns(true);
            saveDataStore.GetFiles(string.Empty).Returns(new[] { "settings.json" });
            saveDataStore.FileExists("settings.json").Returns(true);
            saveDataStore.KeyExists(SaveFileCatalog.METADATA_KEY, "settings.json").Returns(true);
            saveDataStore.LoadMetadata("settings.json").Returns(new SaveMetadata
            {
                TimestampUtc = DateTime.UtcNow,
                ApplicationVersion = "2.0.0",
                SaveKind = SaveFileKind.GlobalDocument,
                OwnerId = "settings",
            });
            var saveFileCatalog = new SaveFileCatalog(
                saveDataStore,
                saveEnvironment: new TestSaveEnvironment(applicationVersion: "1.0.0"));

            SaveCatalogDiscoveryResult result = saveFileCatalog.DiscoverManagedSaveFiles();

            Assert.That(result.Files, Is.Empty);
            Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(
                result.Diagnostics[0].Reason,
                Is.EqualTo(SaveCatalogRejectionReason.UnsupportedVersion));
        }

    }
}
