using FakeMG.Framework.EventBus;
using FakeMG.SaveLoad.Advanced;
using UnityEngine;

namespace FakeMG.Settings
{
    public class SettingAutoSaveEventSubscriber : MonoBehaviour
    {
        [SerializeField] private SaveLoadSystem _saveLoadSystem;

        private void OnEnable()
        {
            EventBus<SettingsAutoSaveEvent>.OnEventNoArgs += HandleSettingsAutoSaveEvent;
        }

        private void OnDisable()
        {
            EventBus<SettingsAutoSaveEvent>.OnEventNoArgs -= HandleSettingsAutoSaveEvent;
        }

        private void HandleSettingsAutoSaveEvent()
        {
            _saveLoadSystem.SaveGame();
        }
    }
}