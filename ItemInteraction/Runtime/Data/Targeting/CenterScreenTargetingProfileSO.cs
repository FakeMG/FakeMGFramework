using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.ItemInteraction
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.ITEM_INTERACTION + "/Targeting/Center Screen")]
    public sealed class CenterScreenTargetingProfileSO : ItemTargetingProfileSO
    {
        public override Vector2 ResolveScreenPositionPixels(Vector2 pointerPositionPixels, Vector2 screenSizePixels)
        {
            return screenSizePixels * 0.5f;
        }
    }
}
