namespace FakeMG.TimeCycle
{
    internal interface IPeriodCycleValue<out T>
    {
        CyclePeriodId PeriodId { get; }
        T Value { get; }
    }
}
