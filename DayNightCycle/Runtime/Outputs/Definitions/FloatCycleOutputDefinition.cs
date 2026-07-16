using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Evaluates a cyclic floating-point curve.
    /// </summary>
    [Serializable]
    public sealed class FloatCycleOutputDefinition : CycleOutputDefinition
    {
        [SerializeField] private AnimationCurve _interpolationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private List<FloatCyclePoint> _points = new();

        public override Type ValueType => typeof(float);

        public FloatCycleOutputDefinition()
        {
        }

        public FloatCycleOutputDefinition(
            FloatCycleOutputKeySO outputKeySO,
            float profileChangeTransitionDurationSeconds,
            AnimationCurve interpolationCurve,
            IEnumerable<FloatCyclePoint> points)
            : base(outputKeySO, profileChangeTransitionDurationSeconds)
        {
            _interpolationCurve = interpolationCurve;
            _points = new List<FloatCyclePoint>(points);
        }

        #region Public Methods

        public override ICycleOutputEvaluator CreateEvaluator(
            double cycleDurationSeconds,
            IReadOnlyList<CyclePeriodDefinition> periods)
        {
            return CycleOutputDefinitionBuilder.CreateContinuous<FloatCyclePoint, float>(
                cycleDurationSeconds,
                _points,
                _interpolationCurve,
                Mathf.LerpUnclamped);
        }

        public override object InterpolateProfileValue(
            object previousValue,
            object destinationValue,
            float progress01)
        {
            return Mathf.LerpUnclamped(
                (float)previousValue,
                (float)destinationValue,
                _interpolationCurve.Evaluate(progress01));
        }

        public override bool TryValidate(
            double cycleDurationSeconds,
            ISet<CyclePeriodId> periodIds,
            out string errorMessage)
        {
            return base.TryValidate(cycleDurationSeconds, periodIds, out errorMessage)
                   && CycleOutputValidation.TryValidateContinuousPoints<FloatCyclePoint, float>(
                       _points,
                       cycleDurationSeconds,
                       _interpolationCurve,
                       out errorMessage)
                   && CycleOutputValueValidation.TryValidate(_points, out errorMessage);
        }

        #endregion
    }
}
