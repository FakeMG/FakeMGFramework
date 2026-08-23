using System;
using System.Text.RegularExpressions;
using System.Threading;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FakeMG.SaveLoad.Tests
{
    public sealed class WorldSaveRepositoryTests
    {
        [Test]
        public async System.Threading.Tasks.Task LoadManifestAsync_CorruptCanonicalWithValidBackup_LoadsAndPromotesBackup()
        {
            const string WORLD_ID = "world_aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            string manifestFilePath = SaveFileCatalog.CreateWorldManifestFilePath(WORLD_ID);
            string backupFilePath = AtomicFileTransactionPaths.GetBackupPath(manifestFilePath);
            ISaveDataStore saveDataStore = Substitute.For<ISaveDataStore>();
            IAtomicFileTransaction atomicFileTransaction = Substitute.For<IAtomicFileTransaction>();
            ISaveMigrationPlan migrationPlan = Substitute.For<ISaveMigrationPlan>();
            var saveEnvironment = new TestSaveEnvironment();
            var saveDataStoreFactory = new TestSaveDataStoreFactory(saveDataStore);
            SaveMetadata metadata = CreateMetadata(WORLD_ID);
            saveDataStore.FileExists(manifestFilePath).Returns(true);
            saveDataStore.FileExists(backupFilePath).Returns(true);
            saveDataStore.LoadMetadata(manifestFilePath).Returns(metadata);
            saveDataStore.LoadMetadata(backupFilePath).Returns(metadata);
            saveDataStore.KeyExists(SaveFileCatalog.WORLD_MANIFEST_KEY, manifestFilePath).Returns(true);
            saveDataStore.KeyExists(SaveFileCatalog.WORLD_MANIFEST_KEY, backupFilePath).Returns(true);
            saveDataStore.LoadState(SaveFileCatalog.WORLD_MANIFEST_KEY, manifestFilePath).Returns(
                new WorldManifest
                {
                    WorldId = WORLD_ID,
                    DisplayName = string.Empty,
                    CreatedTimestampUtc = metadata.TimestampUtc,
                    LastPlayedTimestampUtc = metadata.TimestampUtc,
                });
            var validManifest = new WorldManifest
            {
                WorldId = WORLD_ID,
                DisplayName = "Recovered World",
                CreatedTimestampUtc = metadata.TimestampUtc,
                LastPlayedTimestampUtc = metadata.TimestampUtc,
            };
            saveDataStore.LoadState(SaveFileCatalog.WORLD_MANIFEST_KEY, backupFilePath).Returns(validManifest);
            var saveFileService = new SaveFileService(
                saveDataStoreFactory,
                atomicFileTransaction,
                migrationPlan,
                saveEnvironment,
                new SaveMetadataValidator());
            var worldSaveCatalog = new WorldSaveCatalog(
                saveDataStoreFactory,
                SaveFileProtectionSettings.Plain,
                saveEnvironment);
            var worldSaveRepository = new WorldSaveRepository(
                saveFileService,
                worldSaveCatalog,
                saveDataStoreFactory,
                SaveFileProtectionSettings.Plain);
            LogAssert.Expect(
                LogType.Error,
                new Regex("World display name is required", RegexOptions.Singleline));

            (WorldManifest manifest, SaveFileLoadResult result) =
                await worldSaveRepository.LoadManifestAsync(WORLD_ID, CancellationToken.None);

            Assert.That(result.Status, Is.EqualTo(SaveFileLoadStatus.RecoveredBackup));
            Assert.That(manifest.DisplayName, Is.EqualTo(validManifest.DisplayName));
            atomicFileTransaction.Received(1).PromoteValidatedBackup(
                backupFilePath,
                manifestFilePath);
        }

        private static SaveMetadata CreateMetadata(string worldId)
        {
            return new SaveMetadata
            {
                OwnerId = worldId,
                SaveKind = SaveFileKind.WorldManifest,
                ApplicationVersion = "1.0.0",
                TimestampUtc = new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc),
            };
        }
    }
}
