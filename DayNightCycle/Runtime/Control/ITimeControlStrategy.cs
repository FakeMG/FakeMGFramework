namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Extends persistent time control through independently validated clock instructions.
    /// </summary>
    public interface ITimeControlStrategy
    {
        bool IsValid { get; }
        bool IsAutomaticAdvancementActive { get; }
        double EffectiveRateCycleSecondsPerRealSecond { get; }
        bool TryValidate(out string errorMessage);
        bool TryApply(ITimeControlContext context, float deltaTimeSeconds);
    }
}
