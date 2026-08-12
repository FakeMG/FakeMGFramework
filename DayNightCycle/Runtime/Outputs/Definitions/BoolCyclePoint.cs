using System;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Stores one boolean state change at an independent cycle time.
    /// </summary>
    [Serializable]
    public sealed class BoolCyclePoint : IDiscreteCyclePoint<bool>
    {
        [SerializeField, CycleProgress] private double _pointProgress01;
        [SerializeField] private bool _value;

        public double PointProgress01 => _pointProgress01;
        public bool Value => _value;

        public BoolCyclePoint(double pointProgress01, bool value)
        {
            _pointProgress01 = pointProgress01;
            _value = value;
        }

        #region Public Methods

        public double ResolveTimeSeconds(double cycleDurationSeconds)
        {
            return CycleProgressConversion.ResolveTimeSeconds(_pointProgress01, cycleDurationSeconds);
        }

        #endregion
    }
}
