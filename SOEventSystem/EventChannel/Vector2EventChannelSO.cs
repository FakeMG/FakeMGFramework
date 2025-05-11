using UnityEngine;

namespace FakeMG.SOEventSystem.EventChannel {
    /// <summary>
    /// General Event Channel that broadcasts and carries Vector2 payload.
    /// </summary>
    /// 
    [CreateAssetMenu(menuName = "Events/Vector2EventChannelSO", fileName = "Vector2EventChannel")]
    public class Vector2EventChannelSO : GenericEventChannelSO<Vector2> {
    }
}