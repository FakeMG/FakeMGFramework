using System;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Stores one integer state change at an independent cycle time.
    /// </summary>
    [Serializable]
    public sealed class IntCyclePoint : IDiscreteCyclePoint<int>
    {
        [SerializeField, CycleProgress] private double _pointProgress01;
        [SerializeField] private int _value;

        public double PointProgress01 => _pointProgress01;
        public int Value => _value;

        public IntCyclePoint(double pointProgress01, int value)
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
