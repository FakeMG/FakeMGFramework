using FakeMG.Framework;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace FakeMG.SOEventSystem.EventChannel
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.ROOT + "/Events/VoidEventChannelSO")]
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