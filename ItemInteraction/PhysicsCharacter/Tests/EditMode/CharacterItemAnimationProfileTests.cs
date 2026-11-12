using NUnit.Framework;
using PhysicsCharacterController;
using UnityEngine;

namespace FakeMG.ItemInteraction.Tests
{
    public sealed class CharacterItemAnimationProfileTests
    {
        private CharacterItemAnimationProfileSO _profileSO;
        private StateSO _standingStateSO;
        private StateSO _airborneStateSO;
        private StateSO _unlistedStateSO;
        private LocomotionAnimationDataSO _standingLocomotionSO;
        private AirborneAnimationDataSO _airborneLocomotionSO;

        #region Public Methods

        [SetUp]
        public void SetUp()
        {
            _profileSO = ScriptableObject.CreateInstance<CharacterItemAnimationProfileSO>();
            _standingStateSO = ScriptableObject.CreateInstance<StateSO>();
            _airborneStateSO = ScriptableObject.CreateInstance<StateSO>();
            _unlistedStateSO = ScriptableObject.CreateInstance<StateSO>();
            _standingLocomotionSO = ScriptableObject.CreateInstance<LocomotionAnimationDataSO>();
            _airborneLocomotionSO = ScriptableObject.CreateInstance<AirborneAnimationDataSO>();
            _profileSO.ConfigureLocomotionOverridesForEditor(
                isHoldingPoseReplacedByLocomotionOverride: true,
                new[] { new StateLocomotionOverride(_standingStateSO, _standingLocomotionSO) },
                new[] { new StateAirborneOverride(_airborneStateSO, _airborneLocomotionSO) });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_profileSO);
            Object.DestroyImmediate(_standingStateSO);
            Object.DestroyImmediate(_airborneStateSO);
            Object.DestroyImmediate(_unlistedStateSO);
            Object.DestroyImmediate(_standingLocomotionSO);
            Object.DestroyImmediate(_airborneLocomotionSO);
        }

        [Test]
        public void GetLocomotionOverride_ForAListedState_ReturnsItsOverride()
        {
            Assert.That(_profileSO.GetLocomotionOverride(_standingStateSO), Is.SameAs(_standingLocomotionSO));
        }

        [Test]
        public void GetLocomotionOverride_ForAnUnlistedState_KeepsTheBaseLocomotion()
        {
            Assert.That(_profileSO.GetLocomotionOverride(_unlistedStateSO), Is.Null);
            Assert.That(_profileSO.GetLocomotionOverride(_airborneStateSO), Is.Null);
        }

        [Test]
        public void GetAirborneOverride_ForAListedState_ReturnsItsOverride()
        {
            Assert.That(_profileSO.GetAirborneOverride(_airborneStateSO), Is.SameAs(_airborneLocomotionSO));
            Assert.That(_profileSO.GetAirborneOverride(_standingStateSO), Is.Null);
        }

        [Test]
        public void IsHoldingPoseReplacedByLocomotionOverride_ByDefault_IsFalse()
        {
            var defaultProfileSO = ScriptableObject.CreateInstance<CharacterItemAnimationProfileSO>();

            Assert.That(defaultProfileSO.IsHoldingPoseReplacedByLocomotionOverride, Is.False);
            Assert.That(defaultProfileSO.GetLocomotionOverride(_standingStateSO), Is.Null);

            Object.DestroyImmediate(defaultProfileSO);
        }

        #endregion
    }
}
