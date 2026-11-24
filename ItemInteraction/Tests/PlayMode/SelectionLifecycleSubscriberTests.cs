using System;
using System.Collections;
using System.Threading;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace FakeMG.ItemInteraction.Tests
{
    public sealed class SelectionLifecycleSubscriberTests
    {
        private GameObject _subscriberObject;
        private SelectionLifecycleSubscriber _subscriber;
        private HeldItemPrefabPool _pool;
        private IActionItem _emptyHandItem;
        private IHeldItem _heldItem;
        private ItemSelectionState _selection;
        private IInteractionSelectionLifecycle _interactionLifecycle;
        private IItemSelectionPresentation _presentation;

        #region Public Methods

        [SetUp]
        public void SetUp()
        {
            var catalog = Substitute.For<IInteractionItemCatalog>();
            catalog.Items.Returns(Array.Empty<IInteractionItem>());
            _pool = new HeldItemPrefabPool(catalog, Substitute.For<IHeldItemPrefabProvider>());
            _emptyHandItem = Substitute.For<IActionItem>();
            _heldItem = Substitute.For<IHeldItem>();
            _selection = new ItemSelectionState(_emptyHandItem);
            _interactionLifecycle = Substitute.For<IInteractionSelectionLifecycle>();
            _presentation = Substitute.For<IItemSelectionPresentation>();
            _subscriberObject = new GameObject(nameof(SelectionLifecycleSubscriberTests));
            _subscriber = _subscriberObject.AddComponent<SelectionLifecycleSubscriber>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_subscriberObject);
            _pool.Dispose();
        }

        [Test]
        public void Bind_BeforeHeldPrefabsAreLoaded_ShowsTheEmptyHandAtOnce()
        {
            _subscriber.Bind(_selection, _pool, _interactionLifecycle, _presentation);

            _presentation.Received(1).ShowSelection(_emptyHandItem);
        }

        [Test]
        public void SelectingAHeldItem_BeforeHeldPrefabsAreLoaded_WaitsForThePreload()
        {
            _subscriber.Bind(_selection, _pool, _interactionLifecycle, _presentation);
            _presentation.ClearReceivedCalls();

            _selection.Select(_heldItem);
            _presentation.DidNotReceive().ShowSelection(_heldItem);

            _pool.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
            _presentation.Received(1).ShowSelection(_heldItem);
        }

        [Test]
        public void SelectingTheEmptyHand_AfterAHeldItem_HidesTheItemAndShowsTheEmptyHand()
        {
            _pool.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
            _subscriber.Bind(_selection, _pool, _interactionLifecycle, _presentation);
            _selection.Select(_heldItem);
            _presentation.ClearReceivedCalls();
            _interactionLifecycle.ClearReceivedCalls();

            _selection.SelectEmptyHand();

            _interactionLifecycle.Received(1).ChangeSelection();
            _presentation.Received(1).HideSelection();
            _presentation.Received(1).ShowSelection(_emptyHandItem);
        }

        [UnityTest]
        public IEnumerator DisablingStopsSelectionCallbacksAndReEnablingFiresEachChangeOnce()
        {
            _pool.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
            _subscriber.Bind(_selection, _pool, _interactionLifecycle, _presentation);
            _selection.Select(_heldItem);

            _subscriber.enabled = false;
            yield return null;
            _presentation.ClearReceivedCalls();
            _selection.SelectEmptyHand();
            _presentation.DidNotReceiveWithAnyArgs().ShowSelection(default);

            _subscriber.enabled = true;
            yield return null;
            _presentation.Received(1).ShowSelection(_emptyHandItem);
            _presentation.ClearReceivedCalls();

            _selection.Select(_heldItem);

            _presentation.Received(1).ShowSelection(_heldItem);
        }

        #endregion
    }
}
