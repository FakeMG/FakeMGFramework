using System.Collections.Generic;
using FakeMG.ItemInteraction;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace FakeMG.ItemInteraction.Tests
{
    public sealed class InteractionInputStateTests
    {
        private IActionItem _emptyHandItem;

        #region Public Methods

        [SetUp]
        public void SetUp()
        {
            _emptyHandItem = Substitute.For<IActionItem>();
        }

        [Test]
        public void PressDuringUnfinishedPunch_PreservesActiveInteraction()
        {
            IInteractionController controller = CreateActiveController();
            var state = CreateState(controller);
            state.PressItem(_emptyHandItem, null, default, default, default);
            state.Release();

            Assert.That(state.PressItem(_emptyHandItem, null, default, default, default), Is.False);
            state.Tick(0.2f);

            Assert.That(state.HasActiveInteraction, Is.True);
            controller.Received(1).Tick(0.2f);
            controller.Received(1).TryBeginInteraction(_emptyHandItem, null, Arg.Any<Ray>(), Arg.Any<Vector3>(), Arg.Any<Vector2>());
        }

        [Test]
        public void SelectionChangeWhileHeld_CancelsAndRequiresReleaseBeforeNewPress()
        {
            IInteractionController controller = CreateActiveController();
            var state = CreateState(controller);

            Assert.That(state.PressItem(_emptyHandItem, null, default, default, default), Is.True);
            state.ChangeSelection();

            controller.Received(1).CancelInteraction();
            Assert.That(state.RequiresFreshPress, Is.True);
            Assert.That(state.PressItem(_emptyHandItem, null, default, default, default), Is.False);

            state.Release();
            Assert.That(state.RequiresFreshPress, Is.False);
            Assert.That(state.PressItem(_emptyHandItem, null, default, default, default), Is.True);
            controller.Received(2).TryBeginInteraction(_emptyHandItem, null, Arg.Any<Ray>(), Arg.Any<Vector3>(), Arg.Any<Vector2>());
        }

        [Test]
        public void Release_DoesNotCancelActiveTimedController()
        {
            IInteractionController controller = CreateActiveController();
            var state = CreateState(controller);

            state.PressItem(_emptyHandItem, null, default, default, default);
            state.Release();
            state.Tick(0.5f);

            controller.Received(1).ReleaseInteraction();
            controller.DidNotReceive().CancelInteraction();
            controller.Received(1).Tick(0.5f);
        }

        [Test]
        public void DisablingNormalActions_CancelsActiveInteractionAndBlocksUntilRestored()
        {
            IInteractionController controller = CreateActiveController();
            var state = CreateState(controller);
            Assert.That(state.PressItem(_emptyHandItem, null, default, default, default), Is.True);

            state.SetNormalActionsEnabled(false);

            controller.Received(1).CancelInteraction();
            Assert.That(state.HasActiveInteraction, Is.False);
            Assert.That(state.PressItem(_emptyHandItem, null, default, default, default), Is.False);

            state.SetNormalActionsEnabled(true);

            Assert.That(state.PressItem(_emptyHandItem, null, default, default, default), Is.True);
        }

        #endregion

        #region Private Methods

        private IInteractionController CreateActiveController()
        {
            IInteractionController controller = Substitute.For<IInteractionController>();
            controller.Supports(_emptyHandItem, null).Returns(true);
            controller.TryBeginInteraction(_emptyHandItem, null, Arg.Any<Ray>(), Arg.Any<Vector3>(), Arg.Any<Vector2>()).Returns(true);
            controller.IsInteractionActive.Returns(true);
            return controller;
        }

        private static InteractionInputState CreateState(IInteractionController controller)
        {
            return new InteractionInputState(new InteractionControllerResolver(
                new List<IInteractionController> { controller }));
        }

        #endregion
    }
}
