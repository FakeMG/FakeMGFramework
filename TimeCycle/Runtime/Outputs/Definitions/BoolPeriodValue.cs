using System;
using UnityEngine;

namespace FakeMG.TimeCycle
{
    /// <summary>
    /// Maps one named period start to a boolean state.
    /// </summary>
    [Serializable]
    public sealed class BoolPeriodValue : IPeriodCycleValue<bool>
    {
        [Tooltip("Use the same shared period asset as the time profile and UI styles.")]
        [SerializeField] private CyclePeriodSO _periodSO;
        [SerializeField] private bool _value;

        public CyclePeriodId PeriodId => _periodSO.PeriodId;
        public bool Value => _value;

        public BoolPeriodValue(CyclePeriodSO periodSO, bool value)
        {
            _periodSO = periodSO;
            _value = value;
        }
    }
}
