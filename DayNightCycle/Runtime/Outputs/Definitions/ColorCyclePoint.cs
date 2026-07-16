using System;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Stores one color value at a cycle time.
    /// </summary>
    [Serializable]
    public sealed class ColorCyclePoint : IContinuousCyclePoint<Color>
    {
        [SerializeField] private double _timeSeconds;
        [SerializeField] private Color _value = Color.white;

        public double TimeSeconds => _timeSeconds;
        public Color Value => _value;

        public ColorCyclePoint(double timeSeconds, Color value)
        {
            _timeSeconds = timeSeconds;
            _value = value;
        }
    }
}
