using System.Collections.Generic;

namespace FakeMG.Settings
{
    public abstract class OptionSettingSO : SettingDefinitionGenericSO<string>
    {
        public abstract List<string> GetOptions();
    }
}