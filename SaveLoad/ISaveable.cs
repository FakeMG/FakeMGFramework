namespace FakeMG.SaveLoad
{
    /// <summary>
    /// Defines synchronous state capture and restoration without imposing a Unity base class.
    /// MonoBehaviours, ScriptableObjects, and plain objects can implement the same persistence contract.
    /// </summary>
    public interface ISaveable
    {
        string GetUniqueId()
        {
            return GetType().ToString();
        }

        object CaptureState();
        void RestoreState(object data);
        void RestoreDefaultState();
    }
}
