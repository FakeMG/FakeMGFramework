using System;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Identifies one named period within a repeating cycle.
    /// </summary>
    [Serializable]
    public readonly struct CyclePeriodId : IEquatable<CyclePeriodId>
    {
        [SerializeField] private readonly string _value;

        public string Value => _value;
        public bool IsValid => !string.IsNullOrWhiteSpace(_value);

        public CyclePeriodId(string value)
        {
            _value = value;
        }

        #region Public Methods

        public bool Equals(CyclePeriodId other)
        {
            return string.Equals(_value, other._value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is CyclePeriodId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value == null ? 0 : StringComparer.Ordinal.GetHashCode(_value);
        }

        public override string ToString()
        {
            return _value ?? string.Empty;
        }

        #endregion
    }
}
