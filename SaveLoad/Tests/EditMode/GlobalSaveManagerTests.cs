using System;
using System.Collections.Generic;
using System.Threading;
using NSubstitute;
using NUnit.Framework;

namespace FakeMG.SaveLoad.Tests
{
    public sealed class GlobalSaveManagerTests
    {
        [Test]
        public async System.Threading.Tasks.Task SaveAsync_SettingsDocument_WritesRootJsonWithoutWorldDiscovery()
        {
            ISaveDataStore saveDataStore = Substitute.For<ISaveDataStore>();
            IAtomicFileTransaction atomicFileTransaction = Substitute.For<IAtomicFileTransaction>();
            ISaveMigrationPlan migrationPlan = Substitute.For<ISaveMigrationPlan>();
            ISaveTimeProvider saveTimeProvider = Substitute.For<ISaveTimeProvider>();
            saveTimeProvider.GetUtcNow().Returns(new DateTime(638900000000000000, DateTimeKind.Utc));
            atomicFileTransaction
                .When(transaction => transaction.Commit("settings.json", Arg.Any<Action<string>>()))
                .Do(call => call.Arg<Action<string>>()("settings.json.tmp"));
            var saveFileService = new SaveFileService(
                new TestSaveDataStoreFactory(saveDataStore),
                atomicFileTransaction,
                migrationPlan,
                new TestSaveEnvironment(),
                new SaveMetadataValidator());
            var document = new TestGlobalSaveDocument();
            using var globalSaveManager = new GlobalSaveManager(
                saveFileService,
                saveTimeProvider,
                new[] { document });

            GlobalDocumentSaveResult result = await globalSaveManager.SaveAsync("settings", CancellationToken.None);

            Assert.That(result.Succeeded, Is.True);
            saveDataStore.Received(1).WriteSaveFile(
                "settings.json.tmp",
                Arg.Is<SaveMetadata>(metadata =>
                    metadata.OwnerId == "settings" &&
                    metadata.SaveKind == SaveFileKind.GlobalDocument),
                Arg.Any<IReadOnlyDictionary<string, object>>());
            saveDataStore.DidNotReceive().GetDirectories(Arg.Any<string>());
        }

        [Test]
        public async System.Threading.Tasks.Task SaveAsync_TwoDocuments_UsesEachDocumentProtectionProfile()
        {
            ISaveDataStore saveDataStore = Substitute.For<ISaveDataStore>();
            IAtomicFileTransaction atomicFileTransaction = Substitute.For<IAtomicFileTransaction>();
            ISaveMigrationPlan migrationPlan = Substitute.For<ISaveMigrationPlan>();
            ISaveTimeProvider saveTimeProvider = Substitute.For<ISaveTimeProvider>();
            atomicFileTransaction
                .When(transaction => transaction.Commit(Arg.Any<string>(), Arg.Any<Action<string>>()))
                .Do(call => call.ArgAt<Action<string>>(1)(call.ArgAt<string>(0) + ".tmp"));
            var saveDataStoreFactory = new TestSaveDataStoreFactory(saveDataStore);
            var protectedSettings = new SaveFileProtectionSettings(true, false, "protected-password");
            using var globalSaveManager = new GlobalSaveManager(
                new SaveFileService(
                    saveDataStoreFactory,
                    atomicFileTransaction,
                    migrationPlan,
                    new TestSaveEnvironment(),
                    new SaveMetadataValidator()),
                saveTimeProvider,
                new IGlobalSaveDocument[]
                {
                    new TestGlobalSaveDocument("settings", "settings.json", SaveFileProtectionSettings.Plain),
                    new TestGlobalSaveDocument("keybindings", "keybindings.json", protectedSettings),
                });

            await globalSaveManager.SaveAsync("settings", CancellationToken.None);
            await globalSaveManager.SaveAsync("keybindings", CancellationToken.None);

            Assert.That(saveDataStoreFactory.CreatedProtectionSettings, Has.Count.EqualTo(2));
            Assert.That(saveDataStoreFactory.CreatedProtectionSettings[0], Is.SameAs(SaveFileProtectionSettings.Plain));
            Assert.That(saveDataStoreFactory.CreatedProtectionSettings[1], Is.SameAs(protectedSettings));
        }

        private sealed class TestGlobalSaveDocument : IGlobalSaveDocument
        {
            public string DocumentId { get; }
            public string FileName { get; }
            public ISaveDataStoreProfile StorageProfile { get; }
            public string SaveId => "settings-state";

            public TestGlobalSaveDocument(
                string documentId = "settings",
                string fileName = "settings.json",
                SaveFileProtectionSettings protectionSettings = null)
            {
                DocumentId = documentId;
                FileName = fileName;
                StorageProfile = protectionSettings ?? SaveFileProtectionSettings.Plain;
            }

            public bool TryValidateState(object state, out string failureReason)
            {
                failureReason = string.Empty;
                return true;
            }

            public object CaptureState()
            {
                return 42;
            }

            public void RestoreState(object state)
            {
            }

            public void RestoreDefaultState()
            {
            }
        }
    }
}
