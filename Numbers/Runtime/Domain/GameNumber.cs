using System;
using System.Globalization;
using System.Numerics;
using FakeMG.Framework;
using FakeMG.Framework.ExtensionMethods;

namespace FakeMG.Numbers
{
    /// <summary>
    /// The single number type inventory, shop, and HUD code depends on instead of BigInteger directly.
    /// Defaults to a long backing for currencies that fit within ~9.2 quintillion. A game that needs
    /// arbitrary-magnitude values can define FAKEMG_BIG_NUMBERS (Player Settings > Scripting Define Symbols)
    /// to switch this file to a BigInteger backing instead - hot-path arithmetic then runs on plain long
    /// by default, with no other file needing to change. Rarely-called parsing/formatting still bridges
    /// through BigNumberParser (via BigInteger) in both backings to avoid maintaining two parser implementations.
    /// </summary>
    public readonly struct GameNumber : IEquatable<GameNumber>, IComparable<GameNumber>
    {
#if FAKEMG_BIG_NUMBERS
        private readonly BigInteger _value;

        private GameNumber(BigInteger value)
        {
            _value = value;
        }

        public static readonly GameNumber Zero = new(BigInteger.Zero);
        public static readonly GameNumber One = new(BigInteger.One);

        public static implicit operator GameNumber(int value) => new(value);
        public static implicit operator GameNumber(long value) => new(value);
        public static explicit operator int(GameNumber number) => (int)number._value;

        public static GameNumber FromDouble(double value) => new(new BigInteger(value));
        public double ToDouble() => (double)_value;

        public static GameNumber operator +(GameNumber a, GameNumber b) => new(a._value + b._value);
        public static GameNumber operator -(GameNumber a, GameNumber b) => new(a._value - b._value);
        public static GameNumber operator *(GameNumber a, GameNumber b) => new(a._value * b._value);
        public static bool operator ==(GameNumber a, GameNumber b) => a._value == b._value;
        public static bool operator !=(GameNumber a, GameNumber b) => a._value != b._value;
        public static bool operator <(GameNumber a, GameNumber b) => a._value < b._value;
        public static bool operator <=(GameNumber a, GameNumber b) => a._value <= b._value;
        public static bool operator >(GameNumber a, GameNumber b) => a._value > b._value;
        public static bool operator >=(GameNumber a, GameNumber b) => a._value >= b._value;
        public static GameNumber Max(GameNumber a, GameNumber b) => new(BigInteger.Max(a._value, b._value));

        public static GameNumber Pow(GameNumber baseValue, int exponent) => new(BigInteger.Pow(baseValue._value, exponent));

        public override string ToString() => _value.ToString(CultureInfo.InvariantCulture);
        public string ToShorthand(int decimalPlaces = 1) => BigNumberParser.ToShorthand(_value, decimalPlaces);
        public string SeparateNumberWithComma() => _value.SeparateNumberWithComma();

        public static bool TryParse(string text, out GameNumber value)
        {
            if (BigNumberParser.TryParse(text, out BigInteger parsedValue))
            {
                value = new GameNumber(parsedValue);
                return true;
            }

            value = Zero;
            return false;
        }
#else
        private readonly long _value;

        private GameNumber(long value)
        {
            _value = value;
        }

        public static readonly GameNumber Zero = new(0L);
        public static readonly GameNumber One = new(1L);

        public static implicit operator GameNumber(int value) => new((long)value);
        public static implicit operator GameNumber(long value) => new(value);
        public static explicit operator int(GameNumber number) => (int)number._value;

        public static GameNumber FromDouble(double value) => new((long)value);
        public double ToDouble() => _value;

        public static GameNumber operator +(GameNumber a, GameNumber b) => new(a._value + b._value);
        public static GameNumber operator -(GameNumber a, GameNumber b) => new(a._value - b._value);
        public static GameNumber operator *(GameNumber a, GameNumber b) => new(a._value * b._value);
        public static bool operator ==(GameNumber a, GameNumber b) => a._value == b._value;
        public static bool operator !=(GameNumber a, GameNumber b) => a._value != b._value;
        public static bool operator <(GameNumber a, GameNumber b) => a._value < b._value;
        public static bool operator <=(GameNumber a, GameNumber b) => a._value <= b._value;
        public static bool operator >(GameNumber a, GameNumber b) => a._value > b._value;
        public static bool operator >=(GameNumber a, GameNumber b) => a._value >= b._value;
        public static GameNumber Max(GameNumber a, GameNumber b) => a._value >= b._value ? a : b;

        // Integer-exponent power via repeated long multiplication - the long backing has no
        // BigInteger.Pow equivalent, and callers of this backing accept the long overflow ceiling.
        public static GameNumber Pow(GameNumber baseValue, int exponent)
        {
            GameNumber result = One;
            for (int i = 0; i < exponent; i++)
            {
                result *= baseValue;
            }

            return result;
        }

        public override string ToString() => _value.ToString(CultureInfo.InvariantCulture);
        public string ToShorthand(int decimalPlaces = 1) => BigNumberParser.ToShorthand(_value, decimalPlaces);
        public string SeparateNumberWithComma() => ((BigInteger)_value).SeparateNumberWithComma();

        public static bool TryParse(string text, out GameNumber value)
        {
            value = Zero;

            if (!BigNumberParser.TryParse(text, out BigInteger parsedValue))
            {
                return false;
            }

            if (parsedValue > long.MaxValue || parsedValue < long.MinValue)
            {
                Echo.Warning($"Parsed amount '{text}' exceeds the long-backed GameNumber range and was clamped.");
                value = parsedValue > long.MaxValue ? new GameNumber(long.MaxValue) : new GameNumber(long.MinValue);
                return true;
            }

            value = new GameNumber((long)parsedValue);
            return true;
        }
#endif

        public static GameNumber ParseOrDefault(string text, GameNumber fallback)
        {
            return TryParse(text, out GameNumber value) ? value : fallback;
        }

        public bool Equals(GameNumber other) => this == other;
        public override bool Equals(object obj) => obj is GameNumber other && Equals(other);
        public override int GetHashCode() => ToString().GetHashCode();
        public int CompareTo(GameNumber other) => this < other ? -1 : this > other ? 1 : 0;
    }
}
