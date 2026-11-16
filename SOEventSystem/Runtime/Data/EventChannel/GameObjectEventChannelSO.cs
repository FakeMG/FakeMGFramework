using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.SOEventSystem.EventChannel
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.ROOT + "/Events/GameObjectEventChannelSO")]
    public class GameObjectEventChannelSO : GenericEventChannelSO<GameObject> { }
}