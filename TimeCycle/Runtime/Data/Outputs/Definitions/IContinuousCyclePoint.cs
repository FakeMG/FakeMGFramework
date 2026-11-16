namespace FakeMG.TimeCycle
{
    internal interface IContinuousCyclePoint<out T>
    {
        double PointProgress01 { get; }
        T Value { get; }

        double ResolveTimeSeconds(double cycleDurationSeconds);
    }
}
