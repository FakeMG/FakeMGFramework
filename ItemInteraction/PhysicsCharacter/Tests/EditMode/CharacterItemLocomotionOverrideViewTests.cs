using NUnit.Framework;
using PhysicsCharacterController;
using UnityEngine;

namespace FakeMG.ItemInteraction.Tests
{
    public sealed class CharacterItemLocomotionOverrideViewTests
    {
        private GameObject _characterObject;
        private CharacterItemLocomotionOverrideView _view;
        private CharacterItemAnimationProfileSO _profileSO;

        #region Public Methods

        [SetUp]
        public void SetUp()
        {
            _profileSO = ScriptableObject.CreateInstance<CharacterItemAnimationProfileSO>();
            _profileSO.ConfigureLocomotionOverridesForEditor(
                isHoldingPoseReplacedByLocomotionOverride: true,
                new StateLocomotionOverride[0],
                new StateAirborneOverride[0]);
            _characterObject = new GameObject(nameof(CharacterItemLocomotionOverrideViewTests));
            _view = _characterObject.AddComponent<CharacterItemLocomotionOverrideView>();
            _view.ConfigureForEditor(_characterObject.AddComponent<CharacterAnimator>());
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_characterObject);
            Object.DestroyImmediate(_profileSO);
        }

        [Test]
        public void IsHoldingPoseReplaced_WhileNoOverrideIsPlaying_KeepsTheHoldingPoseLayered()
        {
            _view.SetLocomotionProfile(_profileSO);

            // The character has not started any locomotion, so no override is playing yet.
            Assert.That(_view.IsHoldingPoseReplacedByLocomotionOverride, Is.False);
        }

        [Test]
        public void IsHoldingPoseReplaced_BeforeAnyProfileIsSet_IsFalse()
        {
            Assert.That(_view.IsHoldingPoseReplacedByLocomotionOverride, Is.False);
        }

        [Test]
        public void SetLocomotionProfile_WithAProfileThatCannotOverrideLocomotion_DoesNotReplaceTheHoldingPose()
        {
            ItemAnimationProfileSO plainProfileSO = ScriptableObject.CreateInstance<ItemAnimationProfileSO>();
            _view.SetLocomotionProfile(_profileSO);

            _view.SetLocomotionProfile(plainProfileSO);

            Assert.That(_view.IsHoldingPoseReplacedByLocomotionOverride, Is.False);
            Object.DestroyImmediate(plainProfileSO);
        }

        [Test]
        public void SetLocomotionProfile_WithNoProfile_DoesNotReplaceTheHoldingPose()
        {
            _view.SetLocomotionProfile(_profileSO);

            _view.SetLocomotionProfile(null);

            Assert.That(_view.IsHoldingPoseReplacedByLocomotionOverride, Is.False);
        }

        #endregion
    }
}
