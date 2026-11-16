using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.SOEventSystem.EventChannel
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.ROOT + "/Events/Vector3EventChannelSO")]
    public class Vector3EventChannelSO : GenericEventChannelSO<Vector3> { }
}