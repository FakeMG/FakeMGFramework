namespace FakeMG.TimeCycle
{
    public interface IWorldTimelineCycleRecorder
    {
        void RecordCompletedCycle();
    }

    public interface IWorldTimeline
    {
        double AuthoritativeTimeSeconds { get; }
        long CurrentDay { get; }
    }
}
