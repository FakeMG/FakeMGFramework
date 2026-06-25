using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Visual view that points at a UI target. The pointer is offset from the target so
    /// it does not cover it, flips when any part would be outside the canvas,
    /// and pulses with a looping scale animation to draw the eye.
    /// </summary>
    public sealed class TutorialPointerView : AnimatedTutorialVisualView
    {
        [SerializeField] private RectTransform _pointerRect;
        [SerializeField] private Vector2 _targetOffsetPixels = new(0f, -90f);
        [SerializeField] private float _pulseScale = 1.15f;
        [SerializeField] private float _pulseDurationSeconds = 0.6f;

        private Tween _pulseTween;
        private Vector3 _baseScale = Vector3.one;

        private void OnDestroy()
        {
            StopPulse();
        }

        public void PointAt(RectTransform target, RectTransform visualRoot)
        {
            Canvas targetCanvas = target.GetComponentInParent<Canvas>();
            Camera targetCamera = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? targetCanvas.worldCamera
                : null;

            Vector2 targetScreenPosition = RectTransformUtility.WorldToScreenPoint(targetCamera, target.position);

            Canvas visualCanvas = visualRoot.GetComponentInParent<Canvas>();
            Camera visualCamera = visualCanvas != null && visualCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? visualCanvas.worldCamera
                : null;

            Vector2 offset = _targetOffsetPixels;
            Vector2 flip = Vector2.one;

            ApplyFlip(flip);

            if (!TryApplyPointerPosition(target, visualRoot, visualCamera, targetScreenPosition, offset))
            {
                return;
            }

            if (!IsOutsideCanvas(_pointerRect, visualRoot))
            {
                return;
            }

            Bounds viewBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(visualRoot, _pointerRect);
            Rect rootRect = visualRoot.rect;

            if (viewBounds.min.x < rootRect.xMin || viewBounds.max.x > rootRect.xMax)
            {
                offset.x *= -1f;
                flip.x *= -1f;
            }

            if (viewBounds.min.y < rootRect.yMin || viewBounds.max.y > rootRect.yMax)
            {
                offset.y *= -1f;
                flip.y *= -1f;
            }

            ApplyFlip(flip);
            TryApplyPointerPosition(target, visualRoot, visualCamera, targetScreenPosition, offset);
        }

        public override async UniTask ShowAsync(CancellationToken cancellationToken)
        {
            await base.ShowAsync(cancellationToken);
            StartPulse();
        }

        public override async UniTask HideAsync(CancellationToken cancellationToken)
        {
            StopPulse();
            await base.HideAsync(cancellationToken);
        }

        private bool TryApplyPointerPosition(
            RectTransform target,
            RectTransform visualRoot,
            Camera visualCamera,
            Vector2 targetScreenPosition,
            Vector2 offsetPixels)
        {
            Vector2 desiredScreenPosition = targetScreenPosition + offsetPixels;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    visualRoot,
                    desiredScreenPosition,
                    visualCamera,
                    out Vector2 localPosition))
            {
                Echo.Warning($"Cannot position tutorial pointer for target '{target.name}' because its screen position could not be converted.");
                return false;
            }

            _pointerRect.anchoredPosition = localPosition;
            _pointerRect.ForceUpdateRectTransforms();

            return true;
        }

        private void ApplyFlip(Vector2 flip)
        {
            _baseScale = new Vector3(
                Mathf.Sign(flip.x),
                Mathf.Sign(flip.y),
                1f);

            if (_pulseTween == null || !_pulseTween.IsActive())
            {
                _pointerRect.localScale = _baseScale;
            }
        }

        private static bool IsOutsideCanvas(RectTransform view, RectTransform canvasRoot)
        {
            Bounds viewBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRoot, view);
            Rect canvasRect = canvasRoot.rect;

            return viewBounds.min.x < canvasRect.xMin
                   || viewBounds.max.x > canvasRect.xMax
                   || viewBounds.min.y < canvasRect.yMin
                   || viewBounds.max.y > canvasRect.yMax;
        }

        private void StartPulse()
        {
            StopPulse();

            _pointerRect.localScale = _baseScale;

            Vector3 targetScale = new(
                _baseScale.x * _pulseScale,
                _baseScale.y * _pulseScale,
                _baseScale.z * _pulseScale);

            _pulseTween = _pointerRect
                .DOScale(targetScale, _pulseDurationSeconds)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        private void StopPulse()
        {
            if (_pulseTween != null)
            {
                _pulseTween.Kill();
                _pulseTween = null;
            }

            _pointerRect.localScale = _baseScale;
        }
    }
}