namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Defines one persistent request participating in priority arbitration.
    /// </summary>
    public sealed class TimeControlRequest
    {
        public int Priority { get; }
        public bool AllowsTimeCommands { get; }
        public double AdvancementRateCycleSecondsPerRealSecond { get; }
        public ITimeOfCycleTimeProvider TimeProvider { get; }
        internal ITimeControlStrategy Strategy { get; }

        private TimeControlRequest(
            int priority,
            bool allowsTimeCommands,
            double advancementRateCycleSecondsPerRealSecond,
            ITimeOfCycleTimeProvider timeProvider,
            ITimeControlStrategy strategy)
        {
            Priority = priority;
            AllowsTimeCommands = allowsTimeCommands;
            AdvancementRateCycleSecondsPerRealSecond = advancementRateCycleSecondsPerRealSecond;
            TimeProvider = timeProvider;
            Strategy = strategy;
        }

        #region Public Methods

        public static TimeControlRequest Pause(int priority, bool allowsTimeCommands)
        {
            return new TimeControlRequest(priority, allowsTimeCommands, 0d, null, new PauseTimeControlStrategy());
        }

        public static TimeControlRequest Create(
            ITimeControlStrategy strategy,
            int priority,
            bool allowsTimeCommands)
        {
            return new TimeControlRequest(priority, allowsTimeCommands, 0d, null, strategy);
        }

        public static TimeControlRequest OverrideAdvancementRate(
            double advancementRateCycleSecondsPerRealSecond,
            int priority,
            bool allowsTimeCommands)
        {
            return new TimeControlRequest(
                priority,
                allowsTimeCommands,
                advancementRateCycleSecondsPerRealSecond,
                null,
                new AdvancementRateTimeControlStrategy(advancementRateCycleSecondsPerRealSecond));
        }

        public static TimeControlRequest ProvideAuthoritativeTime(
            ITimeOfCycleTimeProvider timeProvider,
            int priority,
            bool allowsTimeCommands)
        {
            return new TimeControlRequest(
                priority,
                allowsTimeCommands,
                0d,
                timeProvider,
                new ProvidedTimeControlStrategy(timeProvider));
        }

        #endregion
    }
}
