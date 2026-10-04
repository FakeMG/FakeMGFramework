using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.SOEventSystem.EventChannel
{
    /// <summary>
    /// General event channel that broadcasts and carries string payload.
    /// </summary>
    [CreateAssetMenu(menuName = FakeMGEditorMenus.ROOT + "/Events/StringEventChannelSO")]
    public class StringEventChannelSO : GenericEventChannelSO<string> { }
}