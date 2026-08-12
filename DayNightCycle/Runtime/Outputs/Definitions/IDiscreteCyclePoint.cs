namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Exposes one typed discrete timeline change to shared evaluator construction.
    /// </summary>
    internal interface IDiscreteCyclePoint<out T>
    {
        double PointProgress01 { get; }
        T Value { get; }

        double ResolveTimeSeconds(double cycleDurationSeconds);
    }
}
