namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Supplies authoritative time while an external direct-time request is active.
    /// </summary>
    public interface ITimeOfCycleTimeProvider
    {
        bool IsValid { get; }
        bool TryGetCurrentTimeSeconds(out double currentTimeSeconds);
    }
}
