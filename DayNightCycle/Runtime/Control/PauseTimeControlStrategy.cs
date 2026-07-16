namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Pauses automatic advancement while retaining command policy in the owning request.
    /// </summary>
    internal sealed class PauseTimeControlStrategy : ITimeControlStrategy
    {
        public bool IsValid => true;
        public bool IsAutomaticAdvancementActive => false;
        public double EffectiveRateCycleSecondsPerRealSecond => 0d;

        public bool TryValidate(out string errorMessage)
        {
            errorMessage = null;
            return true;
        }

        public bool TryApply(ITimeControlContext context, float deltaTimeSeconds)
        {
            return true;
        }
    }
}
