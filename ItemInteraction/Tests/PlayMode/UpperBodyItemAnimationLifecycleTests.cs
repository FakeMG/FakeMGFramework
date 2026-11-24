#if UNITY_EDITOR
using System.Collections;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FakeMG.ItemInteraction.Tests
{
    /// <summary>Covers what only the frame loop drives; the view's direct method contracts stay in EditMode.</summary>
    public sealed class UpperBodyItemAnimationLifecycleTests
    {
        private const float USE_CLIP_LENGTH_SECONDS = 0.05f;
        private const float CLIP_FRAME_RATE = 60f;
        private const int MAX_WAIT_FRAME_COUNT = 300;

        private GameObject _viewObject;
        private UpperBodyItemAnimationView _view;
        private IItemCharacterAnimation _characterAnimation;
        private IHeldItem _item;
        private ItemAnimationProfileSO _profileSO;
        private AnimationClip _holdingClip;
        private AnimationClip _useClip;

        #region Public Methods

        [SetUp]
        public void SetUp()
        {
            _holdingClip = new AnimationClip();
            _useClip = new AnimationClip { frameRate = CLIP_FRAME_RATE };
            _useClip.SetCurve(string.Empty, typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, 0f, USE_CLIP_LENGTH_SECONDS, 1f));
            _profileSO = ScriptableObject.CreateInstance<ItemAnimationProfileSO>();
            _profileSO.ConfigureAnimationsForEditor(_holdingClip, _useClip);
            _item = Substitute.For<IHeldItem>();
            _item.AnimationProfileSO.Returns(_profileSO);
            _characterAnimation = Substitute.For<IItemCharacterAnimation>();
            _viewObject = new GameObject(nameof(UpperBodyItemAnimationLifecycleTests));
            _view = _viewObject.AddComponent<UpperBodyItemAnimationView>();
            _view.Bind(_characterAnimation);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_viewObject);
            Object.DestroyImmediate(_profileSO);
            Object.DestroyImmediate(_useClip);
            Object.DestroyImmediate(_holdingClip);
        }

        [UnityTest]
        public IEnumerator Update_WhileHolding_ReplaysTheHoldingPose()
        {
            _view.ShowSelection(_item);
            _characterAnimation.ClearReceivedCalls();

            yield return null;
            yield return null;

            _characterAnimation.Received().PlayHoldingPose(
                Arg.Is<ItemAnimationPose>(pose => pose.AnimationClip == _holdingClip),
                _profileSO.FadeDurationSeconds);
        }

        [UnityTest]
        public IEnumerator Update_InWater_DoesNotReplayTheHoldingPose()
        {
            _view.ShowSelection(_item);
            _view.SetWaterState(true);
            _characterAnimation.ClearReceivedCalls();

            yield return null;
            yield return null;

            _characterAnimation.DidNotReceiveWithAnyArgs().PlayHoldingPose(default, default);
        }

        [UnityTest]
        public IEnumerator Update_OneOffUseFinishes_ReturnsToTheHoldingPose()
        {
            _view.ShowSelection(_item);
            Assert.That(_view.TryPlaySelectedItemUseAnimation(), Is.True);
            _characterAnimation.ClearReceivedCalls();

            for (int frame = 0; frame < MAX_WAIT_FRAME_COUNT && !HasReturnedToHoldingPose(); frame++)
            {
                yield return null;
            }

            Assert.That(HasReturnedToHoldingPose(), Is.True, "The one-off use never returned to the holding pose.");
        }

        #endregion

        #region Private Methods

        private bool HasReturnedToHoldingPose()
        {
            foreach (NSubstitute.Core.ICall call in _characterAnimation.ReceivedCalls())
            {
                if (call.GetMethodInfo().Name == nameof(IItemCharacterAnimation.PlayHoldingPose))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
#endif
