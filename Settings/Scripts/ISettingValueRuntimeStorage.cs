using System;

namespace FakeMG.Settings
{
    public interface ISettingValueRuntimeStorage
    {
        Type ValueType { get; }
        object GetValue();
    }
}