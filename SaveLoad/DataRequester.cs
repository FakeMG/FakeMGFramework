using Cysharp.Threading.Tasks;
using FakeMG.Framework.SaveLoad.Advanced;
using UnityEngine;

namespace FakeMG.Framework.SaveLoad
{
    /// <summary>
    /// Base class for systems that need to request and apply save data.
    /// Systems implement this to register with DataApplicationManager and 
    /// receive data when available.
    /// </summary>
    public abstract class DataRequester : MonoBehaviour
    {
        /// <summary>
        /// The scene this system belongs to (used for per-scene data application)
        /// </summary>
        public string SceneName => gameObject.scene.name;

        protected virtual void Awake()
        {
            DataApplicationManager.Instance.RegisterDataRequester(this);
        }

        protected virtual void OnDestroy()
        {
            DataApplicationManager.Instance.UnregisterDataRequester(this);
        }

        public abstract UniTask ApplyDataAsync();
    }
}