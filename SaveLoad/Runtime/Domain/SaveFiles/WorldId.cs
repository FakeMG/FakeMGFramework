using System;

namespace FakeMG.SaveLoad
{
    public readonly struct WorldId : IEquatable<WorldId>
    {
        public string Value { get; }

        private WorldId(string value)
        {
            Value = value;
        }

        public static WorldId CreateNew()
        {
            return new WorldId(SaveFileCatalog.WORLD_ID_PREFIX + Guid.NewGuid().ToString("N"));
        }

        public static bool TryParse(string value, out WorldId worldId)
        {
            if (!string.IsNullOrWhiteSpace(value) &&
                value.StartsWith(SaveFileCatalog.WORLD_ID_PREFIX, StringComparison.Ordinal) &&
                value.Length == SaveFileCatalog.WORLD_ID_PREFIX.Length + 32 &&
                Guid.TryParseExact(value[SaveFileCatalog.WORLD_ID_PREFIX.Length..], "N", out _))
            {
                worldId = new WorldId(value);
                return true;
            }

            worldId = default;
            return false;
        }

        public static WorldId Parse(string value)
        {
            return TryParse(value, out WorldId worldId)
                ? worldId
                : throw new ArgumentException($"Invalid world ID '{value}'.", nameof(value));
        }

        public bool Equals(WorldId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object other)
        {
            return other is WorldId worldId && Equals(worldId);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
