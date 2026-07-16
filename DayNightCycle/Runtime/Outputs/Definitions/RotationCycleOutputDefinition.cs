using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Evaluates authored Euler points with quaternion interpolation.
    /// </summary>
    [Serializable]
    public sealed class RotationCycleOutputDefinition : CycleOutputDefinition
    {
        [SerializeField] private AnimationCurve _interpolationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private List<RotationCyclePoint> _points = new();

        public override Type ValueType => typeof(Quaternion);

        public RotationCycleOutputDefinition()
        {
        }

        public RotationCycleOutputDefinition(
            RotationCycleOutputKeySO outputKeySO,
            float profileChangeTransitionDurationSeconds,
            AnimationCurve interpolationCurve,
            IEnumerable<RotationCyclePoint> points)
            : base(outputKeySO, profileChangeTransitionDurationSeconds)
        {
            _interpolationCurve = interpolationCurve;
            _points = new List<RotationCyclePoint>(points);
        }

        #region Public Methods

        public override ICycleOutputEvaluator CreateEvaluator(
            double cycleDurationSeconds,
            IReadOnlyList<CyclePeriodDefinition> periods)
        {
            return CycleOutputDefinitionBuilder.CreateContinuous<RotationCyclePoint, Quaternion>(
                cycleDurationSeconds,
                _points,
                _interpolationCurve,
                Quaternion.SlerpUnclamped);
        }

        public override object InterpolateProfileValue(
            object previousValue,
            object destinationValue,
            float progress01)
        {
            return Quaternion.SlerpUnclamped(
                (Quaternion)previousValue,
                (Quaternion)destinationValue,
                _interpolationCurve.Evaluate(progress01));
        }

        public override bool TryValidate(
            double cycleDurationSeconds,
            ISet<CyclePeriodId> periodIds,
            out string errorMessage)
        {
            return base.TryValidate(cycleDurationSeconds, periodIds, out errorMessage)
                   && CycleOutputValidation.TryValidateContinuousPoints<RotationCyclePoint, Quaternion>(
                       _points,
                       cycleDurationSeconds,
                       _interpolationCurve,
                       out errorMessage)
                   && CycleOutputValueValidation.TryValidate(_points, out errorMessage);
        }

        #endregion
    }
}
