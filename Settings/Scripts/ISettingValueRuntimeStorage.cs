using System;

namespace FakeMG.Settings
{
    public interface ISettingValueRuntimeStorage
    {
        string SettingId { get; }
        Type ValueType { get; }
        object GetValue();
        PersistedSettingValue CapturePersistedValue();
        bool TryRestore(PersistedSettingValue persistedValue, out string failureReason);
        void RestoreDefault();
    }
}
