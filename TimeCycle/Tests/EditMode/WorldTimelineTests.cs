using NUnit.Framework;
using UnityEngine;

namespace FakeMG.TimeCycle.Tests.EditMode
{
    public sealed class WorldTimelineTests
    {
        #region Public Methods

        [Test]
        public void RestoredTimeContinuesAcrossCycleBoundaries()
        {
            var timeline = new WorldTimeline(100);

            double cycleTimeSeconds = timeline.RestoreAuthoritativeTime(225);

            Assert.That(cycleTimeSeconds, Is.EqualTo(25));
            Assert.That(timeline.GetAuthoritativeTimeSeconds(cycleTimeSeconds), Is.EqualTo(225));

            timeline.RecordCompletedCycle();

            Assert.That(timeline.GetAuthoritativeTimeSeconds(cycleTimeSeconds), Is.EqualTo(325));
        }

        [TestCase(-1)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void InvalidRestoredTimeIsRejected(double authoritativeTimeSeconds)
        {
            var timeline = new WorldTimeline(100);

            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => timeline.RestoreAuthoritativeTime(authoritativeTimeSeconds));
        }

        [Test]
        public void DefaultPersistenceContainsTheConfiguredStartingTime()
        {
            TimeOfCycleProfileSO profileSO = ScriptableObject.CreateInstance<TimeOfCycleProfileSO>();
            try
            {
                var persistence = new WorldTimelinePersistence(profileSO);
                double startingTimeSeconds = profileSO.CycleDurationSeconds * profileSO.DefaultStartingProgress01;

                Assert.That(persistence.SaveData.AuthoritativeTimeSeconds, Is.EqualTo(startingTimeSeconds));

                persistence.Restore(new WorldTimelineSaveData { AuthoritativeTimeSeconds = startingTimeSeconds + 100d });
                persistence.Reset();

                Assert.That(persistence.SaveData.AuthoritativeTimeSeconds, Is.EqualTo(startingTimeSeconds));
            }
            finally
            {
                Object.DestroyImmediate(profileSO);
            }
        }

        #endregion
    }
}
