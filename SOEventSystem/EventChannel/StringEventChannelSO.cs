using UnityEngine;

namespace FakeMG.SOEventSystem.EventChannel {
    /// <summary>
    /// General event channel that broadcasts and carries string payload.
    /// </summary>
    [CreateAssetMenu(menuName = "Events/StringEventChannelSO", fileName = "StringEventChannel")]
    public class StringEventChannelSO : GenericEventChannelSO<string> {
    }
}