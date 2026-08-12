using System;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Maps one named period start to an integer or enum-backed state.
    /// </summary>
    [Serializable]
    public sealed class IntPeriodValue : IPeriodCycleValue<int>
    {
        [SerializeField] private string _periodId;
        [SerializeField] private int _value;

        public CyclePeriodId PeriodId => new(_periodId);
        public int Value => _value;

        public IntPeriodValue(string periodId, int value)
        {
            _periodId = periodId;
            _value = value;
        }
    }
}
