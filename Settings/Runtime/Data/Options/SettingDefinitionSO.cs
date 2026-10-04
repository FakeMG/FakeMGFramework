using UnityEngine;

namespace FakeMG.Settings
{
    public abstract class SettingDefinitionSO : ScriptableObject
    {
        [SerializeField] private string _settingId;
        [SerializeField] private string _label;

        public string SettingId => _settingId;
        public string Label => _label;
    }
}
