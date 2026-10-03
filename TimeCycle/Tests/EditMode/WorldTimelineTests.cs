using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FakeMG.TimeCycle.Tests.EditMode
{
    public sealed class WorldTimelineTests
    {
        private TimeOfCycleProfileSO _profileSO;
        private CyclePeriodSO _periodSO;
        private TimeOfCycleService _time;
        private WorldTimelinePersistence _persistence;
        private WorldTimelineService _timeline;
        private WorldTimelineCycleSubscriber _subscriber;

        #region Public Methods

        [SetUp]
        public void SetUp()
        {
            _periodSO = ScriptableObject.CreateInstance<CyclePeriodSO>();
            _periodSO.ConfigureForEditor("whole_cycle", "Whole Cycle");
            _profileSO = ScriptableObject.CreateInstance<TimeOfCycleProfileSO>();
            _profileSO.ConfigureForEditor(100d, 0.1d, 0d, false, 0f, AnimationCurve.Linear(0f, 0f, 1f, 1f),
                new[] { new CyclePeriodDefinition(_periodSO, 0d) }, Array.Empty<CycleOutputDefinition>());
            _time = new TimeOfCycleService(_profileSO, Array.Empty<ITimeOfCycleOutputApplicator>());
            _time.Initialize();
            _persistence = new WorldTimelinePersistence(_profileSO);
            _timeline = new WorldTimelineService(_time, _profileSO, _persistence);
            _timeline.Initialize();
            _subscriber = new WorldTimelineCycleSubscriber(_time, _timeline);
            _subscriber.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _subscriber.Dispose();
            _timeline.Dispose();
            _time.Dispose();
            UnityEngine.Object.DestroyImmediate(_profileSO);
            UnityEngine.Object.DestroyImmediate(_periodSO);
        }

        [Test]
        public void RestoredTimeContinuesAcrossCycleBoundaries()
        {
            _persistence.Restore(new WorldTimelineSaveData { AuthoritativeTimeSeconds = 225d });
            _timeline.ApplyLoadedDataAsync(default).GetAwaiter().GetResult();
            Assert.That(_time.CurrentState.CycleTimeSeconds, Is.EqualTo(25d));
            Assert.That(_timeline.AuthoritativeTimeSeconds, Is.EqualTo(225d));

            _time.ExecuteTimeCommandAsync(TimeCommand.SimulatedAdvance(0d, TimeMovementDirection.Forward,
                new TimeCommandTransition(0f))).GetAwaiter().GetResult();
            _time.ExecuteTimeCommandAsync(TimeCommand.SimulatedAdvance(25d, TimeMovementDirection.Forward,
                new TimeCommandTransition(0f))).GetAwaiter().GetResult();

            Assert.That(_timeline.AuthoritativeTimeSeconds, Is.EqualTo(325d));
            Assert.That(_timeline.CurrentDay, Is.EqualTo(4));
        }

        [TestCase(-1d)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void InvalidRestoredTimeRejectsSaveAndPreservesPreviousRevision(double authoritativeTimeSeconds)
        {
            var saved = new WorldTimelineSaveData { AuthoritativeTimeSeconds = authoritativeTimeSeconds };
            int revision = _persistence.Revision;
            Assert.That(WorldTimelinePersistence.TryValidate(saved, out string failureReason), Is.False);
            Assert.That(failureReason, Does.Contain("invalid"));
#if LOGGER_ENABLED
            LogAssert.Expect(LogType.Error, new Regex("World timeline save state is invalid"));
#endif

            _persistence.Restore(saved);

            Assert.That(_persistence.Revision, Is.EqualTo(revision));
            Assert.That(_persistence.SaveData.AuthoritativeTimeSeconds, Is.EqualTo(10d));
            Assert.That(_timeline.AuthoritativeTimeSeconds, Is.EqualTo(10d));
        }

        [Test]
        public void DefaultPersistenceContainsTheConfiguredStartingTime()
        {
            Assert.That(_persistence.SaveData.AuthoritativeTimeSeconds, Is.EqualTo(10d));
            _persistence.Restore(new WorldTimelineSaveData { AuthoritativeTimeSeconds = 110d });

            _persistence.Reset();

            Assert.That(_persistence.SaveData.AuthoritativeTimeSeconds, Is.EqualTo(10d));
        }

        #endregion
    }
}
