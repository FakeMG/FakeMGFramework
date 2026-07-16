namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Advances the clock at one persistent cycle-to-real-time rate.
    /// </summary>
    internal sealed class AdvancementRateTimeControlStrategy : ITimeControlStrategy
    {
        private readonly double _rateCycleSecondsPerRealSecond;

        public bool IsValid => true;
        public bool IsAutomaticAdvancementActive => true;
        public double EffectiveRateCycleSecondsPerRealSecond => _rateCycleSecondsPerRealSecond;

        public AdvancementRateTimeControlStrategy(double rateCycleSecondsPerRealSecond)
        {
            _rateCycleSecondsPerRealSecond = rateCycleSecondsPerRealSecond;
        }

        public bool TryValidate(out string errorMessage)
        {
            if (!CycleNumericValidation.IsFiniteNonNegative(_rateCycleSecondsPerRealSecond))
            {
                errorMessage = "Override advancement rate must be finite and non-negative.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        public bool TryApply(ITimeControlContext context, float deltaTimeSeconds)
        {
            return context.TryAdvanceAuthoritativeTime(
                _rateCycleSecondsPerRealSecond * deltaTimeSeconds);
        }
    }
}
