using UnityEngine;
using UnityEngine.UI;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Anchors a pointer to a point on a UI tutorial target (its center by default). The anchor
    /// counts as hidden while the target is unregistered, inactive, faded out by a parent
    /// CanvasGroup, or clipped at the pointing position by a UI mask.
    /// </summary>
    public sealed class UiTargetPointerAnchor : ITutorialPointerAnchor
    {
        private const float MINIMUM_VISIBLE_ALPHA01 = 0.5f;

        private readonly TutorialTargetRegistry _registry;
        private readonly TutorialTargetKeySO _targetKey;
        private readonly Vector2 _pointOnTarget01;

        public UiTargetPointerAnchor(TutorialTargetRegistry registry, TutorialTargetKeySO targetKey)
            : this(registry, targetKey, new Vector2(0.5f, 0.5f))
        {
        }

        /// <param name="pointOnTarget01">Where on the target to point, normalized over its rect: (0, 0) is the
        /// bottom-left corner, (1, 1) the top-right. Off-center points keep the hand off a button's label.</param>
        public UiTargetPointerAnchor(TutorialTargetRegistry registry, TutorialTargetKeySO targetKey, Vector2 pointOnTarget01)
        {
            _registry = registry;
            _targetKey = targetKey;
            _pointOnTarget01 = pointOnTarget01;
        }

        #region Public Methods

        public bool TryGetScreenPositionPixels(out Vector2 screenPositionPixels)
        {
            screenPositionPixels = default;
            if (!_registry.TryGet(_targetKey, out ITutorialTarget target) || target.IsDestroyed() || !target.IsAvailable ||
                target.InteractionTransform is not RectTransform targetRect || !IsEffectivelyVisible(targetRect))
            {
                return false;
            }

            Canvas canvas = targetRect.GetComponentInParent<Canvas>();
            Camera canvasCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Rect rect = targetRect.rect;
            Vector3 pointWorld = targetRect.TransformPoint(rect.min + Vector2.Scale(rect.size, _pointOnTarget01));
            Vector2 pointPixels = RectTransformUtility.WorldToScreenPoint(canvasCamera, pointWorld);
            if (IsClippedByParentMask(targetRect, pointPixels, canvasCamera))
            {
                return false;
            }

            screenPositionPixels = pointPixels;
            return true;
        }

        #endregion

        #region Private Methods

        private static bool IsClippedByParentMask(Transform target, Vector2 pointPixels, Camera canvasCamera)
        {
            for (Transform parent = target.parent; parent != null; parent = parent.parent)
            {
                if (parent.TryGetComponent(out RectMask2D rectMask) && !rectMask.IsRaycastLocationValid(pointPixels, canvasCamera))
                {
                    return true;
                }

                if (parent.TryGetComponent(out Mask mask) && mask.MaskEnabled() && mask.graphic.IsActive() &&
                    !mask.IsRaycastLocationValid(pointPixels, canvasCamera))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsEffectivelyVisible(Transform targetTransform)
        {
            float alpha01 = 1f;
            Transform current = targetTransform;
            while (current != null)
            {
                if (current.TryGetComponent(out CanvasGroup group))
                {
                    alpha01 *= group.alpha;
                    if (group.ignoreParentGroups) break;
                }

                current = current.parent;
            }

            return alpha01 >= MINIMUM_VISIBLE_ALPHA01;
        }

        #endregion
    }
}
