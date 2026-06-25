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
        [SerializeField] private RectTransform _positionRoot;
        [SerializeField] private RectTransform _flipRoot;
        [SerializeField] private RectTransform _pulseRoot;
        [SerializeField] private Vector2 _targetOffsetPixels = new(0f, -90f);
        [SerializeField] private float _pulseScale = 1.15f;
        [SerializeField] private float _pulseDurationSeconds = 0.6f;

        private Tween _pulseTween;
        private Vector3 _flipScale = Vector3.one;

        private void Reset()
        {
            _positionRoot = transform as RectTransform;
            _flipRoot = transform as RectTransform;
            _pulseRoot = transform as RectTransform;
        }

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

            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(visualRoot, _positionRoot);
            Rect rootRect = visualRoot.rect;

            bool shouldFlipHorizontally = bounds.min.x < rootRect.xMin || bounds.max.x > rootRect.xMax;
            bool shouldFlipVertically = bounds.min.y < rootRect.yMin || bounds.max.y > rootRect.yMax;

            if (!shouldFlipHorizontally && !shouldFlipVertically)
            {
                return;
            }

            if (shouldFlipHorizontally)
            {
                offset.x *= -1f;
                flip.x *= -1f;
            }

            if (shouldFlipVertically)
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

            _positionRoot.anchoredPosition = localPosition;
            _positionRoot.ForceUpdateRectTransforms();

            return true;
        }

        private void ApplyFlip(Vector2 flip)
        {
            _flipScale = new Vector3(
                flip.x < 0f ? -1f : 1f,
                flip.y < 0f ? -1f : 1f,
                1f);

            _flipRoot.localScale = _flipScale;
        }

        private void StartPulse()
        {
            StopPulse();

            _pulseRoot.localScale = Vector3.one;

            _pulseTween = _pulseRoot
                .DOScale(Vector3.one * _pulseScale, _pulseDurationSeconds)
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

            _pulseRoot.localScale = Vector3.one;
        }
    }
}