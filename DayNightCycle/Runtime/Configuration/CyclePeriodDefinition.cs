using System;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Defines the identifier and normalized inclusive start position of one named cycle period.
    /// </summary>
    [Serializable]
    public sealed class CyclePeriodDefinition
    {
        [SerializeField] private string _periodId;
        [SerializeField, CycleProgress] private double _startProgress01;

        public CyclePeriodId PeriodId => new(_periodId);
        public double StartProgress01 => _startProgress01;

        public CyclePeriodDefinition(string periodId, double startProgress01)
        {
            _periodId = periodId;
            _startProgress01 = startProgress01;
        }

        #region Public Methods

        public double ResolveStartTimeSeconds(double cycleDurationSeconds)
        {
            return CycleProgressConversion.ResolveTimeSeconds(_startProgress01, cycleDurationSeconds);
        }

        #endregion
    }
}
