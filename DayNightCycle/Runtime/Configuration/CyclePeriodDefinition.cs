using System;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Defines the identifier and inclusive start time of one named cycle period.
    /// </summary>
    [Serializable]
    public sealed class CyclePeriodDefinition
    {
        [SerializeField] private string _periodId;
        [SerializeField] private double _startTimeSeconds;

        public CyclePeriodId PeriodId => new(_periodId);
        public double StartTimeSeconds => _startTimeSeconds;

        public CyclePeriodDefinition(string periodId, double startTimeSeconds)
        {
            _periodId = periodId;
            _startTimeSeconds = startTimeSeconds;
        }
    }
}
