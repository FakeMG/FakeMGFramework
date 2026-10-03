using System;
using UnityEngine;

namespace FakeMG.TimeCycle
{
    /// <summary>
    /// Maps one named period start to an integer or enum-backed state.
    /// </summary>
    [Serializable]
    public sealed class IntPeriodValue : IPeriodCycleValue<int>
    {
        [Tooltip("Use the same shared period asset as the time profile and UI styles.")]
        [SerializeField] private CyclePeriodSO _periodSO;
        [SerializeField] private int _value;

        public CyclePeriodId PeriodId => _periodSO.PeriodId;
        public int Value => _value;

        public IntPeriodValue(CyclePeriodSO periodSO, int value)
        {
            _periodSO = periodSO;
            _value = value;
        }
    }
}
