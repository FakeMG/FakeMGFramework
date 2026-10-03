using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Text.RegularExpressions;

namespace FakeMG.TimeCycle.Tests.EditMode
{
    public sealed class TimeCycleObservationTests
    {
        private readonly List<CyclePeriodSO> _periodsSO = new();
        private TimeOfCycleProfileSO _profileSO;
        private TimeOfCycleOverrideSO _overrideSO;
        private BoolCycleOutputKeySO _outputKeySO;
        private IntCycleOutputKeySO _intOutputKeySO;
        private TimeOfCycleService _time;
        private WorldTimelineService _timeline;
        private WorldTimelineCycleSubscriber _subscriber;
        private WorldTimelinePersistence _persistence;

        #region Public Methods

        [SetUp]
        public void SetUp()
        {
            _profileSO = ScriptableObject.CreateInstance<TimeOfCycleProfileSO>();
            _overrideSO = ScriptableObject.CreateInstance<TimeOfCycleOverrideSO>();
            _outputKeySO = ScriptableObject.CreateInstance<BoolCycleOutputKeySO>();
            _intOutputKeySO = ScriptableObject.CreateInstance<IntCycleOutputKeySO>();
            ConfigureProfile(new[]
            {
                new CyclePeriodDefinition(CreatePeriodSO("day"), 0.25d),
                new CyclePeriodDefinition(CreatePeriodSO("night"), 0.75d)
            });
        }

        [TearDown]
        public void TearDown()
        {
            _subscriber?.Dispose();
            _timeline?.Dispose();
            _time?.Dispose();
            foreach (CyclePeriodSO periodSO in _periodsSO) UnityEngine.Object.DestroyImmediate(periodSO);
            _periodsSO.Clear();
            UnityEngine.Object.DestroyImmediate(_profileSO);
            UnityEngine.Object.DestroyImmediate(_overrideSO);
            UnityEngine.Object.DestroyImmediate(_outputKeySO);
            UnityEngine.Object.DestroyImmediate(_intOutputKeySO);
        }

        [Test]
        public void ActiveLayoutResolvesFourPhasesAndMidnightSpan()
        {
            ConfigureProfile(new[]
            {
                new CyclePeriodDefinition(CreatePeriodSO("dawn"), 0.2d), new CyclePeriodDefinition(CreatePeriodSO("day"), 0.3d),
                new CyclePeriodDefinition(CreatePeriodSO("dusk"), 0.7d), new CyclePeriodDefinition(CreatePeriodSO("night"), 0.8d)
            });
            Initialize();
            Assert.That(_time.ActiveLayout.Periods.Count, Is.EqualTo(4));
            Assert.That(_time.ActiveLayout.Periods[3].DurationProgress01, Is.EqualTo(0.4d).Within(1e-12));
            Assert.That(_time.CurrentState.CurrentPeriodId.Value, Is.EqualTo("night"));
        }

        [Test]
        public void TiedStartsKeepAuthoredOrderAndSkipZeroPhase()
        {
            ConfigureProfile(new[]
            {
                new CyclePeriodDefinition(CreatePeriodSO("zero"), 0d), new CyclePeriodDefinition(CreatePeriodSO("day"), 0d),
                new CyclePeriodDefinition(CreatePeriodSO("night"), 0.75d)
            });
            Initialize();
            Assert.That(_time.ActiveLayout.Periods[0].PeriodId.Value, Is.EqualTo("zero"));
            Assert.That(_time.ActiveLayout.Periods[0].DurationProgress01, Is.Zero);
            Assert.That(_time.CurrentState.CurrentPeriodId.Value, Is.EqualTo("day"));
            _time.ExecuteTimeCommandAsync(TimeCommand.Immediate(80d)).GetAwaiter().GetResult();
            _time.ExecuteTimeCommandAsync(TimeCommand.SimulatedAdvance(10d, TimeMovementDirection.Forward,
                new TimeCommandTransition(0f))).GetAwaiter().GetResult();
            Assert.That(_time.CurrentState.CurrentPeriodId.Value, Is.EqualTo("day"));
            Assert.That(_timeline.CurrentDay, Is.EqualTo(2));
        }

