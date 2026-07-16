namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Exposes evaluated output values without exposing runtime storage.
    /// </summary>
    public interface IReadOnlyCycleOutputState
    {
        bool TryGetValue(BoolCycleOutputKeySO outputKeySO, out bool value);
        bool TryGetValue(IntCycleOutputKeySO outputKeySO, out int value);
        bool TryGetValue(FloatCycleOutputKeySO outputKeySO, out float value);
        bool TryGetValue(ColorCycleOutputKeySO outputKeySO, out UnityEngine.Color value);
        bool TryGetValue(RotationCycleOutputKeySO outputKeySO, out UnityEngine.Quaternion value);
    }
}
