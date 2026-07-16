namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Selects how a time command moves through the repeating cycle.
    /// </summary>
    public enum TimeCommandMode
    {
        Immediate,
        SimulatedAdvance,
        SmoothPresentationJump,
    }
}
