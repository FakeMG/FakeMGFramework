using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using VContainer;

namespace FakeMG.ItemInteraction.Tests
{
    public sealed class HeldItemPrefabPoolLifecycleTests
    {
        private IInteractionItemCatalog _catalog;
        private HeldItemPrefabPool _pool;
        private IConsumableItem _item;
        private IHeldItemPrefabProvider _prefabProvider;

        #region Public Methods

        [SetUp]
        public void SetUp()
        {
            _catalog = Substitute.For<IInteractionItemCatalog>();
            _catalog.Items.Returns(Array.Empty<IInteractionItem>());
            _item = Substitute.For<IConsumableItem>();
            _prefabProvider = Substitute.For<IHeldItemPrefabProvider>();
            _pool = new HeldItemPrefabPool(_catalog, Substitute.For<IObjectResolver>(), _prefabProvider);
        }

        [TearDown]
        public void TearDown()
        {
            _pool.Dispose();
        }

        [Test]
        public void StartAsync_EmptyCatalog_MakesPresentationsReady()
        {
            _pool.StartAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(_pool.ArePresentationAssetsReady, Is.True);
        }

        [Test]
        public void StartAsync_CancelledBeforeStartup_CompletesWithoutPublishingReadiness()
        {
            var publishReadiness = Substitute.For<Action>();
            _pool.OnPresentationAssetsReady += publishReadiness;
            try
            {
                _pool.StartAsync(new CancellationToken(true)).GetAwaiter().GetResult();

                Assert.That(_pool.ArePresentationAssetsReady, Is.False);
                publishReadiness.DidNotReceive().Invoke();
            }
            finally
            {
                _pool.OnPresentationAssetsReady -= publishReadiness;
            }
        }

        [Test]
        public void StartAsync_AfterDisposal_DoesNotMakePresentationsReady()
        {
            _pool.Dispose();

            _pool.StartAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(_pool.ArePresentationAssetsReady, Is.False);
        }

        [Test]
        public void Dispose_AfterStartup_RevokesPresentationReadiness()
        {
            _pool.StartAsync(CancellationToken.None).GetAwaiter().GetResult();

            _pool.Dispose();

            Assert.That(_pool.ArePresentationAssetsReady, Is.False);
        }

        [Test]
        public void Dispose_DuringPreload_ReleasesLateResultWithoutPublishingReadiness()
        {
            _catalog.Items.Returns(new IInteractionItem[] { _item });
            var loadSource = new UniTaskCompletionSource<IHeldItemPrefabHandle>();
            _prefabProvider.LoadAsync(default, CancellationToken.None).Returns(loadSource.Task);
            var handle = Substitute.For<IHeldItemPrefabHandle>();
            handle.IsSucceeded.Returns(true);
            UniTask startup = _pool.StartAsync(CancellationToken.None);

            _pool.Dispose();
            loadSource.TrySetResult(handle);
            startup.GetAwaiter().GetResult();

            handle.Received(1).Dispose();
            Assert.That(_pool.ArePresentationAssetsReady, Is.False);
        }

        [Test]
        public void StartAsync_CancelledDuringPreload_CompletesWithoutPublishingReadiness()
        {
            _catalog.Items.Returns(new IInteractionItem[] { _item });
            using var cancellationSource = new CancellationTokenSource();
            var loadSource = new UniTaskCompletionSource<IHeldItemPrefabHandle>();
            _prefabProvider.LoadAsync(default, cancellationSource.Token).Returns(loadSource.Task);
            UniTask startup = _pool.StartAsync(cancellationSource.Token);

            cancellationSource.Cancel();
            loadSource.TrySetCanceled(cancellationSource.Token);
            startup.GetAwaiter().GetResult();

            Assert.That(_pool.ArePresentationAssetsReady, Is.False);
        }

        #endregion
    }
}
