namespace FakeMG.TimeCycle
{
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
