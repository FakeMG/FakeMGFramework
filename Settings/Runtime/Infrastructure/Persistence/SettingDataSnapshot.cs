using System;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace FakeMG.Settings
{
    /// <summary>Every saved setting value as text, keyed by setting id.</summary>
    [Serializable, Preserve]
    public class SettingDataSnapshot
    {
        public Dictionary<string, string> Values = new();
    }
}
