namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Stores one typed continuous value at an evaluated time.
    /// </summary>
    internal readonly struct ContinuousCycleValue<T>
    {
        public double TimeSeconds { get; }
        public T Value { get; }

        public ContinuousCycleValue(double timeSeconds, T value)
        {
            TimeSeconds = timeSeconds;
            Value = value;
        }
    }
}
