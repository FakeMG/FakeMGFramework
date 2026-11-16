namespace FakeMG.TimeCycle
{
    public interface ITimeControlContext
    {
        bool TryAdvanceAuthoritativeTime(double signedDeltaSeconds);
        bool TrySetAuthoritativeTime(double authoritativeTimeSeconds);
    }
}
