namespace FakeMG.TimeCycle
{
    internal sealed class ResolvedCyclePeriod
    {
        public CyclePeriodId PeriodId { get; }
        public double StartTimeSeconds { get; }
        public double DurationSeconds { get; }

        public ResolvedCyclePeriod(CyclePeriodId periodId, double startTimeSeconds, double durationSeconds)
        {
            PeriodId = periodId;
            StartTimeSeconds = startTimeSeconds;
            DurationSeconds = durationSeconds;
        }
    }
}
