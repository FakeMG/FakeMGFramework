namespace FakeMG.TimeCycle
{
    public interface ITimeControlStrategy
    {
        bool IsValid { get; }
        bool IsAutomaticAdvancementActive { get; }
        double EffectiveRateCycleSecondsPerRealSecond { get; }
        bool TryValidate(out string errorMessage);
        bool TryApply(ITimeControlContext context, float deltaTimeSeconds);
    }
}
