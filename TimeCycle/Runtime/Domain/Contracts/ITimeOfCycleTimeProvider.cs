namespace FakeMG.TimeCycle
{
    public interface ITimeOfCycleTimeProvider
    {
        bool IsValid { get; }
        bool TryGetCurrentTimeSeconds(out double currentTimeSeconds);
    }
}
