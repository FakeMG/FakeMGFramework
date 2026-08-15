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
        private TimeOfCycleProfileSO _profileSO;

        [SetUp]
        public void SetUp()
        {
            _floatOutputKeySO = ScriptableObject.CreateInstance<FloatCycleOutputKeySO>();
            _profileSO = ScriptableObject.CreateInstance<TimeOfCycleProfileSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_floatOutputKeySO);
            Object.DestroyImmediate(_profileSO);
        }

        [Test]
        public void ResolveStartTimeSeconds_WhenDurationChanges_ScalesPeriodStart()
        {
            CyclePeriodDefinition period = new("dawn", DAWN_PROGRESS_01);

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
        public void CreateEvaluator_WhenDurationChanges_PreservesNormalizedCurveShape()
        {
            FloatCycleOutputDefinition definition = CreateFloatDefinition(
                new FloatCyclePoint(0d, 0f),
                new FloatCyclePoint(0.5d, 1f));
            ICycleOutputEvaluator evaluator = definition.CreateEvaluator(
                SHORTENED_DURATION_SECONDS,
                new List<ResolvedCyclePeriod>());

            float evaluatedValue = (float)evaluator.Evaluate(SHORTENED_DURATION_SECONDS * 0.25d);

            Assert.That(evaluatedValue, Is.EqualTo(0.5f).Within(0.000001f));
        }

        [Test]
        public void TryResolve_WhenDurationChanges_ResolvesDefaultAndPeriodsAtRuntimeBoundary()
        {
            _profileSO.ConfigureForEditor(
                SHORTENED_DURATION_SECONDS,
                1d / 3d,
                60d,
                true,
                1f,
                AnimationCurve.Linear(0f, 0f, 1f, 1f),
                new[] { new CyclePeriodDefinition("dawn", DAWN_PROGRESS_01) },
                new List<CycleOutputDefinition>());

            bool isResolved = TimeOfCycleConfigurationResolver.TryResolve(
                _profileSO,
                null,
                null,
                out ResolvedTimeOfCycleConfiguration configuration,
                out string errorMessage);

            Assert.That(isResolved, Is.True, errorMessage);
            Assert.That(configuration.DefaultStartingTimeSeconds, Is.EqualTo(14400d).Within(0.000001d));
            Assert.That(configuration.Periods[0].StartTimeSeconds, Is.EqualTo(9000d).Within(0.000001d));
        }

        private FloatCycleOutputDefinition CreateFloatDefinition(params FloatCyclePoint[] points)
        {
            return new FloatCycleOutputDefinition(
                _floatOutputKeySO,
                0f,
                AnimationCurve.Linear(0f, 0f, 1f, 1f),
                points);
        }
    }
}
