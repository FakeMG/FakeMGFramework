namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Represents a one-time request to change authoritative or presentation time.
    /// </summary>
    public readonly struct TimeCommand
    {
        public TimeCommandMode Mode { get; }
        public TimeMovementDirection Direction { get; }
        public double TargetTimeSeconds { get; }
        public TimeCommandTransition Transition { get; }

        private TimeCommand(
            TimeCommandMode mode,
            TimeMovementDirection direction,
            double targetTimeSeconds,
            TimeCommandTransition transition)
        {
            Mode = mode;
            Direction = direction;
            TargetTimeSeconds = targetTimeSeconds;
            Transition = transition;
        }

        #region Public Methods

        public static TimeCommand Immediate(double targetTimeSeconds)
        {
            return new TimeCommand(
                TimeCommandMode.Immediate,
                TimeMovementDirection.Forward,
                targetTimeSeconds,
                new TimeCommandTransition(0f));
        }

        public static TimeCommand SimulatedAdvance(
            double targetTimeSeconds,
            TimeMovementDirection direction)
        {
            return new TimeCommand(
                TimeCommandMode.SimulatedAdvance,
                direction,
                targetTimeSeconds,
                TimeCommandTransition.ProfileDefault());
        }

        public static TimeCommand SimulatedAdvance(
            double targetTimeSeconds,
            TimeMovementDirection direction,
            TimeCommandTransition transition)
        {
            return new TimeCommand(
                TimeCommandMode.SimulatedAdvance,
                direction,
                targetTimeSeconds,
                transition);
        }

        public static TimeCommand SmoothPresentationJump(
            double targetTimeSeconds,
            TimeMovementDirection direction)
        {
            return new TimeCommand(
                TimeCommandMode.SmoothPresentationJump,
                direction,
                targetTimeSeconds,
                TimeCommandTransition.ProfileDefault());
        }

        public static TimeCommand SmoothPresentationJump(
            double targetTimeSeconds,
            TimeMovementDirection direction,
            TimeCommandTransition transition)
        {
            return new TimeCommand(
                TimeCommandMode.SmoothPresentationJump,
                direction,
                targetTimeSeconds,
                transition);
        }

        #endregion
    }
}
