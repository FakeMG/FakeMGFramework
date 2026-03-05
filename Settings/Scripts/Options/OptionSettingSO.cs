using System.Collections.Generic;

namespace FakeMG.Settings
{
    public abstract class OptionSettingSO : SettingDataGeneric<int>
    {
        public abstract List<string> GetOptions();
    }
}