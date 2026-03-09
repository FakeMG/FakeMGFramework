using System.Collections.Generic;

namespace FakeMG.Settings
{
    public abstract class OptionSettingSO : SettingDefinitionGenericSO<int>
    {
        public abstract List<string> GetOptions();
    }
}