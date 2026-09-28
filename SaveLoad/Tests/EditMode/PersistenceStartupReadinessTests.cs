using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using FakeMG.SceneLoading;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FakeMG.SaveLoad.Tests
{
    public sealed class PersistenceStartupReadinessTests
    {
        private ResumeOrCreateWorldStartupPolicySO _policySO;
        private IGlobalSaveInitializer _globalInitializer;
        private IWorldStartupContext _worldContext;
        private ISceneLoader _sceneLoader;
        private string _activeWorldId;

        #region Public Methods

        [SetUp]
        public void SetUp()
        {
            _policySO = ScriptableObject.CreateInstance<ResumeOrCreateWorldStartupPolicySO>();
            _globalInitializer = Substitute.For<IGlobalSaveInitializer>();
            _worldContext = Substitute.For<IWorldStartupContext>();
            _sceneLoader = Substitute.For<ISceneLoader>();
            _activeWorldId = null;

            _globalInitializer.InitializeAsync(Arg.Any<CancellationToken>())
                .Returns(UniTask.FromResult(new GlobalSaveInitializationResult(true, Array.Empty<string>())));
            _worldContext.ActiveWorldId.Returns(_ => _activeWorldId);
            _worldContext.GetWorlds().Returns(Array.Empty<WorldSummary>());
            _sceneLoader.LoadSceneAsync(
                    Arg.Any<AssetReferenceScene>(),
                    Arg.Any<LoadSceneMode>(),
                    Arg.Any<CancellationToken>())
                .Returns(UniTask.FromResult(new SceneLoadResult(SceneLoadStatus.Ready, default, default, string.Empty)));
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_policySO);
        }

        [Test]
        public async Task SlowNewWorldCreationHoldsGameplayLoadUntilSnapshotCompletes()
        {
            var creationCompletion = new UniTaskCompletionSource<WorldCreationResult>();
            _worldContext.CreateWorldAsync("World", Arg.Any<CancellationToken>()).Returns(creationCompletion.Task);
            PersistenceStartupCoordinator startup = CreateStartup();
            var sceneRequest = new StartupSceneLoadRequest(startup, _sceneLoader);

            UniTask<StartupReadinessResult> initialization = startup.InitializeAsync(CancellationToken.None);
            UniTask<SceneLoadResult> sceneLoad = sceneRequest.LoadAsync(null, CancellationToken.None);

            Assert.That(sceneLoad.Status, Is.EqualTo(UniTaskStatus.Pending));
            await _sceneLoader.DidNotReceiveWithAnyArgs().LoadSceneAsync(default, default, default);

            _activeWorldId = "new-world";
            creationCompletion.TrySetResult(WorldCreationResult.Success(CreateWorldSummary(_activeWorldId)));

            Assert.That((await initialization).Succeeded, Is.True);
            Assert.That((await sceneLoad).Succeeded, Is.True);
            await _sceneLoader.Received(1).LoadSceneAsync(
                Arg.Any<AssetReferenceScene>(),
                Arg.Any<LoadSceneMode>(),
                Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task ExistingWorldOpensBeforeGameplayLoads()
        {
            _worldContext.GetWorlds().Returns(new[] { CreateWorldSummary("saved-world") });
            _worldContext.OpenWorldAsync("saved-world", Arg.Any<CancellationToken>())
                .Returns(_ =>
                {
                    _activeWorldId = "saved-world";
                    return UniTask.FromResult(WorldOperationResult.Success());
                });
            PersistenceStartupCoordinator startup = CreateStartup();
            var sceneRequest = new StartupSceneLoadRequest(startup, _sceneLoader);

            StartupReadinessResult initialization = await startup.InitializeAsync(CancellationToken.None);
            SceneLoadResult sceneLoad = await sceneRequest.LoadAsync(null, CancellationToken.None);

            Assert.That(initialization.Succeeded, Is.True);
            Assert.That(sceneLoad.Succeeded, Is.True);
            await _worldContext.DidNotReceiveWithAnyArgs().CreateWorldAsync(default, default);
        }

        [Test]
        public async Task FailedNewWorldCreationDoesNotLoadGameplay()
        {
            _worldContext.CreateWorldAsync("World", Arg.Any<CancellationToken>())
                .Returns(UniTask.FromResult(WorldCreationResult.Failure("Snapshot write failed.")));
            PersistenceStartupCoordinator startup = CreateStartup();
            var sceneRequest = new StartupSceneLoadRequest(startup, _sceneLoader);
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
                "Gameplay scene load skipped:.*Snapshot write failed"));

            StartupReadinessResult initialization = await startup.InitializeAsync(CancellationToken.None);
            SceneLoadResult sceneLoad = await sceneRequest.LoadAsync(null, CancellationToken.None);

            Assert.That(initialization.Succeeded, Is.False);
            Assert.That(sceneLoad.Status, Is.EqualTo(SceneLoadStatus.StartupFailed));
            Assert.That(sceneLoad.FailureReason, Does.Contain("Snapshot write failed"));
            await _sceneLoader.DidNotReceiveWithAnyArgs().LoadSceneAsync(default, default, default);
        }

        [Test]
        public async Task FailedGlobalInitializationDoesNotCreateOrLoadGameplay()
        {
            _globalInitializer.InitializeAsync(Arg.Any<CancellationToken>())
                .Returns(UniTask.FromResult(new GlobalSaveInitializationResult(false, new[] { "Settings were unreadable." })));
            PersistenceStartupCoordinator startup = CreateStartup();
            var sceneRequest = new StartupSceneLoadRequest(startup, _sceneLoader);
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
                "Gameplay scene load skipped:.*Settings were unreadable"));

            StartupReadinessResult initialization = await startup.InitializeAsync(CancellationToken.None);
            SceneLoadResult sceneLoad = await sceneRequest.LoadAsync(null, CancellationToken.None);

            Assert.That(initialization.Succeeded, Is.False);
            Assert.That(sceneLoad.Status, Is.EqualTo(SceneLoadStatus.StartupFailed));
            await _worldContext.DidNotReceiveWithAnyArgs().CreateWorldAsync(default, default);
            await _sceneLoader.DidNotReceiveWithAnyArgs().LoadSceneAsync(default, default, default);
        }

        [Test]
        public async Task CancelledWorldCreationDoesNotLoadGameplay()
        {
            var creationCompletion = new UniTaskCompletionSource<WorldCreationResult>();
            _worldContext.CreateWorldAsync("World", Arg.Any<CancellationToken>())
                .Returns(call => creationCompletion.Task.AttachExternalCancellation(call.Arg<CancellationToken>()));
            PersistenceStartupCoordinator startup = CreateStartup();
            var sceneRequest = new StartupSceneLoadRequest(startup, _sceneLoader);
            using var cancellationSource = new CancellationTokenSource();

            UniTask<StartupReadinessResult> initialization = startup.InitializeAsync(cancellationSource.Token);
            UniTask<SceneLoadResult> sceneLoad = sceneRequest.LoadAsync(null, CancellationToken.None);
            cancellationSource.Cancel();

            Assert.That((await initialization).Status, Is.EqualTo(StartupReadinessStatus.Cancelled));
            Assert.That((await sceneLoad).Status, Is.EqualTo(SceneLoadStatus.Cancelled));
            await _sceneLoader.DidNotReceiveWithAnyArgs().LoadSceneAsync(default, default, default);
        }

        #endregion

        #region Private Methods

        private PersistenceStartupCoordinator CreateStartup()
        {
            var configuration = new WorldSaveConfiguration(5, 300f, 10f, true, "World", _policySO);
            return new PersistenceStartupCoordinator(_globalInitializer, _worldContext, configuration);
        }

        private static WorldSummary CreateWorldSummary(string worldId)
        {
            return new WorldSummary(new WorldManifest { WorldId = worldId, DisplayName = "World" });
        }

        #endregion
    }
}
