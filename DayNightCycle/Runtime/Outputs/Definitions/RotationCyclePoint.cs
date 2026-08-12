using System;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Stores one Euler-angle rotation at a normalized cycle position for readable authoring.
    /// </summary>
    [Serializable]
    public sealed class RotationCyclePoint : IContinuousCyclePoint<Quaternion>
    {
        [SerializeField, CycleProgress] private double _pointProgress01;
        [SerializeField] private Vector3 _eulerDegrees;

        public double PointProgress01 => _pointProgress01;
        public Quaternion Value => Quaternion.Euler(_eulerDegrees);
        public Quaternion Rotation => Value;

        public RotationCyclePoint(double pointProgress01, Vector3 eulerDegrees)
        {
            _pointProgress01 = pointProgress01;
            _eulerDegrees = eulerDegrees;
        }

        #region Public Methods

        public double ResolveTimeSeconds(double cycleDurationSeconds)
        {
            return CycleProgressConversion.ResolveTimeSeconds(_pointProgress01, cycleDurationSeconds);
        }

        #endregion
    }
}
