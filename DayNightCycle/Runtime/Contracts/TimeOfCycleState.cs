namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Describes the observable state of the repeating cycle at one frame.
    /// </summary>
    public readonly struct TimeOfCycleState
    {
        public double AuthoritativeTimeSeconds { get; }
        public double PresentationTimeSeconds { get; }
        public double NormalizedCycleProgress01 { get; }
        public CyclePeriodId CurrentPeriodId { get; }
        public bool IsAutomaticAdvancementActive { get; }
        public double AdvancementRateCycleSecondsPerRealSecond { get; }

        public TimeOfCycleState(
            double authoritativeTimeSeconds,
            double presentationTimeSeconds,
            double normalizedCycleProgress01,
            CyclePeriodId currentPeriodId,
            bool isAutomaticAdvancementActive,
            double advancementRateCycleSecondsPerRealSecond)
        {
            AuthoritativeTimeSeconds = authoritativeTimeSeconds;
            PresentationTimeSeconds = presentationTimeSeconds;
            NormalizedCycleProgress01 = normalizedCycleProgress01;
            CurrentPeriodId = currentPeriodId;
            IsAutomaticAdvancementActive = isAutomaticAdvancementActive;
            AdvancementRateCycleSecondsPerRealSecond = advancementRateCycleSecondsPerRealSecond;
        }
    }
}
