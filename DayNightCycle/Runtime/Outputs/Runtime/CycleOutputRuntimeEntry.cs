namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Tracks evaluation and an optional in-flight profile transition for one output.
    /// </summary>
    internal sealed class CycleOutputRuntimeEntry
    {
        public CycleOutputDefinition Definition { get; }
        public ICycleOutputEvaluator Evaluator { get; }
        public object CurrentValue { get; set; }
        public object TransitionStartValue { get; }
        public float TransitionElapsedSeconds { get; set; }
        public bool IsTransitioning { get; set; }
        public bool HasLoggedInvalidValue { get; set; }

        public CycleOutputRuntimeEntry(
            CycleOutputDefinition definition,
            ICycleOutputEvaluator evaluator,
            object currentValue,
            object transitionStartValue,
            bool isTransitioning)
        {
            Definition = definition;
            Evaluator = evaluator;
            CurrentValue = currentValue;
            TransitionStartValue = transitionStartValue;
            TransitionElapsedSeconds = 0f;
            IsTransitioning = isTransitioning;
        }
    }
}
