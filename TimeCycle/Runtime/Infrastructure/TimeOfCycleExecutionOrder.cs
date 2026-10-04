namespace FakeMG.TimeCycle
{
    /// <summary>
    /// Ensures environment state is captured before VContainer LifetimeScope initializes entry points at -5000.
    /// </summary>
    // TODO: move state capture into an explicit TimeCycle bootstrap/initialization step.
    // Implicit coupling to VContainer LifetimeScope is not ideal
    internal static class TimeOfCycleExecutionOrder
    {
        public const int ENVIRONMENT_APPLICATOR = -6000;
    }
}
