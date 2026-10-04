namespace FakeMG.SaveLoad
{
    /// <summary>
    /// Defines synchronous state capture and restoration without imposing a Unity base class.
    /// MonoBehaviours, ScriptableObjects, and plain objects can implement the same persistence contract.
    /// </summary>
    public interface ISaveable
    {
        string SaveId { get; }

        object CaptureState();
        bool TryValidateState(object state, out string failureReason);
        void RestoreState(object state);
        void RestoreDefaultState();
    }
}
