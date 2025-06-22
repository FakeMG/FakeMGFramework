using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace FakeMG.FakeMGFramework.SOEventSystem.EventChannel
{
    /// <summary>
    /// General Event Channel that carries no extra data.
    /// </summary>
    [CreateAssetMenu(menuName = "Events/VoidEventChannelSO", fileName = "VoidEventChannel")]
    public class VoidEventChannelSO : ScriptableObject
    {
        [Tooltip("The action to perform")]
        public UnityAction OnEventRaised;

        [Button]
        public void RaiseEvent()
        {
            if (OnEventRaised != null)
                OnEventRaised.Invoke();
        }
    }
}