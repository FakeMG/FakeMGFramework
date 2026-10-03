using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace FakeMG.TimeCycle.Tests.EditMode
{
    public sealed class CycleProgressResolutionTests
    {
        private const double ORIGINAL_DURATION_SECONDS = 86400d;
        private const double SHORTENED_DURATION_SECONDS = 43200d;
        private const double DAWN_PROGRESS_01 = 5d / 24d;

        private FloatCycleOutputKeySO _floatOutputKeySO;
        private readonly List<CyclePeriodSO> _periodsSO = new();
        private TimeOfCycleProfileSO _profileSO;

        #region Public Methods

        [SetUp]
        public void SetUp()
        {
            _floatOutputKeySO = ScriptableObject.CreateInstance<FloatCycleOutputKeySO>();
            _profileSO = ScriptableObject.CreateInstance<TimeOfCycleProfileSO>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (CyclePeriodSO periodSO in _periodsSO) UnityEngine.Object.DestroyImmediate(periodSO);
            _periodsSO.Clear();
            Object.DestroyImmediate(_floatOutputKeySO);
            Object.DestroyImmediate(_profileSO);
        }

        [Test]
        public void ResolveStartTimeSeconds_WhenDurationChanges_ScalesPeriodStart()
        {
            CyclePeriodDefinition period = new(CreatePeriodSO("dawn"), DAWN_PROGRESS_01);

            double resolvedTimeSeconds = period.ResolveStartTimeSeconds(SHORTENED_DURATION_SECONDS);

            Assert.That(resolvedTimeSeconds, Is.EqualTo(9000d).Within(0.000001d));
        }

        [Test]
        public void ResolveTimeSeconds_WhenDurationChanges_ScalesFloatPoint()
        {
            FloatCyclePoint point = new(DAWN_PROGRESS_01, 1f);

            double resolvedTimeSeconds = point.ResolveTimeSeconds(SHORTENED_DURATION_SECONDS);

            Assert.That(resolvedTimeSeconds, Is.EqualTo(9000d).Within(0.000001d));
        }

        [Test]
        public void ResolveTimeSeconds_AtCycleStart_ReturnsZeroForEveryPointType()
        {
            Assert.That(new FloatCyclePoint(0d, 1f).ResolveTimeSeconds(ORIGINAL_DURATION_SECONDS), Is.Zero);
            Assert.That(new ColorCyclePoint(0d, Color.white).ResolveTimeSeconds(ORIGINAL_DURATION_SECONDS), Is.Zero);
            Assert.That(new RotationCyclePoint(0d, Vector3.zero).ResolveTimeSeconds(ORIGINAL_DURATION_SECONDS), Is.Zero);
            Assert.That(new BoolCyclePoint(0d, true).ResolveTimeSeconds(ORIGINAL_DURATION_SECONDS), Is.Zero);
            Assert.That(new IntCyclePoint(0d, 1).ResolveTimeSeconds(ORIGINAL_DURATION_SECONDS), Is.Zero);
        }

        [Test]
        public void TryValidate_WhenPointProgressEqualsOne_ReturnsFalse()
        {
            FloatCycleOutputDefinition definition = CreateFloatDefinition(new FloatCyclePoint(1d, 1f));

            bool isValid = definition.TryValidate(
                ORIGINAL_DURATION_SECONDS,
                new HashSet<CyclePeriodId>(),
                out string errorMessage);

            Assert.That(isValid, Is.False);
            Assert.That(errorMessage, Does.Contain("outside [0, 1)"));
        }

        [Test]
        public void TryValidate_WhenPointProgressIsDuplicated_ReturnsFalse()
        {
            FloatCycleOutputDefinition definition = CreateFloatDefinition(
                new FloatCyclePoint(0.5d, 1f),
                new FloatCyclePoint(0.5d, 2f));

            bool isValid = definition.TryValidate(
                ORIGINAL_DURATION_SECONDS,
                new HashSet<CyclePeriodId>(),
                out string errorMessage);

            Assert.That(isValid, Is.False);
            Assert.That(errorMessage, Does.Contain("duplicated"));
        }

        [Test]
        public void ServiceOutputs_WhenDurationChanges_PreserveNormalizedCurveShape()
        {
            FloatCycleOutputDefinition definition = CreateFloatDefinition(
                new FloatCyclePoint(0d, 0f),
                new FloatCyclePoint(0.5d, 1f));
            _profileSO.ConfigureForEditor(SHORTENED_DURATION_SECONDS, 0.25d, 0d, false, 0f,
                AnimationCurve.Linear(0f, 0f, 1f, 1f),
                new[] { new CyclePeriodDefinition(CreatePeriodSO("day"), 0d) }, new[] { definition });
            var outputRecorder = new FloatOutputRecorder(_floatOutputKeySO);
            using var time = new TimeOfCycleService(_profileSO, new[] { outputRecorder });

            time.Initialize();

            Assert.That(time.IsInitialized, Is.True);
            Assert.That(outputRecorder.LastValue, Is.EqualTo(0.5f).Within(0.000001f));
        }

        [Test]
        public void ServiceInitialization_WhenDurationChanges_ResolvesStartingTimeAndPeriodLayout()
        {
            _profileSO.ConfigureForEditor(
                SHORTENED_DURATION_SECONDS,
                1d / 3d,
                60d,
                true,
                1f,
                AnimationCurve.Linear(0f, 0f, 1f, 1f),
                new[] { new CyclePeriodDefinition(CreatePeriodSO("dawn"), DAWN_PROGRESS_01) },
                new List<CycleOutputDefinition>());

            using var time = new TimeOfCycleService(_profileSO, System.Array.Empty<ITimeOfCycleOutputApplicator>());

            time.Initialize();

            Assert.That(time.IsInitialized, Is.True);
            Assert.That(time.CurrentState.CycleTimeSeconds, Is.EqualTo(14400d).Within(0.000001d));
            Assert.That(time.CurrentState.CurrentPeriodId, Is.EqualTo(new CyclePeriodId("dawn")));
            Assert.That(time.ActiveLayout.Periods[0].StartProgress01, Is.EqualTo(DAWN_PROGRESS_01));
            Assert.That(time.ActiveLayout.CycleDurationSeconds, Is.EqualTo(SHORTENED_DURATION_SECONDS));
        }

        [Test]
        public void ProgressImmediatelyBelowOneRemainsValidAndResolvesNearCycleEnd()
        {
            const double PROGRESS_01 = 1d - 1e-10;
            var definition = CreateFloatDefinition(new FloatCyclePoint(PROGRESS_01, 1f));
            var period = new CyclePeriodDefinition(CreatePeriodSO("last"), PROGRESS_01);

            Assert.That(definition.TryValidate(ORIGINAL_DURATION_SECONDS, new HashSet<CyclePeriodId>(), out string errorMessage),
                Is.True, errorMessage);
            Assert.That(period.ResolveStartTimeSeconds(ORIGINAL_DURATION_SECONDS), Is.LessThan(ORIGINAL_DURATION_SECONDS));
            Assert.That(period.ResolveStartTimeSeconds(ORIGINAL_DURATION_SECONDS),
                Is.EqualTo(ORIGINAL_DURATION_SECONDS * PROGRESS_01).Within(1e-9));
            Assert.That(new FloatCyclePoint(PROGRESS_01, 1f).ResolveTimeSeconds(ORIGINAL_DURATION_SECONDS),
                Is.EqualTo(ORIGINAL_DURATION_SECONDS * PROGRESS_01).Within(1e-9));
        }

        [Test]
        public void AuthoringPeriodWithoutSharedAssetThrowsClearArgumentError()
        {
            var exception = Assert.Throws<System.ArgumentNullException>(() => new CyclePeriodDefinition(null, 0d));
            Assert.That(exception.ParamName, Is.EqualTo("periodSO"));
        }

        #endregion

        #region Private Methods

        private CyclePeriodSO CreatePeriodSO(string periodId)
        {
            var periodSO = ScriptableObject.CreateInstance<CyclePeriodSO>();
            periodSO.ConfigureForEditor(periodId, periodId);
            _periodsSO.Add(periodSO);
            return periodSO;
        }
        private FloatCycleOutputDefinition CreateFloatDefinition(params FloatCyclePoint[] points)
        {
            return new FloatCycleOutputDefinition(
                _floatOutputKeySO,
                0f,
                AnimationCurve.Linear(0f, 0f, 1f, 1f),
                points);
        }

        #endregion

        private sealed class FloatOutputRecorder : ITimeOfCycleOutputApplicator
        {
            private readonly FloatCycleOutputKeySO _keySO;
            public IReadOnlyList<CycleOutputKeySO> RequiredOutputKeys { get; }
            public float LastValue { get; private set; }

            public FloatOutputRecorder(FloatCycleOutputKeySO keySO)
            {
                _keySO = keySO;
                RequiredOutputKeys = new[] { keySO };
            }

            #region Public Methods

            public void Apply(IReadOnlyCycleOutputState state)
            {
                Assert.That(state.TryGetValue(_keySO, out float value), Is.True);
                LastValue = value;
            }

            #endregion
        }
    }
}
