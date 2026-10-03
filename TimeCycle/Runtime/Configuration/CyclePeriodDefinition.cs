using System;
using UnityEngine;

namespace FakeMG.TimeCycle
{
    /// <summary>
    /// Defines the identifier and normalized inclusive start position of one named cycle period.
    /// </summary>
    [Serializable]
    public sealed class CyclePeriodDefinition
    {
        [Tooltip("Shared period identity, also referenced by outputs and UI styles.")]
        [SerializeField, Sirenix.OdinInspector.Required] private CyclePeriodSO _periodSO;
        [SerializeField, CycleProgress] private double _startProgress01;

        public CyclePeriodId PeriodId => _periodSO.PeriodId;
        public double StartProgress01 => _startProgress01;

        public CyclePeriodDefinition(CyclePeriodSO periodSO, double startProgress01)
        {
            if (periodSO == null) throw new ArgumentNullException(nameof(periodSO), "Period authoring requires a shared period asset.");
            _periodSO = periodSO;
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
