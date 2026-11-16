using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace FakeMG.SOEventSystem.EventChannel
{
    public abstract class GenericEventChannelSO<T> : ScriptableObject
    {
        [Tooltip("The action to perform; Listeners subscribe to this UnityAction")]
        public UnityAction<T> OnEventRaised;

        [Button]
        public void RaiseEvent(T parameter)
        {
            OnEventRaised?.Invoke(parameter);
        }
    }


}