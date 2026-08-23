using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FakeMG.SaveLoad.Tests
{
    public sealed class SaveFileServiceTests
    {
        [Test]
        public async System.Threading.Tasks.Task LoadAsync_CrossOwnerMetadata_RejectsWithoutMutatingState()
        {
            const string REQUESTED_WORLD_ID = "world_aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            const string STORED_WORLD_ID = "world_bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
            const string SNAPSHOT_FILE_PATH =
                "Saves/world_aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa/manual_1.sav";
            ISaveDataStore saveDataStore = Substitute.For<ISaveDataStore>();
            IAtomicFileTransaction atomicFileTransaction = Substitute.For<IAtomicFileTransaction>();
            ISaveMigrationPlan migrationPlan = Substitute.For<ISaveMigrationPlan>();
            var saveable = new TestSaveable();
            var saveables = new Dictionary<string, ISaveable> { [saveable.SaveId] = saveable };
            saveDataStore.FileExists(SNAPSHOT_FILE_PATH).Returns(true);
            saveDataStore.LoadMetadata(SNAPSHOT_FILE_PATH).Returns(new SaveMetadata
            {
                ApplicationVersion = "1.0.0",
                OwnerId = STORED_WORLD_ID,
                SaveKind = SaveFileKind.Manual,
            });
            var saveFileService = new SaveFileService(
                new TestSaveDataStoreFactory(saveDataStore),
                atomicFileTransaction,
                migrationPlan,
                new TestSaveEnvironment(),
                new SaveMetadataValidator());
            LogAssert.Expect(
                LogType.Error,
                new Regex($"Save owner '{STORED_WORLD_ID}' does not match '{REQUESTED_WORLD_ID}'", RegexOptions.Singleline));
            LogAssert.Expect(
                LogType.Error,
                new Regex($"No valid candidate could load '{Regex.Escape(SNAPSHOT_FILE_PATH)}'", RegexOptions.Singleline));

            SaveFileLoadResult result = await saveFileService.LoadAsync(
                new SaveFileDescriptor(
                    SNAPSHOT_FILE_PATH,
                    REQUESTED_WORLD_ID,
                    SaveFileKind.Manual,
                    SaveFileProtectionSettings.Plain),
                saveables,
                Array.Empty<IAsyncSaveParticipant>(),
                false,
                false,
                CancellationToken.None);

            Assert.That(result.Status, Is.EqualTo(SaveFileLoadStatus.Rejected));
            Assert.That(saveable.RestoreStateCallCount, Is.Zero);
            Assert.That(saveable.RestoreDefaultCallCount, Is.Zero);
        }

        [Test]
        public async System.Threading.Tasks.Task LoadAsync_MissingCanonicalWithValidBackup_PromotesAndLoadsBackup()
        {
            const string saveFilePath = "settings.json";
            string backupFilePath = AtomicFileTransactionPaths.GetBackupPath(saveFilePath);
            ISaveDataStore saveDataStore = Substitute.For<ISaveDataStore>();
            IAtomicFileTransaction atomicFileTransaction = Substitute.For<IAtomicFileTransaction>();
            ISaveMigrationPlan migrationPlan = Substitute.For<ISaveMigrationPlan>();
            var saveable = new TestSaveable();
            var saveables = new Dictionary<string, ISaveable> { [saveable.SaveId] = saveable };
            saveDataStore.FileExists(saveFilePath).Returns(false);
            saveDataStore.FileExists(backupFilePath).Returns(true);
            saveDataStore.LoadMetadata(backupFilePath).Returns(new SaveMetadata
            {
                ApplicationVersion = "1.0.0",
                OwnerId = "settings",
                SaveKind = SaveFileKind.GlobalDocument,
            });
            saveDataStore.KeyExists(saveable.SaveId, backupFilePath).Returns(true);
            saveDataStore.LoadState(saveable.SaveId, backupFilePath).Returns((object)null);
            var saveFileService = new SaveFileService(
                new TestSaveDataStoreFactory(saveDataStore),
                atomicFileTransaction,
                migrationPlan,
                new TestSaveEnvironment(),
                new SaveMetadataValidator());

            SaveFileLoadResult result = await saveFileService.LoadAsync(
                new SaveFileDescriptor(
                    saveFilePath,
                    "settings",
                    SaveFileKind.GlobalDocument,
                    SaveFileProtectionSettings.Plain),
                saveables,
                Array.Empty<IAsyncSaveParticipant>(),
                true,
                true,
                CancellationToken.None);

            Assert.That(result.Status, Is.EqualTo(SaveFileLoadStatus.RecoveredBackup));
            Assert.That(saveable.RestoreStateCallCount, Is.EqualTo(1), "A deliberately saved null is still saved state.");
            Assert.That(saveable.RestoreDefaultCallCount, Is.Zero);
            atomicFileTransaction.Received(1).PromoteValidatedBackup(backupFilePath, saveFilePath);
        }

        [Test]
        public async System.Threading.Tasks.Task LoadAsync_SecondStateThrows_DoesNotPartiallyMutateLiveState()
        {
            const string saveFilePath = "settings.json";
            ISaveDataStore saveDataStore = Substitute.For<ISaveDataStore>();
            IAtomicFileTransaction atomicFileTransaction = Substitute.For<IAtomicFileTransaction>();
            ISaveMigrationPlan migrationPlan = Substitute.For<ISaveMigrationPlan>();
            var firstSaveable = new TestSaveable("first");
            var secondSaveable = new TestSaveable("second");
            var saveables = new Dictionary<string, ISaveable>
            {
                [firstSaveable.SaveId] = firstSaveable,
                [secondSaveable.SaveId] = secondSaveable,
            };
            saveDataStore.FileExists(saveFilePath).Returns(true);
            saveDataStore.LoadMetadata(saveFilePath).Returns(new SaveMetadata
            {
                ApplicationVersion = "1.0.0",
                OwnerId = "settings",
                SaveKind = SaveFileKind.GlobalDocument,
            });
            saveDataStore.KeyExists(Arg.Any<string>(), saveFilePath).Returns(true);
            saveDataStore.LoadState("first", saveFilePath).Returns(1);
            saveDataStore.LoadState("second", saveFilePath).Returns(_ => throw new InvalidOperationException("corrupt"));
            var saveFileService = new SaveFileService(
                new TestSaveDataStoreFactory(saveDataStore),
                atomicFileTransaction,
                migrationPlan,
                new TestSaveEnvironment(),
                new SaveMetadataValidator());
            LogAssert.Expect(LogType.Error, new Regex("Failed to load candidate 'settings\\.json'.*corrupt", RegexOptions.Singleline));
            LogAssert.Expect(LogType.Error, new Regex("No valid candidate could load 'settings\\.json'.*corrupt", RegexOptions.Singleline));

            SaveFileLoadResult result = await saveFileService.LoadAsync(
                new SaveFileDescriptor(
                    saveFilePath,
                    "settings",
                    SaveFileKind.GlobalDocument,
                    SaveFileProtectionSettings.Plain),
                saveables,
                Array.Empty<IAsyncSaveParticipant>(),
                false,
                false,
                CancellationToken.None);

            Assert.That(result.Status, Is.EqualTo(SaveFileLoadStatus.Failed));
            Assert.That(firstSaveable.RestoreStateCallCount, Is.Zero);
            Assert.That(secondSaveable.RestoreStateCallCount, Is.Zero);
        }

        [Test]
        public async System.Threading.Tasks.Task LoadAsync_CancelledParticipantApplication_RollsBackWithIndependentToken()
        {
            const string SAVE_FILE_PATH = "settings.json";
            ISaveDataStore saveDataStore = Substitute.For<ISaveDataStore>();
            IAtomicFileTransaction atomicFileTransaction = Substitute.For<IAtomicFileTransaction>();
            ISaveMigrationPlan migrationPlan = Substitute.For<ISaveMigrationPlan>();
            var saveable = new TestSaveable();
            var saveables = new Dictionary<string, ISaveable> { [saveable.SaveId] = saveable };
            using var callerCancellationSource = new CancellationTokenSource();
            var appliedParticipant = new TestAsyncSaveParticipant("applied");
            var cancellingParticipant = new TestAsyncSaveParticipant(
                "cancelling",
                callerCancellationSource);
            saveDataStore.FileExists(SAVE_FILE_PATH).Returns(true);
            saveDataStore.LoadMetadata(SAVE_FILE_PATH).Returns(new SaveMetadata
            {
                ApplicationVersion = "1.0.0",
                OwnerId = "settings",
                SaveKind = SaveFileKind.GlobalDocument,
            });
            saveDataStore.KeyExists(saveable.SaveId, SAVE_FILE_PATH).Returns(true);
            saveDataStore.LoadState(saveable.SaveId, SAVE_FILE_PATH).Returns(2);
            var saveFileService = new SaveFileService(
                new TestSaveDataStoreFactory(saveDataStore),
                atomicFileTransaction,
                migrationPlan,
                new TestSaveEnvironment(),
                new SaveMetadataValidator());

            SaveFileLoadResult result = await saveFileService.LoadAsync(
                new SaveFileDescriptor(
                    SAVE_FILE_PATH,
                    "settings",
                    SaveFileKind.GlobalDocument,
                    SaveFileProtectionSettings.Plain),
                saveables,
                new IAsyncSaveParticipant[] { appliedParticipant, cancellingParticipant },
                false,
                false,
                callerCancellationSource.Token);

            Assert.That(result.Status, Is.EqualTo(SaveFileLoadStatus.Cancelled));
            Assert.That(appliedParticipant.RollbackCallCount, Is.EqualTo(1));
            Assert.That(appliedParticipant.WasRollbackTokenCancelled, Is.False);
            Assert.That(saveable.RestoreStateCallCount, Is.EqualTo(2));
        }

        private sealed class TestSaveable : ISaveable
        {
            private readonly string _uniqueId;

            public int RestoreStateCallCount { get; private set; }
            public int RestoreDefaultCallCount { get; private set; }
            public string SaveId => _uniqueId;

            public TestSaveable(string uniqueId = "test")
            {
                _uniqueId = uniqueId;
            }

            public bool TryValidateState(object state, out string failureReason)
            {
                failureReason = string.Empty;
                return true;
            }

            public object CaptureState()
            {
                return 1;
            }

            public void RestoreState(object state)
            {
                RestoreStateCallCount++;
            }

            public void RestoreDefaultState()
            {
                RestoreDefaultCallCount++;
            }
        }

        private sealed class TestAsyncSaveParticipant : IAsyncSaveParticipant
        {
            private readonly CancellationTokenSource _cancellationSource;

            public string ParticipantId { get; }
            public IReadOnlyCollection<string> RunsAfterParticipantIds => Array.Empty<string>();
            public int RollbackCallCount { get; private set; }
            public bool WasRollbackTokenCancelled { get; private set; }

            public TestAsyncSaveParticipant(
                string participantId,
                CancellationTokenSource cancellationSource = null)
            {
                ParticipantId = participantId;
                _cancellationSource = cancellationSource;
            }

            public UniTask PrepareSaveAsync(
                SaveOperationContext context,
                CancellationToken cancellationToken)
            {
                return UniTask.CompletedTask;
            }

            public UniTask ApplyLoadedStateAsync(
                LoadOperationContext context,
                CancellationToken cancellationToken)
            {
                if (_cancellationSource == null)
                {
                    return UniTask.CompletedTask;
                }

                _cancellationSource.Cancel();
                return UniTask.FromCanceled(_cancellationSource.Token);
            }

            public UniTask RollBackLoadedStateAsync(
                LoadOperationContext context,
                CancellationToken cancellationToken)
            {
                RollbackCallCount++;
                WasRollbackTokenCancelled = cancellationToken.IsCancellationRequested;
                return UniTask.CompletedTask;
            }

            public UniTask CompleteSaveAsync(
                SaveOperationContext context,
                bool didMetadataCommit,
                CancellationToken cancellationToken)
            {
                return UniTask.CompletedTask;
            }
        }
    }
}
