using System;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Stores one floating-point value at a normalized cycle position.
    /// </summary>
    [Serializable]
    public sealed class FloatCyclePoint : IContinuousCyclePoint<float>
    {
        [SerializeField, CycleProgress] private double _pointProgress01;
        [SerializeField] private float _value;

        public double PointProgress01 => _pointProgress01;
        public float Value => _value;

        public FloatCyclePoint(double pointProgress01, float value)
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
