using UnityEngine;

namespace FakeMG.Framework.SOEventSystem.EventChannel
{
    /// <summary>
    /// General event channel that broadcasts and carries Vector3 payload.
    /// </summary>
    [CreateAssetMenu(menuName = FakeMGEditorMenus.ROOT + "/Events/Vector3EventChannelSO")]
    public class Vector3EventChannelSO : GenericEventChannelSO<Vector3> { }
}