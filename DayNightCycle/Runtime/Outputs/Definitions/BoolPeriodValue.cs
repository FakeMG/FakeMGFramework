using System;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Maps one named period start to a boolean state.
    /// </summary>
    [Serializable]
    public sealed class BoolPeriodValue : IPeriodCycleValue<bool>
    {
        [SerializeField] private string _periodId;
        [SerializeField] private bool _value;

        public CyclePeriodId PeriodId => new(_periodId);
        public bool Value => _value;

        public BoolPeriodValue(string periodId, bool value)
        {
            _periodId = periodId;
            _value = value;
        }
    }
}
