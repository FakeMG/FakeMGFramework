using System;

namespace FakeMG.Tutorial
{
    public readonly struct TutorialId : IEquatable<TutorialId>
    {
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);

        public TutorialId(string value)
        {
            Value = value;
        }

        public bool Equals(TutorialId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is TutorialId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : Value.GetHashCode();
        public override string ToString() => Value ?? string.Empty;
    }
}
