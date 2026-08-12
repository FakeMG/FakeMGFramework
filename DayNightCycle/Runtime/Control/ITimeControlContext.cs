namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Exposes the bounded clock operations available to extensible persistent-control strategies.
    /// </summary>
    public interface ITimeControlContext
    {
        bool TryAdvanceAuthoritativeTime(double signedDeltaSeconds);
        bool TrySetAuthoritativeTime(double authoritativeTimeSeconds);
    }
}
