using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.SOEventSystem.EventChannel
{
    /// <summary>
    /// This event channel broadcasts and carries Boolean payload.
    /// </summary>
    [CreateAssetMenu(menuName = FakeMGEditorMenus.ROOT + "/Events/BoolEventChannelSO")]
    public class BoolEventChannelSO : GenericEventChannelSO<bool> { }
}