namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Exposes one typed continuous value to shared evaluator construction.
    /// </summary>
    internal interface IContinuousCyclePoint<out T>
    {
        double TimeSeconds { get; }
        T Value { get; }
    }
}
