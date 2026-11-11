using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using VContainer;

namespace FakeMG.ItemInteraction.Tests
{
    public sealed class ConsumableInteractionControllerTests
    {
        private IConsumableItem _item;
        private ItemActionSO _actionSO;
        private ItemAnimationProfileSO _animationProfileSO;
        private PressToUseInputLifecycleSO _inputLifecycleSO;
        private AnimationClip _animationClip;
        private IActiveItemSource _selection;
        private ISelectedItemConsumption _consumption;
        private ISelectedItemConsumptionReservation _reservation;
        private IConsumableEffect _effect;
        private IItemUsePresentation _presentation;
        private ConsumableInteractionController _controller;

        #region Public Methods

        [SetUp]
        public void SetUp()
        {
            _item = Substitute.For<IConsumableItem>();
            _actionSO = ScriptableObject.CreateInstance<ItemActionSO>();
            _animationProfileSO = ScriptableObject.CreateInstance<ItemAnimationProfileSO>();
            _inputLifecycleSO = ScriptableObject.CreateInstance<PressToUseInputLifecycleSO>();
            _animationClip = new AnimationClip();
            _animationProfileSO.ConfigureAnimationsForEditor(_animationClip, _animationClip);
            _actionSO.ConfigureForEditor(_inputLifecycleSO, null, _animationProfileSO, null);
            _item.Actions.Returns(new[] { _actionSO });
            _item.PrimaryActionSO.Returns(_actionSO);
            _selection = Substitute.For<IActiveItemSource>();
            _selection.ActiveItem.Returns(_item);
            _consumption = Substitute.For<ISelectedItemConsumption>();
            _reservation = Substitute.For<ISelectedItemConsumptionReservation>();
            _consumption.TryReserve(_item, 1, out Arg.Any<ISelectedItemConsumptionReservation>()).Returns(ReserveItem);
            _reservation.TryCommit().Returns(true);
            _effect = Substitute.For<IConsumableEffect>();
            _effect.CanApply(_item).Returns(true);
            _effect.TryApply(_item).Returns(true);
            _presentation = Substitute.For<IItemUsePresentation>();
            _controller = new ConsumableInteractionController(_selection, _consumption, _effect, new[] { _presentation });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_actionSO);
            Object.DestroyImmediate(_animationProfileSO);
            Object.DestroyImmediate(_inputLifecycleSO);
            Object.DestroyImmediate(_animationClip);
        }

        [Test]
        public void ActionMoment_CommitsOnceAndFinishesPresentation()
        {
            Assert.That(BeginUse(), Is.True);

            _controller.Tick(1f);
            _controller.Tick(1f);

            _effect.Received(1).TryApply(_item);
            _reservation.Received(1).TryCommit();
            _reservation.DidNotReceive().Cancel();
            _presentation.Received(1).BeginUse(_item, _actionSO);
            _presentation.Received(1).EndUse(_item, _actionSO);
            Assert.That(_controller.IsInteractionActive, Is.False);
        }

        [Test]
        public void BeforeActionMoment_DoesNotApplyEffectOrReserveItem()
        {
            BeginUse();

            _controller.Tick(0f);

            _effect.DidNotReceive().TryApply(_item);
            _consumption.DidNotReceiveWithAnyArgs().TryReserve(default, default, out _);
            Assert.That(_controller.IsInteractionActive, Is.True);
        }

        [Test]
        public void EffectRejected_CancelsReservationWithoutCommitting()
        {
            _effect.TryApply(_item).Returns(false);
            BeginUse();

            _controller.Tick(1f);

            _reservation.Received(1).Cancel();
            _reservation.DidNotReceive().TryCommit();
        }

        [Test]
        public void ReservationRejected_DoesNotApplyEffect()
        {
            _consumption.TryReserve(_item, 1, out Arg.Any<ISelectedItemConsumptionReservation>()).Returns(false);
            BeginUse();

            _controller.Tick(1f);

            _effect.DidNotReceive().TryApply(_item);
        }

