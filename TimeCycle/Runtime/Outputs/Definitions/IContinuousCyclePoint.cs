namespace FakeMG.TimeCycle
{
    /// <summary>
    /// Exposes one typed continuous value to shared evaluator construction.
    /// </summary>
    internal interface IContinuousCyclePoint<out T>
    {
        double PointProgress01 { get; }
        T Value { get; }

        double ResolveTimeSeconds(double cycleDurationSeconds);
    }
}
