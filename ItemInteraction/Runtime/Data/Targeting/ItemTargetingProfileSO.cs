using UnityEngine;

namespace FakeMG.ItemInteraction
{
    public class ItemTargetingProfileSO : ScriptableObject
    {
        public virtual Vector2 ResolveScreenPositionPixels(Vector2 pointerPositionPixels, Vector2 screenSizePixels)
        {
            return pointerPositionPixels;
        }
    }
}
