namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Stores one ordered discrete state change and its tie-breaking priority.
    /// </summary>
    internal readonly struct DiscreteCycleChange<T>
    {
        public double TimeSeconds { get; }
        public int Priority { get; }
        public T Value { get; }

        public DiscreteCycleChange(double timeSeconds, int priority, T value)
        {
            TimeSeconds = timeSeconds;
            Priority = priority;
            Value = value;
        }
    }
}
