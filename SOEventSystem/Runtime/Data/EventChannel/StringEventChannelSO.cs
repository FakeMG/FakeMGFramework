using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.SOEventSystem.EventChannel
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.ROOT + "/Events/StringEventChannelSO")]
    public class StringEventChannelSO : GenericEventChannelSO<string> { }
}