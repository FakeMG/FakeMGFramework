using System;
using NSubstitute;
using NUnit.Framework;

namespace FakeMG.ItemInteraction.Tests
{
    public sealed class SelectionPresenterTests
    {
        private HeldItemPrefabPool _pool;
        private IActionItem _item;
        private IInteractionSelectionLifecycle _interactionLifecycle;
        private IItemSelectionPresentation _presentation;

        #region Public Methods

        [SetUp]
        public void SetUp()
        {
            var catalog = Substitute.For<IInteractionItemCatalog>();
            catalog.Items.Returns(Array.Empty<IInteractionItem>());
            _pool = new HeldItemPrefabPool(catalog, Substitute.For<IHeldItemPrefabProvider>());
            _item = Substitute.For<IActionItem>();
            _interactionLifecycle = Substitute.For<IInteractionSelectionLifecycle>();
            _presentation = Substitute.For<IItemSelectionPresentation>();
        }

        [TearDown]
        public void TearDown()
        {
            _pool.Dispose();
        }

        [Test]
        public void PresentSelectedItem_SameItemAndInstance_KeepsTheCurrentPresentation()
        {
            SelectionPresenter presenter = CreatePresenter(CreateInstanceSource("instance-1"));
            presenter.PresentSelectedItem();
            ClearReceivedCalls();

            presenter.PresentSelectedItem();

            _interactionLifecycle.DidNotReceive().ChangeSelection();
            _presentation.DidNotReceiveWithAnyArgs().HideSelection();
            _presentation.DidNotReceiveWithAnyArgs().ShowSelection(default);
        }

        [Test]
        public void PresentSelectedItem_SameItemWithANewInstance_PresentsItAgain()
        {
            var source = CreateInstanceSource("instance-1");
            SelectionPresenter presenter = CreatePresenter(source);
            presenter.PresentSelectedItem();
            ClearReceivedCalls();

            ((ISelectedItemInstanceSource)source).SelectedItemInstanceId.Returns("instance-2");
            presenter.PresentSelectedItem();

            _interactionLifecycle.Received(1).ChangeSelection();
            _presentation.Received(1).HideSelection();
            _presentation.Received(1).ShowSelection(_item);
        }

        [Test]
        public void PresentSelectedItem_SourceWithoutInstanceIds_PresentsTheSameItemAgain()
        {
            var source = Substitute.For<ISelectedItemSource>();
            source.SelectedItem.Returns(_item);
            SelectionPresenter presenter = CreatePresenter(source);
            presenter.PresentSelectedItem();
            ClearReceivedCalls();

            presenter.PresentSelectedItem();

            _interactionLifecycle.Received(1).ChangeSelection();
            _presentation.Received(1).ShowSelection(_item);
        }

        [Test]
        public void CancelAndClear_ThenPresentingTheSameInstance_PresentsItAgain()
        {
            SelectionPresenter presenter = CreatePresenter(CreateInstanceSource("instance-1"));
            presenter.PresentSelectedItem();

            presenter.CancelAndClear();
            ClearReceivedCalls();
            presenter.PresentSelectedItem();

            _presentation.Received(1).ShowSelection(_item);
        }

        #endregion

        #region Private Methods

        private SelectionPresenter CreatePresenter(ISelectedItemSource source)
        {
            return new SelectionPresenter(source, _pool, _interactionLifecycle, new[] { _presentation });
        }

        private ISelectedItemSource CreateInstanceSource(string instanceId)
        {
            var source = Substitute.For<ISelectedItemSource, ISelectedItemInstanceSource>();
            source.SelectedItem.Returns(_item);
            ((ISelectedItemInstanceSource)source).SelectedItemInstanceId.Returns(instanceId);
            return source;
        }

        private void ClearReceivedCalls()
        {
            _interactionLifecycle.ClearReceivedCalls();
            _presentation.ClearReceivedCalls();
        }

        #endregion
    }
}
