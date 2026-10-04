using System;
using System.Collections.Generic;

namespace FakeMG.Settings
{
    [Serializable]
    public class SettingDataSnapshot
    {
        public Dictionary<string, string> Values = new();
        public Dictionary<string, string> ValueTypes = new();
    }
}