using System;

namespace FakeMG.FakeMGFramework.Audio
{
    public readonly struct AudioCueKey
    {
        public static AudioCueKey Invalid = new(string.Empty, null);

        private readonly string _value;
        private readonly AudioCueSO _audioCue;

        internal AudioCueKey(string value, AudioCueSO audioCue)
        {
            _value = value;
            _audioCue = audioCue;
        }

        public override bool Equals(Object obj)
        {
            return obj is AudioCueKey x && _value == x._value && _audioCue == x._audioCue;
        }

        public override int GetHashCode()
        {
            return (_value?.GetHashCode() ?? 0) ^ (_audioCue?.GetHashCode() ?? 0);
        }

        public static bool operator ==(AudioCueKey x, AudioCueKey y)
        {
            return x._value == y._value && x._audioCue == y._audioCue;
        }

        public static bool operator !=(AudioCueKey x, AudioCueKey y)
        {
            return !(x == y);
        }
    }
}