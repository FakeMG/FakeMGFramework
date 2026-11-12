using System;
using NSubstitute;
using NUnit.Framework;

namespace FakeMG.ItemInteraction.Tests
{
    public sealed class ItemSelectionStateTests
    {
        private IActionItem _emptyHandItem;
        private IHeldItem _heldItem;
        private ItemSelectionState _selection;
        private Action _publishChange;

        #region Public Methods

        [SetUp]
        public void SetUp()
        {
            _emptyHandItem = Substitute.For<IActionItem>();
            _heldItem = Substitute.For<IHeldItem>();
            _selection = new ItemSelectionState(_emptyHandItem);
            _publishChange = Substitute.For<Action>();
            _selection.OnSelectedItemChanged += _publishChange;
        }

        [TearDown]
        public void TearDown()
        {
            _selection.OnSelectedItemChanged -= _publishChange;
        }

        [Test]
        public void SelectedItem_BeforeAnySelection_IsTheEmptyHand()
        {
            Assert.That(_selection.SelectedItem, Is.SameAs(_emptyHandItem));
        }

        [Test]
        public void Select_DifferentItem_MakesItActiveAndPublishesTheChange()
        {
            _selection.Select(_heldItem);

            Assert.That(_selection.SelectedItem, Is.SameAs(_heldItem));
            _publishChange.Received(1).Invoke();
        }

        [Test]
        public void SelectEmptyHand_AfterHoldingAnItem_ReturnsToTheEmptyHand()
        {
            _selection.Select(_heldItem);
            _publishChange.ClearReceivedCalls();

            _selection.SelectEmptyHand();

            Assert.That(_selection.SelectedItem, Is.SameAs(_emptyHandItem));
            _publishChange.Received(1).Invoke();
        }

        [Test]
        public void SelectEmptyHand_WhileAlreadyEmptyHanded_PublishesNothing()
        {
            _selection.SelectEmptyHand();

            _publishChange.DidNotReceive().Invoke();
        }

        [Test]
        public void Select_SameItemAgain_PublishesNothing()
        {
            _selection.Select(_heldItem);
            _publishChange.ClearReceivedCalls();

            _selection.Select(_heldItem);

            _publishChange.DidNotReceive().Invoke();
        }

        #endregion
    }
}
