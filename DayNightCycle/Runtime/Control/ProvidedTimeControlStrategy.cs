namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Samples authoritative cycle time from one externally owned provider.
    /// </summary>
    internal sealed class ProvidedTimeControlStrategy : ITimeControlStrategy
    {
        private readonly ITimeOfCycleTimeProvider _timeProvider;

        public bool IsValid => _timeProvider != null && _timeProvider.IsValid;
        public bool IsAutomaticAdvancementActive => false;
        public double EffectiveRateCycleSecondsPerRealSecond => 0d;

        public ProvidedTimeControlStrategy(ITimeOfCycleTimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
        }

        public bool TryValidate(out string errorMessage)
        {
            if (!IsValid)
            {
                errorMessage = "A valid direct-time provider is required.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        public bool TryApply(ITimeControlContext context, float deltaTimeSeconds)
        {
            if (!IsValid
                || !_timeProvider.TryGetCurrentTimeSeconds(out double providedTimeSeconds)
                || !CycleNumericValidation.IsFinite(providedTimeSeconds))
            {
                return false;
            }

            return context.TrySetAuthoritativeTime(providedTimeSeconds);
        }
    }
}
