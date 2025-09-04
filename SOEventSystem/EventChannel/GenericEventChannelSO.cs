using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace FakeMG.Framework.SOEventSystem.EventChannel
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

    // To create addition event channels, simply derive a class from GenericEventChannelSO
    // filling in the type T. Leave the concrete implementation blank. This is a quick way
    // to create new event channels.

    // For example:
    //[CreateAssetMenu(menuName = "Events/Float EventChannel", fileName = "FloatEventChannel")]
    //public class FloatEventChannelSO : GenericEventChannelSO<float> {}
}