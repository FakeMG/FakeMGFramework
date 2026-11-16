using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.SOEventSystem.EventChannel
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.ROOT + "/Events/BoolEventChannelSO")]
    public class BoolEventChannelSO : GenericEventChannelSO<bool> { }
}