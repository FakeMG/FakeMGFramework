using UnityEngine;

namespace FakeMG.Settings
{
    public abstract class SettingDefinitionSO : ScriptableObject
    {
        [SerializeField] private string _settingId;
        [SerializeField] private string _label;
        [Tooltip("Shown to the player next to the setting, to explain what it changes.")]
        [SerializeField, TextArea] private string _description;

        public string SettingId => _settingId;
        public string Label => _label;
        public string Description => _description;
    }
}
