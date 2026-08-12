namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Ensures environment state is captured before VContainer LifetimeScope initializes entry points at -5000.
    /// </summary>
    internal static class TimeOfCycleExecutionOrder
    {
        public const int ENVIRONMENT_APPLICATOR = -6000;
    }
}
