using UnityEngine;
using UnityEngine.UI;

namespace FakeMG.FakeMGFramework.ExtensionMethods
{
    public static class ScrollRectExtension
    {
        public static void ScrollTo(this ScrollRect scrollRect, Transform target, bool isVertical = true)
        {
            scrollRect.normalizedPosition = isVertical
                ? new Vector2(0f, 1f - (scrollRect.content.rect.height / 2f - target.localPosition.y) / scrollRect.content.rect.height)
                : new Vector2(1f - (scrollRect.content.rect.width / 2f - target.localPosition.x) / scrollRect.content.rect.width, 0f);
        }
    }
}