        [Test]
        public void TinyPhaseKeepsProportionalDuration()
        {
            ConfigureProfile(new[]
            {
                new CyclePeriodDefinition(CreatePeriodSO("tiny"), 0d),
                new CyclePeriodDefinition(CreatePeriodSO("day"), 1e-8)
            });
            Initialize();
            Assert.That(_time.ActiveLayout.Periods[0].DurationProgress01, Is.EqualTo(1e-8));
        }

        [Test]
        public void SinglePhaseAwayFromMidnightCoversWholeCycle()
        {
            ConfigureProfile(new[] { new CyclePeriodDefinition(CreatePeriodSO("day"), 0.2d) });
            Initialize();
            Assert.That(_time.ActiveLayout.Periods[0].DurationProgress01, Is.EqualTo(1d));
        }

        [Test]
        public void RejectedOverrideRetainsLayoutAndClock()
        {
            Initialize();
            TimeOfCycleLayout previous = _time.ActiveLayout;
            _overrideSO.ConfigureCycleForEditor(-1d, new[] { new CyclePeriodDefinition(CreatePeriodSO("day"), 0d) });
            Assert.That(_time.SetRuntimeOverride(_overrideSO), Is.False);
            Assert.That(_time.ActiveLayout, Is.SameAs(previous));
            Assert.That(_time.CurrentState.NormalizedCycleProgress01, Is.EqualTo(0.1d));
        }

        [Test]
        public void OnlyZeroDurationOutputValuesRejectInitialization()
        {
            var output = new BoolCycleOutputDefinition(_outputKeySO, 0f, Array.Empty<BoolCyclePoint>(),
                new[] { new BoolPeriodValue(CreatePeriodSO("zero"), true) });
            ConfigureProfile(new[]
            {
                new CyclePeriodDefinition(CreatePeriodSO("zero"), 0d),
                new CyclePeriodDefinition(CreatePeriodSO("day"), 0d)
            }, output);
#if LOGGER_ENABLED
            LogAssert.Expect(LogType.Error, new Regex("Time-of-cycle initialization failed"));
#endif
            Initialize();
            Assert.That(_time.IsInitialized, Is.False);
            Assert.That(_time.ActiveLayout, Is.Null);
        }

        [Test]
        public void ZeroDurationPeriodValueCannotReplaceTheActiveOutput()
        {
            var output = new BoolCycleOutputDefinition(_outputKeySO, 0f, Array.Empty<BoolCyclePoint>(),
                new[] { new BoolPeriodValue(CreatePeriodSO("zero"), true), new BoolPeriodValue(CreatePeriodSO("day"), false) });
            ConfigureProfile(new[]
            {
                new CyclePeriodDefinition(CreatePeriodSO("zero"), 0d),
                new CyclePeriodDefinition(CreatePeriodSO("day"), 0d)
            }, output);
            var recorder = new BoolOutputRecorder(_outputKeySO);
            _time = new TimeOfCycleService(_profileSO, new[] { recorder });

            _time.Initialize();
            _time.ExecuteTimeCommandAsync(TimeCommand.Immediate(0d)).GetAwaiter().GetResult();

            Assert.That(_time.IsInitialized, Is.True);
            Assert.That(recorder.LastValue, Is.False);
        }

        [Test]
        public void ForwardJumpDoesNotEmitIntermediatePhaseOrCycleEvents()
        {
            Initialize();
            using var recorder = new ClockNotificationSubscriber(_time);

            _timeline.JumpToDayAsync(5, 0.6d).GetAwaiter().GetResult();

            Assert.That(recorder.CompletedCycleCount, Is.Zero);
            Assert.That(recorder.ChangedPeriodIds, Is.EqualTo(new[] { "day" }));
            Assert.That(_timeline.CurrentDay, Is.EqualTo(5));
        }

