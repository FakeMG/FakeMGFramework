namespace FakeMG.SaveLoad
{
    public interface ISaveable
    {
        string SaveId { get; }

        object CaptureState();
        bool TryValidateState(object state, out string failureReason);
        void RestoreState(object state);
        void RestoreDefaultState();
    }
}
