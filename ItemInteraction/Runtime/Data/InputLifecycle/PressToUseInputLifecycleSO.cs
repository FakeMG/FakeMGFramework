using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.ItemInteraction
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.ITEM_INTERACTION + "/Input/Press To Use")]
    public sealed class PressToUseInputLifecycleSO : ItemInputLifecycleSO
    {
        public override bool ShouldCancelWhenReleasedBeforeActionMoment => false;

        public override bool CanCommitAtActionMoment(bool isInputHeld)
        {
            return true;
        }
    }
}
