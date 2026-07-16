using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Evaluates a cyclic color curve.
    /// </summary>
    [Serializable]
    public sealed class ColorCycleOutputDefinition : CycleOutputDefinition
    {
        [SerializeField] private AnimationCurve _interpolationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private List<ColorCyclePoint> _points = new();

        public override Type ValueType => typeof(Color);

        public ColorCycleOutputDefinition()
        {
        }

        public ColorCycleOutputDefinition(
            ColorCycleOutputKeySO outputKeySO,
            float profileChangeTransitionDurationSeconds,
            AnimationCurve interpolationCurve,
            IEnumerable<ColorCyclePoint> points)
            : base(outputKeySO, profileChangeTransitionDurationSeconds)
        {
            _interpolationCurve = interpolationCurve;
            _points = new List<ColorCyclePoint>(points);
        }

        #region Public Methods

        public override ICycleOutputEvaluator CreateEvaluator(
            double cycleDurationSeconds,
            IReadOnlyList<CyclePeriodDefinition> periods)
        {
            return CycleOutputDefinitionBuilder.CreateContinuous<ColorCyclePoint, Color>(
                cycleDurationSeconds,
                _points,
                _interpolationCurve,
                Color.LerpUnclamped);
        }

        public override object InterpolateProfileValue(
            object previousValue,
            object destinationValue,
            float progress01)
        {
            return Color.LerpUnclamped(
                (Color)previousValue,
                (Color)destinationValue,
                _interpolationCurve.Evaluate(progress01));
        }

        public override bool TryValidate(
            double cycleDurationSeconds,
            ISet<CyclePeriodId> periodIds,
            out string errorMessage)
        {
            return base.TryValidate(cycleDurationSeconds, periodIds, out errorMessage)
                   && CycleOutputValidation.TryValidateContinuousPoints<ColorCyclePoint, Color>(
                       _points,
                       cycleDurationSeconds,
                       _interpolationCurve,
                       out errorMessage)
                   && CycleOutputValueValidation.TryValidate(_points, out errorMessage);
        }

        #endregion
    }
}
