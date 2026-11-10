using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.ItemInteraction
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.ITEM_INTERACTION + "/Input/Hold To Confirm")]
    public class HoldToConfirmInputLifecycleSO : ItemInputLifecycleSO
    {
        public override bool ShouldCancelWhenReleasedBeforeActionMoment => true;

        public override bool CanCommitAtActionMoment(bool isInputHeld)
        {
            return isInputHeld;
        }
    }
}
