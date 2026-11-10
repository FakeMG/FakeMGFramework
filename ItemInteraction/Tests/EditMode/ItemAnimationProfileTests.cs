using System;
using NUnit.Framework;
using UnityEngine;

namespace FakeMG.ItemInteraction.Tests
{
    public sealed class ItemAnimationProfileTests
    {
        private ItemAnimationProfileSO _profileSO;
        private AnimationClip _holdingClip;
        private AnimationClip _useClip;

        #region Public Methods

        [SetUp]
        public void SetUp()
        {
            _profileSO = ScriptableObject.CreateInstance<ItemAnimationProfileSO>();
            _holdingClip = new AnimationClip();
            _useClip = new AnimationClip();
            _profileSO.ConfigureAnimationsForEditor(_holdingClip, _useClip);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_profileSO);
            UnityEngine.Object.DestroyImmediate(_holdingClip);
            UnityEngine.Object.DestroyImmediate(_useClip);
        }

        [Test]
        public void HoldingPose_UsesAuthoredClipAndOffsetIndependentlyOfLocomotion()
        {
            Vector3 offsetDegrees = new(10f, 20f, 30f);
            _profileSO.ConfigureRotationOffsetsForEditor(offsetDegrees, Vector3.zero);

            ItemAnimationPose pose = _profileSO.GetHoldingPose();

            Assert.That(pose.AnimationClip, Is.SameAs(_holdingClip));
            Assert.That(pose.UpperBodyRotationOffsetDegrees, Is.EqualTo(offsetDegrees));
        }

        [Test]
        public void UsePose_UsesItsOwnAuthoredClipAndOffset()
        {
            Vector3 holdingOffsetDegrees = new(10f, 20f, 30f);
            Vector3 useOffsetDegrees = new(-5f, 40f, 0f);
            _profileSO.ConfigureRotationOffsetsForEditor(holdingOffsetDegrees, useOffsetDegrees);

            Assert.That(_profileSO.GetHoldingPose().UpperBodyRotationOffsetDegrees, Is.EqualTo(holdingOffsetDegrees));
            Assert.That(_profileSO.GetUsePose().AnimationClip, Is.SameAs(_useClip));
            Assert.That(_profileSO.GetUsePose().UpperBodyRotationOffsetDegrees, Is.EqualTo(useOffsetDegrees));
        }

        [Test]
        public void Validation_AcceptsOnlyTheTwoRequiredClips()
        {
            Assert.DoesNotThrow(_profileSO.ValidateOrThrow);
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public void Validation_RejectsMissingRequiredClips(bool hasHoldingClip, bool hasUseClip)
        {
            _profileSO.ConfigureAnimationsForEditor(hasHoldingClip ? _holdingClip : null, hasUseClip ? _useClip : null);

            Assert.Throws<InvalidOperationException>(_profileSO.ValidateOrThrow);
        }

        #endregion
    }
}
