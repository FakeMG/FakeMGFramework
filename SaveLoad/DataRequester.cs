using Cysharp.Threading.Tasks;
using FakeMG.Framework.EventBus;
using FakeMG.SaveLoad.Advanced;
using UnityEngine;

namespace FakeMG.SaveLoad
{
    /// <summary>
    /// Base class for systems that need to request and apply save data.
    /// Systems implement this to register with DataApplicationManager and 
    /// so that they can be awaited during the data application process.
    /// This is used for systems that need to wait for the data to be applied before they can function properly
    /// </summary>
    public abstract class DataRequester : MonoBehaviour
    {
        /// <summary>
        /// The scene this system belongs to (used for per-scene data application)
        /// </summary>
        public string SceneName => gameObject.scene.name;

        protected virtual void Awake()
        {
            RequestRegistration();
        }

        protected virtual void OnDestroy()
        {
            RequestUnregistration();
        }

        private void RequestRegistration()
        {
            var registerEvent = new RegisterDataRequesterEvent
            {
                Requester = this
            };

            EventBus<RegisterDataRequesterEvent>.Raise(registerEvent);
        }

        private void RequestUnregistration()
        {
            var unregisterEvent = new UnregisterDataRequesterEvent
            {
                Requester = this
            };

            EventBus<UnregisterDataRequesterEvent>.Raise(unregisterEvent);
        }

        public abstract UniTask ApplyDataAsync();
    }
}