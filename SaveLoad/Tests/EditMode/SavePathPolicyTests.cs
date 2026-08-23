using System;
using NUnit.Framework;

namespace FakeMG.SaveLoad.Tests
{
    public sealed class SavePathPolicyTests
    {
        [Test]
        public void CreateGlobalSaveFilePath_RootJson_ReturnsUnchangedPath()
        {
            string saveFilePath = SaveFileCatalog.CreateGlobalSaveFilePath("settings.json");

            Assert.That(saveFilePath, Is.EqualTo("settings.json"));
        }

        [Test]
        public void CreateGlobalSaveFilePath_WorldDirectory_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                SaveFileCatalog.CreateGlobalSaveFilePath("Saves/world/settings.json"));
        }

        [Test]
        public void CreateWorldSnapshotFilePath_KnownTimestamp_UsesWorldFolderAndSavExtension()
        {
            string worldId = SaveFileCatalog.CreateWorldId(
                Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
            var timestampUtc = new DateTime(638900000000000000, DateTimeKind.Utc);

            string saveFilePath = SaveFileCatalog.CreateWorldSnapshotFilePath(
                worldId,
                SaveFileKind.Auto,
                timestampUtc);

            Assert.That(
                saveFilePath,
                Is.EqualTo($"Saves/{worldId}/autosave_{timestampUtc.Ticks}.sav"));
        }

        [Test]
        public void CreateWorldDirectoryPath_TraversalWorldId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                SaveFileCatalog.CreateWorldDirectoryPath("world_../../settings"));
        }

        [TestCase("manual_latest.sav")]
        [TestCase("autosave_123.SAV")]
        [TestCase("Manual_123.sav")]
        [TestCase("AutoSave_123.sav")]
        [TestCase("manual_9223372036854775807.sav")]
        public void IsWorldSnapshotPath_MalformedOrLegacyName_ReturnsFalse(string snapshotFileName)
        {
            Assert.That(SaveFileCatalog.IsWorldSnapshotPath(snapshotFileName), Is.False);
        }

        [Test]
        public void GetSnapshotFilePath_CrossWorldPath_ThrowsArgumentException()
        {
            string worldId = SaveFileCatalog.CreateWorldId(Guid.NewGuid());
            var saveDataStore = NSubstitute.Substitute.For<ISaveDataStore>();
            var catalog = new WorldSaveCatalog(
                new TestSaveDataStoreFactory(saveDataStore),
                SaveFileProtectionSettings.Plain);

            Assert.Throws<ArgumentException>(() =>
                catalog.GetSnapshotFilePath(worldId, "../world.json"));
        }
    }
}