        [Test]
        public void SimulatedWrapDoesNotPublishZeroDurationPeriod()
        {
            ConfigureProfile(new[]
            {
                new CyclePeriodDefinition(CreatePeriodSO("zero"), 0d), new CyclePeriodDefinition(CreatePeriodSO("day"), 0d),
                new CyclePeriodDefinition(CreatePeriodSO("night"), 0.75d)
            });
            Initialize();
            _time.ExecuteTimeCommandAsync(TimeCommand.Immediate(80d)).GetAwaiter().GetResult();
            using var recorder = new ClockNotificationSubscriber(_time);

            _time.ExecuteTimeCommandAsync(TimeCommand.SimulatedAdvance(10d, TimeMovementDirection.Forward,
                new TimeCommandTransition(0f))).GetAwaiter().GetResult();

            Assert.That(recorder.ChangedPeriodIds, Is.EqualTo(new[] { "day" }));
            Assert.That(recorder.CompletedCycleCount, Is.EqualTo(1));
        }

        [Test]
        public void ForwardJumpSetsDayProgressAndCapturedWorldTime()
        {
            Initialize();
            var result = _timeline.JumpToDayAsync(4, 0.6d).GetAwaiter().GetResult();
            Assert.That(result.Status, Is.EqualTo(TimeCommandStatus.Completed));
            Assert.That(_timeline.CurrentDay, Is.EqualTo(4));
            Assert.That(_time.CurrentState.NormalizedCycleProgress01, Is.EqualTo(0.6d));
            Assert.That(_timeline.AuthoritativeTimeSeconds, Is.EqualTo(360d));
            Assert.That(_timeline.TryCapture(out var saved, out _), Is.True);
            Assert.That(saved.AuthoritativeTimeSeconds, Is.EqualTo(360d));
        }

        [Test]
        public void JumpNotificationsObserveCommittedDayProgressAndEnvironment()
        {
            var output = new BoolCycleOutputDefinition(_outputKeySO, 0f,
                new[] { new BoolCyclePoint(0d, false), new BoolCyclePoint(0.25d, true) }, Array.Empty<BoolPeriodValue>());
            ConfigureProfile(_profileSO.Periods, output);
            var outputRecorder = new BoolOutputRecorder(_outputKeySO);
            Initialize(outputRecorder);
            using var subscriber = new CommittedWorldTimeSubscriber(_time, _timeline, outputRecorder);

            _timeline.JumpToDayAsync(4, 0.6d).GetAwaiter().GetResult();

            Assert.That(subscriber.ObservedDays, Is.EqualTo(new[] { 4L, 4L }));
            Assert.That(subscriber.ObservedProgress01, Is.EqualTo(new[] { 0.6d, 0.6d }));
            Assert.That(subscriber.ObservedPresentationTimeSeconds, Is.EqualTo(new[] { 60d, 60d }));
            Assert.That(subscriber.ObservedWorldTimeSeconds, Is.EqualTo(new[] { 360d, 360d }));
            Assert.That(subscriber.ObservedOutputValues, Is.EqualTo(new[] { true, true }));
        }

        [Test]
        public void ImmediateCommandNotifiesOnlyAfterCommittingPublicStateAndOutputs()
        {
            var output = new BoolCycleOutputDefinition(_outputKeySO, 0f,
                new[] { new BoolCyclePoint(0d, false), new BoolCyclePoint(0.25d, true) }, Array.Empty<BoolPeriodValue>());
            ConfigureProfile(_profileSO.Periods, output);
            var outputRecorder = new BoolOutputRecorder(_outputKeySO);
            Initialize(outputRecorder);
            using var subscriber = new CommittedWorldTimeSubscriber(_time, _timeline, outputRecorder);

            _time.ExecuteTimeCommandAsync(TimeCommand.Immediate(60d)).GetAwaiter().GetResult();

            Assert.That(subscriber.ObservedDays, Is.EqualTo(new[] { 1L, 1L }));
            Assert.That(subscriber.ObservedProgress01, Is.EqualTo(new[] { 0.6d, 0.6d }));
            Assert.That(subscriber.ObservedOutputValues, Is.EqualTo(new[] { true, true }));
        }

        [TestCase(0, 0.5d)]
        [TestCase(1, -0.1d)]
        [TestCase(1, 1d)]
        [TestCase(1, double.NaN)]
        [TestCase(1, 0d)]
        [TestCase(long.MaxValue, 0d)]
        public void InvalidBackwardOrExcessiveJumpPreservesState(long dayNumber, double progress01)
        {
            Initialize();
            var result = _timeline.JumpToDayAsync(dayNumber, progress01).GetAwaiter().GetResult();
            Assert.That(result.Status, Is.EqualTo(TimeCommandStatus.Rejected));
            Assert.That(_timeline.CurrentDay, Is.EqualTo(1));
            Assert.That(_timeline.AuthoritativeTimeSeconds, Is.EqualTo(10d));
        }