        [Test]
        public void SelectionChangedBeforeActionMoment_DoesNotConsumeStaleItem()
        {
            BeginUse();
            _selection.ActiveItem.Returns((IInteractionItem)null);

            _controller.Tick(1f);

            _effect.DidNotReceive().TryApply(_item);
            _consumption.DidNotReceiveWithAnyArgs().TryReserve(default, default, out _);
        }

        [Test]
        public void CancelBeforeActionMoment_EndsPresentationWithoutConsuming()
        {
            BeginUse();

            _controller.CancelInteraction();
            _controller.Tick(1f);

            _effect.DidNotReceive().TryApply(_item);
            _presentation.Received(1).EndUse(_item, _actionSO);
            Assert.That(_controller.IsInteractionActive, Is.False);
        }

        [Test]
        public void InvalidSelection_RejectsStartWithoutPresentation()
        {
            _selection.ActiveItem.Returns((IInteractionItem)null);

            Assert.That(BeginUse(), Is.False);

            _presentation.DidNotReceive().BeginUse(_item, _actionSO);
            Assert.That(_controller.IsInteractionActive, Is.False);
        }

        [Test]
        public void TwoCharactersUsingTheSameItem_CancelAndConsumeIndependently()
        {
            var otherConsumption = Substitute.For<ISelectedItemConsumption>();
            var otherPresentation = Substitute.For<IItemUsePresentation>();
            using IObjectResolver shared = new ContainerBuilder().Build();
            using IScopedObjectResolver first = shared.CreateScope(
                builder => RegisterCharacter(builder, _consumption, _presentation));
            using IScopedObjectResolver second = shared.CreateScope(
                builder => RegisterCharacter(builder, otherConsumption, otherPresentation));
            InteractionInputState firstInput = first.Resolve<InteractionInputState>();
            InteractionInputState secondInput = second.Resolve<InteractionInputState>();

            Assert.That(firstInput.PressItem(_item, _actionSO, default, default, default), Is.True);
            Assert.That(secondInput.PressItem(_item, _actionSO, default, default, default), Is.True);
            secondInput.ChangeSelection();
            Assert.That(secondInput.RequiresFreshPress, Is.True);
            Assert.That(firstInput.HasActiveInteraction, Is.True);
            Assert.That(firstInput.RequiresFreshPress, Is.False);
            secondInput.Release();
            firstInput.Tick(1f);

            _reservation.Received(1).TryCommit();
            otherConsumption.DidNotReceiveWithAnyArgs().TryReserve(default, default, out _);
            _presentation.Received(1).BeginUse(_item, _actionSO);
            _presentation.Received(1).EndUse(_item, _actionSO);
            otherPresentation.Received(1).BeginUse(_item, _actionSO);
            otherPresentation.Received(1).EndUse(_item, _actionSO);
            first.Resolve<IConsumableEffect>().Received(1).TryApply(_item);
            second.Resolve<IConsumableEffect>().DidNotReceive().TryApply(_item);
        }

        #endregion

        #region Private Methods

        private bool BeginUse()
        {
            return _controller.TryBeginInteraction(_item, _actionSO, default, default, default);
        }

        private bool ReserveItem(NSubstitute.Core.CallInfo call)
        {
            call[2] = _reservation;
            return true;
        }

        private void RegisterCharacter(IContainerBuilder builder, ISelectedItemConsumption consumption, IItemUsePresentation presentation)
        {
            var source = Substitute.For<IActiveItemSource>();
            source.ActiveItem.Returns(_item);
            var effect = Substitute.For<IConsumableEffect>();
            effect.CanApply(_item).Returns(true);
            effect.TryApply(_item).Returns(true);
            builder.RegisterInstance(source);
            builder.RegisterInstance(effect);
            builder.RegisterInstance(consumption);
            builder.RegisterInstance(presentation);
            ItemInteractionServiceInstaller.RegisterCharacter(builder);
            ItemInteractionServiceInstaller.RegisterConsumables(builder);
        }

        #endregion
    }
}
