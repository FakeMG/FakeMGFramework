using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace FakeMG.Framework.ExtensionMethods
{
    public static class ScrollRectExtension
    {
        public static void ScrollTo(this ScrollRect scrollRect, Transform target, bool isVertical = true)
        {
            if (scrollRect == null || scrollRect.content == null || target == null) return;

            // Compute normalized positions based on content dimensions and clamp to [0,1].
            const float DURATION = 0.35f;
            if (isVertical)
            {
                float finalNormalizedY = CalculateNormalizedPosition(scrollRect, target, true);

                DOTween.Kill(scrollRect);
                scrollRect.DOVerticalNormalizedPos(finalNormalizedY, DURATION).SetEase(Ease.OutQuad);
            }
            else
            {
                float finalNormalizedX = CalculateNormalizedPosition(scrollRect, target, false);

                DOTween.Kill(scrollRect);
                scrollRect.DOHorizontalNormalizedPos(finalNormalizedX, DURATION).SetEase(Ease.OutQuad);
            }
        }

        public static void SnapTo(this ScrollRect scrollRect, Transform target, bool isVertical = true)
        {
            if (scrollRect == null || scrollRect.content == null || target == null) return;

            if (isVertical)
            {
                float finalNormalizedY = CalculateNormalizedPosition(scrollRect, target, true);

                DOTween.Kill(scrollRect);
                scrollRect.verticalNormalizedPosition = finalNormalizedY;
            }
            else
            {
                float finalNormalizedX = CalculateNormalizedPosition(scrollRect, target, false);

                DOTween.Kill(scrollRect);
                scrollRect.horizontalNormalizedPosition = finalNormalizedX;
            }
        }

        private static float CalculateNormalizedPosition(ScrollRect scrollRect, Transform target, bool isVertical)
        {
            var content = scrollRect.content;
            var viewport = scrollRect.viewport != null ? scrollRect.viewport : scrollRect.GetComponent<RectTransform>();

            if (isVertical)
            {
                float contentHeight = content.rect.height;
                float viewportHeight = Mathf.Max(0.0001f, viewport.rect.height);

                // Convert target world position into content-local space
                float targetLocalY = content.InverseTransformPoint(target.position).y;

                // Top edge local Y (depends on content pivot)
                float topEdgeLocalY = contentHeight * (1f - content.pivot.y);

                // Distance from top edge down to the target center
                float distanceFromTop = topEdgeLocalY - targetLocalY;

                // Scrollable range (content minus viewport)
                float scrollable = Mathf.Max(0.0001f, contentHeight - viewportHeight);

                // Center the target in the viewport vertically
                float desiredScroll = distanceFromTop - viewportHeight * 0.5f;

                float normalized = Mathf.Clamp01(desiredScroll / scrollable);
                return 1f - normalized; // ScrollRect y: 1 = top, 0 = bottom
            }
            else
            {
                float contentWidth = content.rect.width;
                float viewportWidth = Mathf.Max(0.0001f, viewport.rect.width);

                float targetLocalX = content.InverseTransformPoint(target.position).x;
                float leftEdgeLocalX = -contentWidth * content.pivot.x; // left edge in local coords

                // Distance from left edge to target center
                float distanceFromLeft = targetLocalX - leftEdgeLocalX;

                float scrollable = Mathf.Max(0.0001f, contentWidth - viewportWidth);

                // Center the target in the viewport horizontally
                float desiredScroll = distanceFromLeft - viewportWidth * 0.5f;

                float normalized = Mathf.Clamp01(desiredScroll / scrollable);
                return normalized; // ScrollRect x: 0 = left, 1 = right
            }
        }
    }
}