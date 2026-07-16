using System;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Stores one Euler-angle rotation at a cycle time for readable authoring.
    /// </summary>
    [Serializable]
    public sealed class RotationCyclePoint : IContinuousCyclePoint<Quaternion>
    {
        [SerializeField] private double _timeSeconds;
        [SerializeField] private Vector3 _eulerDegrees;

        public double TimeSeconds => _timeSeconds;
        public Quaternion Value => Quaternion.Euler(_eulerDegrees);
        public Quaternion Rotation => Value;

        public RotationCyclePoint(double timeSeconds, Vector3 eulerDegrees)
        {
            _timeSeconds = timeSeconds;
            _eulerDegrees = eulerDegrees;
        }
    }
}
