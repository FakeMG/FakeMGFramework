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
        private AnimationClip _firstFollowUpClip;
        private AnimationClip _secondFollowUpClip;

        #region Public Methods

        [SetUp]
        public void SetUp()
        {
            _profileSO = ScriptableObject.CreateInstance<ItemAnimationProfileSO>();
            _holdingClip = new AnimationClip();
            _useClip = new AnimationClip();
            _firstFollowUpClip = new AnimationClip();
            _secondFollowUpClip = new AnimationClip();
            _profileSO.ConfigureAnimationsForEditor(_holdingClip, _useClip);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_profileSO);
            UnityEngine.Object.DestroyImmediate(_holdingClip);
            UnityEngine.Object.DestroyImmediate(_useClip);
            UnityEngine.Object.DestroyImmediate(_firstFollowUpClip);
            UnityEngine.Object.DestroyImmediate(_secondFollowUpClip);
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
            ItemAnimationPose usePose = _profileSO.GetUsePose(0, 1f);
            Assert.That(usePose.AnimationClip, Is.SameAs(_useClip));
            Assert.That(usePose.UpperBodyRotationOffsetDegrees, Is.EqualTo(useOffsetDegrees));
        }

        [Test]
        public void UseSequence_WithoutFollowUpClips_StaysOnTheUseClip()
        {
            Assert.That(_profileSO.UseDurationSeconds, Is.EqualTo(_useClip.length));
            Assert.That(_profileSO.GetUseClipIndex(0f), Is.Zero);
            Assert.That(_profileSO.GetUseClipIndex(_useClip.length * 10f), Is.Zero);
        }

        [Test]
        public void UseSequence_WithFollowUpClips_AdvancesAtEachClipEndAndStaysOnTheLastClip()
        {
            _profileSO.ConfigureFollowUpUseAnimationsForEditor(_firstFollowUpClip, _secondFollowUpClip);
            float firstFollowUpStartSeconds = _useClip.length;
            float secondFollowUpStartSeconds = firstFollowUpStartSeconds + _firstFollowUpClip.length;
            float sequenceEndSeconds = secondFollowUpStartSeconds + _secondFollowUpClip.length;

            Assert.That(_profileSO.UseDurationSeconds, Is.EqualTo(sequenceEndSeconds));
            Assert.That(_profileSO.GetUseClipIndex(0f), Is.Zero);
            Assert.That(_profileSO.GetUseClipIndex(firstFollowUpStartSeconds * 0.99f), Is.Zero);
            Assert.That(_profileSO.GetUseClipIndex(firstFollowUpStartSeconds), Is.EqualTo(1));
            Assert.That(_profileSO.GetUseClipIndex(secondFollowUpStartSeconds), Is.EqualTo(2));
            Assert.That(_profileSO.GetUseClipIndex(sequenceEndSeconds * 10f), Is.EqualTo(2));
        }

        [Test]
        public void UsePose_ForFollowUpClip_UsesThatClipWithTheUseOffsetAndRequestedSpeed()
        {
            Vector3 useOffsetDegrees = new(-5f, 40f, 0f);
            _profileSO.ConfigureRotationOffsetsForEditor(Vector3.zero, useOffsetDegrees);
            _profileSO.ConfigureFollowUpUseAnimationsForEditor(_firstFollowUpClip, _secondFollowUpClip);

            ItemAnimationPose pose = _profileSO.GetUsePose(2, 1.5f);

            Assert.That(pose.AnimationClip, Is.SameAs(_secondFollowUpClip));
            Assert.That(pose.UpperBodyRotationOffsetDegrees, Is.EqualTo(useOffsetDegrees));
            Assert.That(pose.PlaybackSpeedMultiplier, Is.EqualTo(1.5f));
        }

        [Test]
        public void HoldingPose_PlaysAtAuthoredSpeed()
        {
            Assert.That(_profileSO.GetHoldingPose().PlaybackSpeedMultiplier, Is.EqualTo(1f));
        }

        [Test]
        public void ActionMomentSeconds_IsTheNormalizedMomentAcrossTheWholeUseSequence()
        {
            _profileSO.ConfigureFollowUpUseAnimationsForEditor(_firstFollowUpClip);
            _profileSO.ConfigurePlaybackForEditor(fadeDurationSeconds: 0.1f, actionMomentNormalized: 0.25f);

            Assert.That(_profileSO.ActionMomentSeconds, Is.EqualTo(_profileSO.UseDurationSeconds * 0.25f).Within(0.0001f));
        }

        [Test]
        public void Validation_RejectsMissingFollowUpClip()
        {
            _profileSO.ConfigureFollowUpUseAnimationsForEditor(_firstFollowUpClip, null);

            Assert.Throws<InvalidOperationException>(_profileSO.ValidateOrThrow);
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
