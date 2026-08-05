using FakeMG.Framework.EventBus;
using FakeMG.SaveLoad;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FakeMG.Settings
{
    /// <summary>
    /// Converts the settings save event into an asynchronous save request. Subscription ownership is
    /// paired with this component's enabled lifetime so repeated scene activation cannot leak handlers.
    /// </summary>
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
            _saveLoadSystem.SaveGameAsync().Forget();
        }
    }
}
