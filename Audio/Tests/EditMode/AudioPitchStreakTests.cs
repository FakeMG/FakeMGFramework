using System;
using NUnit.Framework;

namespace FakeMG.Audio.Tests
{
    public sealed class AudioPitchStreakTests
    {
        #region Public Methods

        [Test]
        public void FirstPlayback_UsesBasePitch()
        {
            AudioPitchStreak streak = new(0.06f, 1.5f, 0.6f);

            Assert.That(streak.GetNextPitchMultiplier(0f), Is.EqualTo(1f));
        }

        [Test]
        public void QuickSuccessivePlayback_RaisesPitch()
        {
            AudioPitchStreak streak = new(0.06f, 1.5f, 0.6f);
            streak.RecordPlayback(0f);

            Assert.That(streak.GetNextPitchMultiplier(0.1f), Is.EqualTo(1.06f).Within(0.0001f));
            streak.RecordPlayback(0.1f);
            Assert.That(streak.GetNextPitchMultiplier(0.2f), Is.EqualTo(1.12f).Within(0.0001f));
        }

        [Test]
        public void SuppressedPlayback_DoesNotAdvancePitchOrExtendStreak()
        {
            AudioPitchStreak streak = new(0.06f, 1.5f, 0.6f);
            streak.RecordPlayback(0f);

            Assert.That(streak.GetNextPitchMultiplier(0.1f), Is.EqualTo(1.06f).Within(0.0001f));
            Assert.That(streak.GetNextPitchMultiplier(0.2f), Is.EqualTo(1.06f).Within(0.0001f));
            Assert.That(streak.GetNextPitchMultiplier(0.6f), Is.EqualTo(1f));
        }

        [TestCase(0.6f)]
        [TestCase(3f)]
        [TestCase(-1f)]
        public void PauseOrClockReset_ReturnsToBasePitch(float currentTimeSeconds)
        {
            AudioPitchStreak streak = new(0.06f, 1.5f, 0.6f);
            streak.RecordPlayback(0f);

            Assert.That(streak.GetNextPitchMultiplier(currentTimeSeconds), Is.EqualTo(1f));
        }

        [Test]
        public void LongStreak_ClampsPitchAtMaximum()
        {
            AudioPitchStreak streak = new(0.3f, 1.5f, 0.6f);
            streak.RecordPlayback(0f);
            streak.RecordPlayback(0.1f);
            streak.RecordPlayback(0.2f);

            Assert.That(streak.GetNextPitchMultiplier(0.3f), Is.EqualTo(1.5f));
        }

        [TestCase(0f, 1.5f, 0.6f)]
        [TestCase(float.NaN, 1.5f, 0.6f)]
        [TestCase(0.06f, 0.9f, 0.6f)]
        [TestCase(0.06f, 3.1f, 0.6f)]
        [TestCase(0.06f, float.PositiveInfinity, 0.6f)]
        [TestCase(0.06f, 1.5f, 0f)]
        public void InvalidStartupTuning_IsRejected(float pitchStepMultiplier, float maximumPitchMultiplier, float resetAfterSeconds)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new AudioPitchStreak(pitchStepMultiplier, maximumPitchMultiplier, resetAfterSeconds));
        }

        #endregion
    }
}
