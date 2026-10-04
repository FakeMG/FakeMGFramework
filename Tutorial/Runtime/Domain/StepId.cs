using System;

namespace FakeMG.Tutorial
{
    public readonly struct StepId : IEquatable<StepId>
    {
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);

        public StepId(string value)
        {
            Value = value;
        }

        public bool Equals(StepId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is StepId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : Value.GetHashCode();
        public override string ToString() => Value ?? string.Empty;
    }
}
