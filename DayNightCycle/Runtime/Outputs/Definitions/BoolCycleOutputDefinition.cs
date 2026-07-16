using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Evaluates boolean changes from period conditions and independent points.
    /// </summary>
    [Serializable]
    public sealed class BoolCycleOutputDefinition : CycleOutputDefinition
    {
        [SerializeField] private List<BoolCyclePoint> _timelinePoints = new();
        [SerializeField] private List<BoolPeriodValue> _periodValues = new();

        public override Type ValueType => typeof(bool);

        public BoolCycleOutputDefinition()
        {
        }

        public BoolCycleOutputDefinition(
            BoolCycleOutputKeySO outputKeySO,
            float profileChangeTransitionDurationSeconds,
            IEnumerable<BoolCyclePoint> timelinePoints,
            IEnumerable<BoolPeriodValue> periodValues)
            : base(outputKeySO, profileChangeTransitionDurationSeconds)
        {
            _timelinePoints = new List<BoolCyclePoint>(timelinePoints);
            _periodValues = new List<BoolPeriodValue>(periodValues);
        }

        #region Public Methods

        public override ICycleOutputEvaluator CreateEvaluator(
            double cycleDurationSeconds,
            IReadOnlyList<CyclePeriodDefinition> periods)
        {
            return CycleOutputDefinitionBuilder.CreateDiscrete<BoolCyclePoint, BoolPeriodValue, bool>(
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
                   && CycleOutputValidation.TryValidateDiscretePoints<BoolCyclePoint, BoolPeriodValue, bool>(
                       _timelinePoints,
                       _periodValues,
                       cycleDurationSeconds,
                       periodIds,
                       out errorMessage);
        }

        #endregion
    }
}
