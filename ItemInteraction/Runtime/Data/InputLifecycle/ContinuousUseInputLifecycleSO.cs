using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.ItemInteraction
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.ITEM_INTERACTION + "/Input/Continuous Use")]
    public sealed class ContinuousUseInputLifecycleSO : HoldToConfirmInputLifecycleSO
    {
        public override bool IsContinuous => true;
    }
}
