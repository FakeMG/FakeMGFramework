namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Reports the previous and current named periods after gameplay time changes.
    /// </summary>
    public readonly struct CyclePeriodChange
    {
        public CyclePeriodId PreviousPeriodId { get; }
        public CyclePeriodId CurrentPeriodId { get; }

        public CyclePeriodChange(CyclePeriodId previousPeriodId, CyclePeriodId currentPeriodId)
        {
            PreviousPeriodId = previousPeriodId;
            CurrentPeriodId = currentPeriodId;
        }
    }
}
