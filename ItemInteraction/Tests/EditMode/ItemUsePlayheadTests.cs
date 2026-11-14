using NUnit.Framework;
using UnityEngine;

namespace FakeMG.ItemInteraction.Tests
{
    public sealed class ItemUsePlayheadTests
    {
        private ItemTestFixtures _fixtures;
        private AnimationClip _followUpClip;
        private ItemUsePlayhead _playhead;

        #region Public Methods

        [SetUp]
        public void SetUp()
        {
            _fixtures = new ItemTestFixtures();
            _followUpClip = _fixtures.CreateClip();
            _fixtures.ProfileSO.ConfigureFollowUpUseAnimationsForEditor(_followUpClip);
            _playhead = new ItemUsePlayhead();
        }

        [TearDown]
        public void TearDown()
        {
            _fixtures.Dispose();
        }

        [Test]
        public void Start_PlaysTheFirstUseClipAtThePlaybackSpeed()
        {
            _playhead.Start(_fixtures.ProfileSO, playbackSpeedMultiplier: 2f, stopsAtSequenceEnd: false);

            Assert.That(_playhead.IsPlaying, Is.True);
            Assert.That(_playhead.CurrentPose.AnimationClip, Is.SameAs(_fixtures.UseClip));
            Assert.That(_playhead.CurrentPose.PlaybackSpeedMultiplier, Is.EqualTo(2f));
        }

        [Test]
        public void Advance_PastTheFirstClip_ReportsTheNextClipOnce()
        {
            _playhead.Start(_fixtures.ProfileSO, playbackSpeedMultiplier: 1f, stopsAtSequenceEnd: false);

            bool hasReachedNextClip = _playhead.Advance(_fixtures.UseClip.length);
            bool hasReachedAnotherClip = _playhead.Advance(0f);

            Assert.That(hasReachedNextClip, Is.True);
            Assert.That(hasReachedAnotherClip, Is.False);
            Assert.That(_playhead.CurrentPose.AnimationClip, Is.SameAs(_followUpClip));
        }

        [Test]
        public void Advance_ScalesTimeByThePlaybackSpeed()
        {
            _playhead.Start(_fixtures.ProfileSO, playbackSpeedMultiplier: 2f, stopsAtSequenceEnd: false);

            Assert.That(_playhead.Advance(_fixtures.UseClip.length * 0.5f), Is.True);
        }

        [Test]
        public void HasFinished_ForAUseItsOwnerEnds_StaysOnTheLastClip()
        {
            _playhead.Start(_fixtures.ProfileSO, playbackSpeedMultiplier: 1f, stopsAtSequenceEnd: false);

            _playhead.Advance(_fixtures.ProfileSO.UseDurationSeconds * 10f);

            Assert.That(_playhead.HasFinished, Is.False);
            Assert.That(_playhead.CurrentPose.AnimationClip, Is.SameAs(_followUpClip));
        }

        [Test]
        public void HasFinished_ForAOneOffUse_IsTrueAtTheSequenceEnd()
        {
            _playhead.Start(_fixtures.ProfileSO, playbackSpeedMultiplier: 1f, stopsAtSequenceEnd: true);

            _playhead.Advance(_fixtures.ProfileSO.UseDurationSeconds * 0.5f);
            Assert.That(_playhead.HasFinished, Is.False);

            _playhead.Advance(_fixtures.ProfileSO.UseDurationSeconds);
            Assert.That(_playhead.HasFinished, Is.True);
        }

        [Test]
        public void Stop_EndsPlayback()
        {
            _playhead.Start(_fixtures.ProfileSO, playbackSpeedMultiplier: 1f, stopsAtSequenceEnd: false);

            _playhead.Stop();

            Assert.That(_playhead.IsPlaying, Is.False);
        }

        #endregion
    }
}
