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
        [SerializeField] private double _timeSeconds;
        [SerializeField] private bool _value;

        public double TimeSeconds => _timeSeconds;
        public bool Value => _value;

        public BoolCyclePoint(double timeSeconds, bool value)
        {
            _timeSeconds = timeSeconds;
            _value = value;
        }
    }
}
