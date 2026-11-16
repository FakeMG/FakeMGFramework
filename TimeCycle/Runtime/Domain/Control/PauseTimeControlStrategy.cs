namespace FakeMG.TimeCycle
{
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
