using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.SOEventSystem.EventChannel
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.ROOT + "/Events/FloatEventChannelSO")]
    public class FloatEventChannelSO : GenericEventChannelSO<float> { }
}