        [Test]
        public void CancelledAndBlockedJumpsPreserveWorldTime()
        {
            Initialize();
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            Assert.That(_timeline.JumpToDayAsync(2, 0.5d, cancellation.Token).GetAwaiter().GetResult().Status,
                Is.EqualTo(TimeCommandStatus.Cancelled));
            using IDisposable control = _time.RegisterControl(TimeControlRequest.Pause(100, false));
            Assert.That(_timeline.JumpToDayAsync(2, 0.5d).GetAwaiter().GetResult().Status, Is.EqualTo(TimeCommandStatus.Rejected));
            Assert.That(_timeline.AuthoritativeTimeSeconds, Is.EqualTo(10d));
        }

        [Test]
        public void UnchangedDestinationDoesNotChangeTime()
        {
            Initialize();
            Assert.That(_timeline.JumpToDayAsync(1, 0.1d).GetAwaiter().GetResult().Status, Is.EqualTo(TimeCommandStatus.Completed));
            Assert.That(_timeline.AuthoritativeTimeSeconds, Is.EqualTo(10d));
        }

        [Test]
        public void DurationOverridePreservesDayWorldTimeAndSaveRoundTrip()
        {
            Initialize();
            _timeline.JumpToDayAsync(4, 0.6d).GetAwaiter().GetResult();
            _overrideSO.ConfigureCycleForEditor(50d, new[] { new CyclePeriodDefinition(CreatePeriodSO("day"), 0d) });
            Assert.That(_time.SetRuntimeOverride(_overrideSO), Is.True);
            Assert.That(_timeline.CurrentDay, Is.EqualTo(4));
            Assert.That(_timeline.AuthoritativeTimeSeconds, Is.EqualTo(360d));
            Assert.That(_time.CurrentState.CycleTimeSeconds, Is.EqualTo(30d));
            _timeline.TryCapture(out var saved, out _);
            _timeline.JumpToDayAsync(5, 0.8d).GetAwaiter().GetResult();
            _persistence.Restore(saved);
            _timeline.ApplyLoadedDataAsync(default).GetAwaiter().GetResult();
            Assert.That(_timeline.CurrentDay, Is.EqualTo(4));
            Assert.That(_timeline.AuthoritativeTimeSeconds, Is.EqualTo(360d));
            Assert.That(_time.CurrentState.CycleTimeSeconds, Is.EqualTo(30d));
        }

        [Test]
        public void IntegerOutputAppliesPeriodValuesAndTimelinePointPrecedence()
        {
            CyclePeriodSO daySO = CreatePeriodSO("day");
            CyclePeriodSO nightSO = CreatePeriodSO("night");
            var output = new IntCycleOutputDefinition(_intOutputKeySO, 0f, new[] { new IntCyclePoint(0.75d, 7) },
                new[] { new IntPeriodValue(daySO, 2), new IntPeriodValue(nightSO, 4) });
            ConfigureProfile(new[] { new CyclePeriodDefinition(daySO, 0.25d), new CyclePeriodDefinition(nightSO, 0.75d) }, output);
            var recorder = new IntOutputRecorder(_intOutputKeySO);
            Initialize(recorder);

            _time.ExecuteTimeCommandAsync(TimeCommand.Immediate(25d)).GetAwaiter().GetResult();
            Assert.That(recorder.LastValue, Is.EqualTo(2));

            _time.ExecuteTimeCommandAsync(TimeCommand.Immediate(75d)).GetAwaiter().GetResult();
            Assert.That(recorder.LastValue, Is.EqualTo(7));
        }

