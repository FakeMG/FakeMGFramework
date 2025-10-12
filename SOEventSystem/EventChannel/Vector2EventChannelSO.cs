using UnityEngine;

namespace FakeMG.Framework.SOEventSystem.EventChannel
{
    /// <summary>
    /// General Event Channel that broadcasts and carries Vector2 payload.
    /// </summary>
    /// 
    [CreateAssetMenu(menuName = FakeMGEditorMenus.ROOT + "/Events/Vector2EventChannelSO")]
    public class Vector2EventChannelSO : GenericEventChannelSO<Vector2> { }
}