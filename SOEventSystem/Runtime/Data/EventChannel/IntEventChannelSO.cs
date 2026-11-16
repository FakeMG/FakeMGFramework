using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.SOEventSystem.EventChannel
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.ROOT + "/Events/IntEventChannelSO")]
    public class IntEventChannelSO : GenericEventChannelSO<int> { }
}