        [Test]
        public void ExactTravelBudgetSucceeds()
        {
            Initialize();
            long destinationDay = 1L + _time.ActiveLayout.MaximumCycleCrossingsPerUpdate;

            var result = _timeline.JumpToDayAsync(destinationDay, 0.1d).GetAwaiter().GetResult();

            Assert.That(result.Status, Is.EqualTo(TimeCommandStatus.Completed));
            Assert.That(_timeline.CurrentDay, Is.EqualTo(destinationDay));
            Assert.That(_timeline.AuthoritativeTimeSeconds, Is.EqualTo((destinationDay - 1) * 100d + 10d));
        }

        [Test]
        public void FirstCycleBeyondTravelBudgetRejectsWithoutMutation()
        {
            Initialize();
            long destinationDay = 2L + _time.ActiveLayout.MaximumCycleCrossingsPerUpdate;
            using var recorder = new ClockNotificationSubscriber(_time);

            var result = _timeline.JumpToDayAsync(destinationDay, 0.1d).GetAwaiter().GetResult();

            Assert.That(result.Status, Is.EqualTo(TimeCommandStatus.Rejected));
            Assert.That(_timeline.CurrentDay, Is.EqualTo(1));
            Assert.That(_timeline.AuthoritativeTimeSeconds, Is.EqualTo(10d));
            Assert.That(recorder.ChangedPeriodIds, Is.Empty);
            Assert.That(recorder.CompletedCycleCount, Is.Zero);
        }

