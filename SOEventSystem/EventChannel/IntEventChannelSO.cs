using UnityEngine;

namespace FakeMG.Framework.SOEventSystem.EventChannel
{
    /// <summary>
    /// This is a ScriptableObject-based event that takes an integer as a payload.
    /// </summary> 
    [CreateAssetMenu(menuName = FakeMGEditorMenus.ROOT + "/Events/IntEventChannelSO")]
    public class IntEventChannelSO : GenericEventChannelSO<int> { }
}