using System;
using System.Collections.Generic;
using System.Threading;
using System.Text.RegularExpressions;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FakeMG.SaveLoad.Tests
{
    public sealed class WorldSaveManagerTests
    {
        private ResumeOrCreateWorldStartupPolicySO _startupPolicySO;

        [SetUp]
        public void SetUp()
        {
            _startupPolicySO = ScriptableObject.CreateInstance<ResumeOrCreateWorldStartupPolicySO>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_startupPolicySO);
        }

        [Test]
        public async System.Threading.Tasks.Task CreateWorldAsync_ValidName_WritesManifestAndInitialAutoSnapshot()
        {
            var timestampUtc = new DateTime(638900000000000000, DateTimeKind.Utc);
            ISaveDataStore saveDataStore = Substitute.For<ISaveDataStore>();
            IAtomicFileTransaction atomicFileTransaction = CreateCommittingTransaction();
            ISaveTimeProvider saveTimeProvider = Substitute.For<ISaveTimeProvider>();
            saveTimeProvider.GetUtcNow().Returns(timestampUtc);
            using WorldSaveManager worldSaveManager = CreateManager(
                saveDataStore,
                atomicFileTransaction,
                saveTimeProvider,
                new TestSaveable());

            WorldCreationResult result = await worldSaveManager.CreateWorldAsync(
                "My World",
                CancellationToken.None);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(WorldId.TryParse(result.World.WorldId, out _), Is.True);
            Assert.That(worldSaveManager.ActiveWorldId, Is.EqualTo(result.World.WorldId));
            saveDataStore.Received(1).WriteSaveFile(
                Arg.Is<string>(path => path.EndsWith("world.json.tmp")),
                Arg.Is<SaveMetadata>(metadata => metadata.SaveKind == SaveFileKind.WorldManifest),
                Arg.Any<IReadOnlyDictionary<string, object>>());
            saveDataStore.Received(1).WriteSaveFile(
                Arg.Is<string>(path => path.Contains("autosave_") && path.EndsWith(".sav.tmp")),
                Arg.Is<SaveMetadata>(metadata => metadata.SaveKind == SaveFileKind.Auto),
                Arg.Any<IReadOnlyDictionary<string, object>>());
            saveDataStore.DidNotReceive().WriteSaveFile(
                Arg.Any<string>(),
                Arg.Is<SaveMetadata>(metadata => metadata.SaveKind == SaveFileKind.Manual),
                Arg.Any<IReadOnlyDictionary<string, object>>());
        }

        [Test]
        public async System.Threading.Tasks.Task SaveManualAsync_ExplicitRequest_WritesDistinctManualSnapshot()
        {
            ISaveDataStore saveDataStore = Substitute.For<ISaveDataStore>();
            IAtomicFileTransaction atomicFileTransaction = CreateCommittingTransaction();
            ISaveTimeProvider saveTimeProvider = Substitute.For<ISaveTimeProvider>();
            saveTimeProvider.GetUtcNow().Returns(new DateTime(638900000000000000, DateTimeKind.Utc));
            using WorldSaveManager worldSaveManager = CreateManager(
                saveDataStore,
                atomicFileTransaction,
                saveTimeProvider,
                new TestSaveable());
            WorldCreationResult creationResult = await worldSaveManager.CreateWorldAsync(
                "World",
                CancellationToken.None);

            WorldSaveResult saveResult = await worldSaveManager.SaveManualAsync(CancellationToken.None);

            Assert.That(creationResult.Succeeded, Is.True);
            Assert.That(saveResult.Succeeded, Is.True);
            saveDataStore.Received(1).WriteSaveFile(
                Arg.Is<string>(path => path.Contains("manual_") && path.EndsWith(".sav.tmp")),
                Arg.Is<SaveMetadata>(metadata => metadata.SaveKind == SaveFileKind.Manual),
                Arg.Any<IReadOnlyDictionary<string, object>>());
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("invalid-world")]
        public async System.Threading.Tasks.Task OpenWorldAsync_InvalidId_IsRejectedWithoutStorageAccess(string worldId)
        {
            ISaveDataStore store = Substitute.For<ISaveDataStore>();
            using WorldSaveManager manager = CreateManager(
                store, CreateCommittingTransaction(), Substitute.For<ISaveTimeProvider>(), new TestSaveable());
            WorldOperationResult result = await manager.OpenWorldAsync(worldId);
            Assert.That(result.Status, Is.EqualTo(WorldOperationStatus.Rejected));
            Assert.That(result.FailureReason, Does.Contain("Invalid world ID"));
            Assert.That(store.ReceivedCalls(), Is.Empty);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("invalid-world")]
        public async System.Threading.Tasks.Task DeleteWorldAsync_InvalidId_IsRejectedWithoutStorageAccess(string worldId)
        {
            ISaveDataStore store = Substitute.For<ISaveDataStore>();
            using WorldSaveManager manager = CreateManager(
                store, CreateCommittingTransaction(), Substitute.For<ISaveTimeProvider>(), new TestSaveable());
            WorldOperationResult result = await manager.DeleteWorldAsync(worldId);
            Assert.That(result.Status, Is.EqualTo(WorldOperationStatus.Rejected));
            Assert.That(result.FailureReason, Does.Contain("Invalid world ID"));
            Assert.That(store.ReceivedCalls(), Is.Empty);
        }

        [Test]
        public async System.Threading.Tasks.Task CreateWorldAsync_CaptureFault_LogsExceptionAndPreservesFailureResult()
        {
            ISaveable saveable = Substitute.For<ISaveable>();
            saveable.SaveId.Returns("faulting-saveable");
            saveable.CaptureState().Returns(_ => throw new InvalidOperationException("capture regression failure"));
            using WorldSaveManager manager = CreateManager(
                Substitute.For<ISaveDataStore>(), CreateCommittingTransaction(), Substitute.For<ISaveTimeProvider>(), saveable);
            LogAssert.Expect(LogType.Error, new Regex("InvalidOperationException: capture regression failure"));
            WorldCreationResult result = await manager.CreateWorldAsync("New world");
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FailureReason, Does.Contain("capture regression failure"));
            Assert.That(manager.HasActiveWorld, Is.False);
        }

        private WorldSaveManager CreateManager(
            ISaveDataStore saveDataStore,
            IAtomicFileTransaction atomicFileTransaction,
            ISaveTimeProvider saveTimeProvider,
            ISaveable saveable)
        {
            var saveDataStoreFactory = new TestSaveDataStoreFactory(saveDataStore);
            var saveFileService = new SaveFileService(
                saveDataStoreFactory,
                atomicFileTransaction,
                Substitute.For<ISaveMigrationPlan>(),
                new TestSaveEnvironment(),
                new SaveMetadataValidator());
            var worldSaveCatalog = new WorldSaveCatalog(
                saveDataStoreFactory,
                SaveFileProtectionSettings.Plain);
            var repository = new WorldSaveRepository(
                saveFileService,
                worldSaveCatalog,
                saveDataStoreFactory,
                SaveFileProtectionSettings.Plain);
            var configuration = new WorldSaveConfiguration(
                5,
                300f,
                10f,
                true,
                "World",
                _startupPolicySO);
            return new WorldSaveManager(
                saveFileService,
                repository,
                new WorldSnapshotRetentionPolicy(),
                saveTimeProvider,
                configuration,
                new AsyncSaveParticipantDependencyOrderResolver(),
                new[] { saveable });
        }

        private static IAtomicFileTransaction CreateCommittingTransaction()
        {
            IAtomicFileTransaction atomicFileTransaction = Substitute.For<IAtomicFileTransaction>();
            atomicFileTransaction
                .When(transaction => transaction.Commit(Arg.Any<string>(), Arg.Any<Action<string>>()))
                .Do(call => call.ArgAt<Action<string>>(1)(call.ArgAt<string>(0) + ".tmp"));
            return atomicFileTransaction;
        }

        private sealed class TestSaveable : ISaveable
        {
            public string SaveId => "test";

            public object CaptureState()
            {
                return 42;
            }

            public bool TryValidateState(object state, out string failureReason)
            {
                failureReason = state is int ? string.Empty : "State must be an integer.";
                return state is int;
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
