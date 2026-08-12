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
        [Tooltip(
            "Sets the value at one exact time in the cycle. " +
            "Example: 20:00 = false means the output becomes false at exactly 20:00. " +
            "It stays false until a later period value or timeline point changes the output. " +
            "If both are set to the same time, this timeline point wins.")]
        [SerializeField] private List<BoolCyclePoint> _timelinePoints = new();

        [Tooltip(
            "Sets the value from the moment this named period begins. " +
            "Example: Night = true means the output becomes true when Night begins. " +
            "It stays true until a later period value or timeline point changes the output. " +
            "If both are set to the same time, the timeline point wins.")]
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

        internal override ICycleOutputEvaluator CreateEvaluator(double cycleDurationSeconds, IReadOnlyList<ResolvedCyclePeriod> periods)
        {
            return CycleOutputDefinitionBuilder.CreateDiscrete<BoolCyclePoint, BoolPeriodValue, bool>(
                cycleDurationSeconds,
                _timelinePoints,
                _periodValues,
                periods);
        }

        public override object InterpolateProfileValue(object previousValue, object destinationValue, float progress01)
        {
            return progress01 >= 1f ? destinationValue : previousValue;
        }

        public override bool TryValidate(double cycleDurationSeconds, ISet<CyclePeriodId> periodIds, out string errorMessage)
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