        [TestCase(0d, 1L)]
        [TestCase(100d, 2L)]
        [TestCase(300d, 4L)]
        public void RestoreAtCycleBoundarySetsOneBasedDayAndZeroProgress(double worldTimeSeconds, long expectedDay)
        {
            Initialize();
            _timeline.JumpToDayAsync(5, 0.8d).GetAwaiter().GetResult();
            _persistence.Restore(new WorldTimelineSaveData { AuthoritativeTimeSeconds = worldTimeSeconds });

            _timeline.ApplyLoadedDataAsync(default).GetAwaiter().GetResult();

            Assert.That(_timeline.CurrentDay, Is.EqualTo(expectedDay));
            Assert.That(_timeline.AuthoritativeTimeSeconds, Is.EqualTo(worldTimeSeconds));
            Assert.That(_time.CurrentState.NormalizedCycleProgress01, Is.Zero);
            Assert.That(_time.CurrentState.PresentationTimeSeconds, Is.Zero);
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void NonFiniteImmediateCommandRejectsAndPreservesState(double targetTimeSeconds)
        {
            var outputRecorder = InitializeRecordedOutput();
            using var recorder = new ClockNotificationSubscriber(_time);

            var result = _time.ExecuteTimeCommandAsync(TimeCommand.Immediate(targetTimeSeconds)).GetAwaiter().GetResult();

            AssertRejectedCommandPreservedState(result, recorder, outputRecorder);
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void NonFiniteCommandTransitionRejectsAndPreservesState(float transitionDurationSeconds)
        {
            var outputRecorder = InitializeRecordedOutput();
            using var recorder = new ClockNotificationSubscriber(_time);
            var command = TimeCommand.SimulatedAdvance(60d, TimeMovementDirection.Forward,
                new TimeCommandTransition(transitionDurationSeconds));

            var result = _time.ExecuteTimeCommandAsync(command).GetAwaiter().GetResult();

            AssertRejectedCommandPreservedState(result, recorder, outputRecorder);
        }

        [Test]
        public void InvalidCommandDirectionRejectsAndPreservesState()
        {
            var outputRecorder = InitializeRecordedOutput();
            using var recorder = new ClockNotificationSubscriber(_time);
            var command = TimeCommand.SimulatedAdvance(60d, (TimeMovementDirection)123, new TimeCommandTransition(0f));

            var result = _time.ExecuteTimeCommandAsync(command).GetAwaiter().GetResult();

            AssertRejectedCommandPreservedState(result, recorder, outputRecorder);
        }

        [TestCase(-0.001d)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void InvalidAdvancementRateRejectsAndPreservesState(double rateCycleSecondsPerRealSecond)
        {
            var outputRecorder = InitializeRecordedOutput();
            Assert.That(_time.TrySetAdvancementRate(3d), Is.True);
            _time.SetAutomaticAdvancementEnabled(true);
            using var recorder = new ClockNotificationSubscriber(_time);

            bool isAccepted = _time.TrySetAdvancementRate(rateCycleSecondsPerRealSecond);

            Assert.That(isAccepted, Is.False);
            Assert.That(outputRecorder.LastValue, Is.False);
            Assert.That(_time.CurrentState.AdvancementRateCycleSecondsPerRealSecond, Is.EqualTo(3d));
            Assert.That(_timeline.AuthoritativeTimeSeconds, Is.EqualTo(10d));
            Assert.That(recorder.ChangedPeriodIds, Is.Empty);
            Assert.That(recorder.CompletedCycleCount, Is.Zero);
        }

        [Test]
        public void UninitializedClockRejectsWorldJump()
        {
            _time = new TimeOfCycleService(_profileSO, Array.Empty<ITimeOfCycleOutputApplicator>());
            _persistence = new WorldTimelinePersistence(_profileSO);
            _timeline = new WorldTimelineService(_time, _profileSO, _persistence);

            var result = _timeline.JumpToDayAsync(2, 0.5d).GetAwaiter().GetResult();

            Assert.That(result.Status, Is.EqualTo(TimeCommandStatus.Rejected));
            Assert.That(_timeline.CurrentDay, Is.EqualTo(1));
            Assert.That(_time.IsInitialized, Is.False);
        }

        #endregion

        #region Private Methods

        private void AssertRejectedCommandPreservedState(TimeCommandResult result, ClockNotificationSubscriber recorder, BoolOutputRecorder outputRecorder)
        {
            Assert.That(result.Status, Is.EqualTo(TimeCommandStatus.Rejected));
            Assert.That(outputRecorder.LastValue, Is.False);
            Assert.That(recorder.CommandStatuses, Is.EqualTo(new[] { TimeCommandStatus.Rejected }));
            Assert.That(_time.CurrentState.CycleTimeSeconds, Is.EqualTo(10d));
            Assert.That(_time.CurrentState.PresentationTimeSeconds, Is.EqualTo(10d));
            Assert.That(_timeline.AuthoritativeTimeSeconds, Is.EqualTo(10d));
            Assert.That(recorder.ChangedPeriodIds, Is.Empty);
            Assert.That(recorder.CompletedCycleCount, Is.Zero);
        }

        private BoolOutputRecorder InitializeRecordedOutput()
        {
            var output = new BoolCycleOutputDefinition(_outputKeySO, 0f,
                new[] { new BoolCyclePoint(0d, false), new BoolCyclePoint(0.5d, true) }, Array.Empty<BoolPeriodValue>());
            ConfigureProfile(_profileSO.Periods, output);
            var recorder = new BoolOutputRecorder(_outputKeySO);
            Initialize(recorder);
            return recorder;
        }

        private CyclePeriodSO CreatePeriodSO(string periodId)
        {
            var periodSO = ScriptableObject.CreateInstance<CyclePeriodSO>();
            periodSO.ConfigureForEditor(periodId, periodId);
            _periodsSO.Add(periodSO);
            return periodSO;
        }
        private void ConfigureProfile(IEnumerable<CyclePeriodDefinition> periods, params CycleOutputDefinition[] outputs)
        {
            _profileSO.ConfigureForEditor(100d, 0.1d, 0d, false, 0f, AnimationCurve.Linear(0f, 0f, 1f, 1f), periods, outputs);
        }

        private void Initialize(params ITimeOfCycleOutputApplicator[] outputApplicators)
        {
            _time = new TimeOfCycleService(_profileSO, outputApplicators);
            _time.Initialize();
            _persistence = new WorldTimelinePersistence(_profileSO);
            _timeline = new WorldTimelineService(_time, _profileSO, _persistence);
            _timeline.Initialize();
            _subscriber = new WorldTimelineCycleSubscriber(_time, _timeline);
            _subscriber.Initialize();
        }

        #endregion

        private sealed class BoolOutputRecorder : ITimeOfCycleOutputApplicator
        {
            private readonly BoolCycleOutputKeySO _keySO;
            public IReadOnlyList<CycleOutputKeySO> RequiredOutputKeys { get; }
            public bool LastValue { get; private set; }

            public BoolOutputRecorder(BoolCycleOutputKeySO keySO)
            {
                _keySO = keySO;
                RequiredOutputKeys = new[] { keySO };
            }

            #region Public Methods

            public void Apply(IReadOnlyCycleOutputState state)
            {
                Assert.That(state.TryGetValue(_keySO, out bool value), Is.True);
                LastValue = value;
            }

            #endregion
        }

        private sealed class CommittedWorldTimeSubscriber : IDisposable
        {
            private readonly ITimeOfCycle _time;
            private readonly IWorldTimeline _timeline;
            private readonly BoolOutputRecorder _outputRecorder;
            public List<long> ObservedDays { get; } = new();
            public List<double> ObservedProgress01 { get; } = new();
            public List<double> ObservedPresentationTimeSeconds { get; } = new();
            public List<double> ObservedWorldTimeSeconds { get; } = new();
            public List<bool> ObservedOutputValues { get; } = new();

            #region Public Methods

            public CommittedWorldTimeSubscriber(ITimeOfCycle time, IWorldTimeline timeline, BoolOutputRecorder outputRecorder)
            {
                _time = time;
                _timeline = timeline;
                _outputRecorder = outputRecorder;
                _time.OnPeriodChanged += RecordCommittedPeriodState;
                _time.OnTimeCommandCompleted += RecordCommittedCommandState;
            }

            public void Dispose()
            {
                _time.OnPeriodChanged -= RecordCommittedPeriodState;
                _time.OnTimeCommandCompleted -= RecordCommittedCommandState;
            }

            #endregion

            #region Private Methods

            private void RecordCommittedPeriodState(CyclePeriodChange change) => RecordCommittedState();

            private void RecordCommittedCommandState(TimeCommandResult result)
            {
                if (result.Status == TimeCommandStatus.Completed) RecordCommittedState();
            }

            private void RecordCommittedState()
            {
                ObservedDays.Add(_timeline.CurrentDay);
                ObservedProgress01.Add(_time.CurrentState.NormalizedCycleProgress01);
                ObservedPresentationTimeSeconds.Add(_time.CurrentState.PresentationTimeSeconds);
                ObservedWorldTimeSeconds.Add(_timeline.AuthoritativeTimeSeconds);
                ObservedOutputValues.Add(_outputRecorder.LastValue);
            }

            #endregion
        }

        private sealed class IntOutputRecorder : ITimeOfCycleOutputApplicator
        {
            private readonly IntCycleOutputKeySO _keySO;
            public IReadOnlyList<CycleOutputKeySO> RequiredOutputKeys { get; }
            public int LastValue { get; private set; }

            public IntOutputRecorder(IntCycleOutputKeySO keySO)
            {
                _keySO = keySO;
                RequiredOutputKeys = new[] { keySO };
            }

            #region Public Methods

            public void Apply(IReadOnlyCycleOutputState state)
            {
                Assert.That(state.TryGetValue(_keySO, out int value), Is.True);
                LastValue = value;
            }

            #endregion
        }

        private sealed class ClockNotificationSubscriber : IDisposable
        {
            private readonly ITimeOfCycle _time;
            public List<string> ChangedPeriodIds { get; } = new();
            public int CompletedCycleCount { get; private set; }
            public List<TimeCommandStatus> CommandStatuses { get; } = new();

            public ClockNotificationSubscriber(ITimeOfCycle time)
            {
                _time = time;
                _time.OnPeriodChanged += RecordChangedPeriod;
                _time.OnCycleCompleted += RecordCycleCompletion;
                _time.OnTimeCommandCompleted += RecordCommandStatus;
            }

            #region Public Methods

            public void Dispose()
            {
                _time.OnPeriodChanged -= RecordChangedPeriod;
                _time.OnCycleCompleted -= RecordCycleCompletion;
                _time.OnTimeCommandCompleted -= RecordCommandStatus;
            }

            #endregion

            #region Private Methods

            private void RecordChangedPeriod(CyclePeriodChange change) => ChangedPeriodIds.Add(change.CurrentPeriodId.Value);
            private void RecordCycleCompletion() => CompletedCycleCount++;
            private void RecordCommandStatus(TimeCommandResult result) => CommandStatuses.Add(result.Status);

            #endregion
        }
    }
}
