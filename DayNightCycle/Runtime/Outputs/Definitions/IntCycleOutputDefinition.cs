using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Evaluates integer changes from period conditions and independent points.
    /// </summary>
    [Serializable]
    public sealed class IntCycleOutputDefinition : CycleOutputDefinition
    {
        [SerializeField] private List<IntCyclePoint> _timelinePoints = new();
        [SerializeField] private List<IntPeriodValue> _periodValues = new();

        public override Type ValueType => typeof(int);

        public IntCycleOutputDefinition()
        {
        }

        public IntCycleOutputDefinition(
            IntCycleOutputKeySO outputKeySO,
            float profileChangeTransitionDurationSeconds,
            IEnumerable<IntCyclePoint> timelinePoints,
            IEnumerable<IntPeriodValue> periodValues)
            : base(outputKeySO, profileChangeTransitionDurationSeconds)
        {
            _timelinePoints = new List<IntCyclePoint>(timelinePoints);
            _periodValues = new List<IntPeriodValue>(periodValues);
        }

        #region Public Methods

        public override ICycleOutputEvaluator CreateEvaluator(
            double cycleDurationSeconds,
            IReadOnlyList<CyclePeriodDefinition> periods)
        {
            return CycleOutputDefinitionBuilder.CreateDiscrete<IntCyclePoint, IntPeriodValue, int>(
                _timelinePoints,
                _periodValues,
                periods);
        }

        public override object InterpolateProfileValue(
            object previousValue,
            object destinationValue,
            float progress01)
        {
            return progress01 >= 1f ? destinationValue : previousValue;
        }

        public override bool TryValidate(
            double cycleDurationSeconds,
            ISet<CyclePeriodId> periodIds,
            out string errorMessage)
        {
            return base.TryValidate(cycleDurationSeconds, periodIds, out errorMessage)
                   && CycleOutputValidation.TryValidateDiscretePoints<IntCyclePoint, IntPeriodValue, int>(
                       _timelinePoints,
                       _periodValues,
                       cycleDurationSeconds,
                       periodIds,
                       out errorMessage);
        }

        #endregion
    }
}
