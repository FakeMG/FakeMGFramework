namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Describes one observable boundary crossed while advancing the cyclic clock.
    /// </summary>
    internal readonly struct CycleClockNotification
    {
        public bool IsCycleCompleted { get; }
        public CyclePeriodChange PeriodChange { get; }

        private CycleClockNotification(bool isCycleCompleted, CyclePeriodChange periodChange)
        {
            IsCycleCompleted = isCycleCompleted;
            PeriodChange = periodChange;
        }

        #region Public Methods

        public static CycleClockNotification CycleCompleted()
        {
            return new CycleClockNotification(true, default);
        }

        public static CycleClockNotification PeriodChanged(CyclePeriodChange periodChange)
        {
            return new CycleClockNotification(false, periodChange);
        }

        #endregion
    }
}
