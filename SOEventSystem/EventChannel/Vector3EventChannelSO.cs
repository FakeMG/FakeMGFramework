using UnityEngine;

namespace FakeMG.Framework.SOEventSystem.EventChannel
{
    /// <summary>
    /// General event channel that broadcasts and carries Vector3 payload.
    /// </summary>
    [CreateAssetMenu(menuName = "Events/Vector3EventChannelSO", fileName = "Vector3EventChannel")]
    public class Vector3EventChannelSO : GenericEventChannelSO<Vector3> { }
}