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
                GameVersion = "development",
                SaveKind = SaveFileKind.Manual,
            };
            saveDataStore.DirectoryExists(string.Empty).Returns(true);
            saveDataStore.GetFiles(string.Empty).Returns(new[] { "ManualSave_1" });
            saveDataStore.KeyExists(SaveFileCatalog.METADATA_KEY, "ManualSave_1").Returns(true);
            saveDataStore.LoadMetadata("ManualSave_1").Returns(metadata);
            var saveFileCatalog = new SaveFileCatalog(saveDataStore);

            var managedFiles = saveFileCatalog.GetManagedSaveFiles();

            Assert.That(managedFiles, Has.Count.EqualTo(1));
            Assert.That(managedFiles[0].Metadata, Is.SameAs(metadata));
            saveDataStore.Received(1).LoadMetadata("ManualSave_1");
        }

    }
}
