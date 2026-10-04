namespace FakeMG.TimeCycle
{
    public interface IReadOnlyTimeOfCycle
    {
        bool IsInitialized { get; }
        TimeOfCycleState CurrentState { get; }
        TimeOfCycleLayout ActiveLayout { get; }
    }
}
