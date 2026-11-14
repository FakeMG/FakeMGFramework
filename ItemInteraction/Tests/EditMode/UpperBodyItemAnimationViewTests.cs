using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace FakeMG.ItemInteraction.Tests
{
    public sealed class UpperBodyItemAnimationViewTests
    {
        private ItemTestFixtures _fixtures;
        private GameObject _viewObject;
        private UpperBodyItemAnimationView _view;
        private IItemCharacterAnimation _characterAnimation;
        private IHeldItem _item;
        private IActionItem _emptyHandItem;
        private ItemActionSO _actionSO;

        #region Public Methods

        [SetUp]
        public void SetUp()
        {
            _fixtures = new ItemTestFixtures();
            _actionSO = _fixtures.CreateAction();
            _item = Substitute.For<IHeldItem>();
            _item.AnimationProfileSO.Returns(_fixtures.ProfileSO);
            _emptyHandItem = Substitute.For<IActionItem>();
            _characterAnimation = Substitute.For<IItemCharacterAnimation>();
            _viewObject = new GameObject(nameof(UpperBodyItemAnimationViewTests));
            _view = _viewObject.AddComponent<UpperBodyItemAnimationView>();
            _view.Bind(_characterAnimation);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_viewObject);
            _fixtures.Dispose();
        }

        [Test]
        public void ShowSelection_WithAHeldItem_PlaysItsHoldingPoseWithItsProfile()
        {
            _view.ShowSelection(_item);

            _characterAnimation.Received(1).SetLocomotionProfile(_fixtures.ProfileSO);
            _characterAnimation.Received(1).PlayHoldingPose(
                Arg.Is<ItemAnimationPose>(pose => pose.AnimationClip == _fixtures.HoldingClip),
                _fixtures.ProfileSO.FadeDurationSeconds);
        }

        [Test]
        public void BeginUse_PlaysTheUseClipFromItsStartAtThePlaybackSpeed()
        {
            _view.ShowSelection(_item);
            _characterAnimation.ClearReceivedCalls();

            _view.BeginUse(_item, _actionSO, 2.5f);

            _characterAnimation.Received(1).PlayUsePose(
                Arg.Is<ItemAnimationPose>(pose => pose.AnimationClip == _fixtures.UseClip && pose.PlaybackSpeedMultiplier == 2.5f),
                _fixtures.ProfileSO.FadeDurationSeconds,
                true);
        }

        [Test]
        public void BeginUse_ForUnselectedItem_PlaysNothing()
        {
            _view.BeginUse(_item, _actionSO, 1f);

            _characterAnimation.DidNotReceiveWithAnyArgs().PlayUsePose(default, default, default);
        }

        [Test]
        public void ShowSelection_WithEmptyHand_KeepsBaseLocomotionWithoutAHoldingPose()
        {
            _view.ShowSelection(_emptyHandItem);

            _characterAnimation.Received(1).Stop(Arg.Any<float>());
            _characterAnimation.DidNotReceiveWithAnyArgs().PlayHoldingPose(default, default);
        }

        [Test]
        public void BeginUse_WithEmptyHand_PlaysTheActionUseClipOverBaseLocomotion()
        {
            _view.ShowSelection(_emptyHandItem);
            _characterAnimation.ClearReceivedCalls();

            _view.BeginUse(_emptyHandItem, _actionSO, 1f);

            _characterAnimation.Received(1).SetLocomotionProfile(null);
            _characterAnimation.Received(1).PlayUsePose(
                Arg.Is<ItemAnimationPose>(pose => pose.AnimationClip == _fixtures.UseClip),
                _fixtures.ProfileSO.FadeDurationSeconds,
                true);
        }

        [Test]
        public void EndUse_WithEmptyHand_StopsTheItemAnimation()
        {
            _view.ShowSelection(_emptyHandItem);
            _view.BeginUse(_emptyHandItem, _actionSO, 1f);
            _characterAnimation.ClearReceivedCalls();

            _view.EndUse(_emptyHandItem, _actionSO);

            _characterAnimation.Received(1).Stop(_fixtures.ProfileSO.FadeDurationSeconds);
            _characterAnimation.DidNotReceiveWithAnyArgs().PlayHoldingPose(default, default);
        }

        [Test]
        public void TryPlaySelectedItemUseAnimation_WithAHeldItem_PlaysItsUseClipFromItsStart()
        {
            _view.ShowSelection(_item);
            _characterAnimation.ClearReceivedCalls();

            Assert.That(_view.TryPlaySelectedItemUseAnimation(), Is.True);
            _characterAnimation.Received(1).PlayUsePose(
                Arg.Is<ItemAnimationPose>(pose => pose.AnimationClip == _fixtures.UseClip && pose.PlaybackSpeedMultiplier == 1f),
                _fixtures.ProfileSO.FadeDurationSeconds,
                true);
        }

        [Test]
        public void TryPlaySelectedItemUseAnimation_WithEmptyHand_PlaysNothing()
        {
            _view.ShowSelection(_emptyHandItem);
            _characterAnimation.ClearReceivedCalls();

            Assert.That(_view.TryPlaySelectedItemUseAnimation(), Is.False);
            _characterAnimation.DidNotReceiveWithAnyArgs().PlayUsePose(default, default, default);
        }

        [Test]
        public void TryPlaySelectedItemUseAnimation_DuringAnInteractionUse_PlaysNothing()
        {
            _view.ShowSelection(_item);
            _view.BeginUse(_item, _actionSO, 1f);
            _characterAnimation.ClearReceivedCalls();

            Assert.That(_view.TryPlaySelectedItemUseAnimation(), Is.False);
            _characterAnimation.DidNotReceiveWithAnyArgs().PlayUsePose(default, default, default);
        }

        [Test]
        public void HideSelection_DuringItemUse_StopsTheItemAnimation()
        {
            _view.ShowSelection(_item);
            _view.BeginUse(_item, _actionSO, 1f);
            _characterAnimation.ClearReceivedCalls();

            _view.HideSelection();

            _characterAnimation.ReceivedWithAnyArgs(1).Stop(default);
        }

        [Test]
        public void EndUse_WithSelectedItem_ReturnsToTheHoldingPose()
        {
            _view.ShowSelection(_item);
            _view.BeginUse(_item, _actionSO, 1f);
            _characterAnimation.ClearReceivedCalls();

            _view.EndUse(_item, _actionSO);

            _characterAnimation.Received(1).PlayHoldingPose(
                Arg.Is<ItemAnimationPose>(pose => pose.AnimationClip == _fixtures.HoldingClip),
                _fixtures.ProfileSO.FadeDurationSeconds);
            _characterAnimation.DidNotReceiveWithAnyArgs().Stop(default);
        }

        [Test]
        public void EndUse_ForAnActionThatIsNotActive_KeepsTheCurrentAnimation()
        {
            _view.ShowSelection(_item);
            _characterAnimation.ClearReceivedCalls();

            _view.EndUse(_item, _actionSO);

            _characterAnimation.DidNotReceiveWithAnyArgs().PlayHoldingPose(default, default);
            _characterAnimation.DidNotReceiveWithAnyArgs().Stop(default);
        }

        [Test]
        public void SetWaterState_LeavingWaterDuringAUse_ResumesTheUseWithoutRestarting()
        {
            _view.ShowSelection(_item);
            _view.BeginUse(_item, _actionSO, 1f);
            _view.SetWaterState(true);
            _characterAnimation.ClearReceivedCalls();

            _view.SetWaterState(false);

            _characterAnimation.Received(1).PlayUsePose(
                Arg.Is<ItemAnimationPose>(pose => pose.AnimationClip == _fixtures.UseClip),
                _fixtures.ProfileSO.FadeDurationSeconds,
                false);
        }

        [Test]
        public void SetWaterState_EnteringWaterDuringAOneOffUse_DropsIt()
        {
            _view.ShowSelection(_item);
            _view.TryPlaySelectedItemUseAnimation();
            _view.SetWaterState(true);
            _characterAnimation.ClearReceivedCalls();

            _view.SetWaterState(false);

            _characterAnimation.DidNotReceiveWithAnyArgs().PlayUsePose(default, default, default);
            _characterAnimation.Received(1).PlayHoldingPose(Arg.Any<ItemAnimationPose>(), _fixtures.ProfileSO.FadeDurationSeconds);
        }

        #endregion
    }
}
