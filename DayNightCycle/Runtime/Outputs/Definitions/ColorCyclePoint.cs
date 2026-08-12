using System;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Stores one color value at a normalized cycle position.
    /// </summary>
    [Serializable]
    public sealed class ColorCyclePoint : IContinuousCyclePoint<Color>
    {
        [SerializeField, CycleProgress] private double _pointProgress01;
        [SerializeField] private Color _value = Color.white;

        public double PointProgress01 => _pointProgress01;
        public Color Value => _value;

        public ColorCyclePoint(double pointProgress01, Color value)
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
