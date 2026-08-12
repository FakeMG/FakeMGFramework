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
        [Tooltip(
            "Sets the value at one exact time in the cycle. " +
            "Example: 20:00 = 2 means the output becomes 2 at exactly 20:00. " +
            "It stays 2 until a later period value or timeline point changes the output. " +
            "If both are set to the same time, this timeline point wins.")]
        [SerializeField] private List<IntCyclePoint> _timelinePoints = new();

        [Tooltip(
            "Sets the value from the moment this named period begins. " +
            "Example: Night = 2 means the output becomes 2 when Night begins. " +
            "It stays 2 until a later period value or timeline point changes the output. " +
            "If both are set to the same time, the timeline point wins.")]
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

        internal override ICycleOutputEvaluator CreateEvaluator(double cycleDurationSeconds, IReadOnlyList<ResolvedCyclePeriod> periods)
        {
            return CycleOutputDefinitionBuilder.CreateDiscrete<IntCyclePoint, IntPeriodValue, int>(
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
                && CycleOutputValidation.TryValidateDiscretePoints<IntCyclePoint, IntPeriodValue, int>(
                    _timelinePoints,
                    _periodValues,
                    cycleDurationSeconds,
                    periodIds,
                    out errorMessage
                );
        }

        #endregion
    }
}
