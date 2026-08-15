namespace FakeMG.TimeCycle
{
    /// <summary>
    /// Holds one profile period resolved into runtime clock seconds.
    /// </summary>
    internal sealed class ResolvedCyclePeriod
    {
        public CyclePeriodId PeriodId { get; }
        public double StartTimeSeconds { get; }

        public ResolvedCyclePeriod(CyclePeriodId periodId, double startTimeSeconds)
        {
            PeriodId = periodId;
            StartTimeSeconds = startTimeSeconds;
        }
    }
}
