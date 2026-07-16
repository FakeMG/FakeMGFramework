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
        [SerializeField] private double _timeSeconds;
        [SerializeField] private int _value;

        public double TimeSeconds => _timeSeconds;
        public int Value => _value;

        public IntCyclePoint(double timeSeconds, int value)
        {
            _timeSeconds = timeSeconds;
            _value = value;
        }
    }
}
