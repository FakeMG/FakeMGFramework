using System;

namespace FakeMG.SaveLoad
{
    public interface IAutoSaveGate
    {
        /// <summary>
        /// Stops every autosave, on an interval and on focus loss or quit, until each suspension is disposed. For times the
        /// world is open but should not be saved, e.g. while gameplay loads or after the colony fell. Manual saves still run.
        /// </summary>
        IDisposable SuspendAutoSave(string reason);
    }
}
