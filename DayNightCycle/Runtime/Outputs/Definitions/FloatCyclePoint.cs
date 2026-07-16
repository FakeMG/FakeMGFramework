using System;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Stores one floating-point value at a cycle time.
    /// </summary>
    [Serializable]
    public sealed class FloatCyclePoint : IContinuousCyclePoint<float>
    {
        [SerializeField] private double _timeSeconds;
        [SerializeField] private float _value;

        public double TimeSeconds => _timeSeconds;
        public float Value => _value;

        public FloatCyclePoint(double timeSeconds, float value)
        {
            _timeSeconds = timeSeconds;
            _value = value;
        }
    }
}
