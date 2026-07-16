namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Reports one monotonic progress step produced by the active time command.
    /// </summary>
    internal readonly struct TimeCommandFrame
    {
        public double SignedDistanceDeltaSeconds { get; }
        public float DirectedProgress01 { get; }
        public bool HasCompleted { get; }

        public TimeCommandFrame(
            double signedDistanceDeltaSeconds,
            float directedProgress01,
            bool hasCompleted)
        {
            SignedDistanceDeltaSeconds = signedDistanceDeltaSeconds;
            DirectedProgress01 = directedProgress01;
            HasCompleted = hasCompleted;
        }
    }
}
