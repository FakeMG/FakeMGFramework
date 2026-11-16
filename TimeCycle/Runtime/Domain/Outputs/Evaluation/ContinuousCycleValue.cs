namespace FakeMG.TimeCycle
{
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
