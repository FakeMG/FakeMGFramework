namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Exposes one typed named-period value to shared evaluator construction.
    /// </summary>
    internal interface IPeriodCycleValue<out T>
    {
        CyclePeriodId PeriodId { get; }
        T Value { get; }
    }
}
