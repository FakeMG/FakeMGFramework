using FakeMG.Framework;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Visual view that points at a UI target. The pointer is offset from the target so
    /// it does not cover it, clamped on screen when the target is near or past an edge,
    /// and pulses with a looping scale animation to draw the eye.
    /// </summary>
    public sealed class TutorialPointerView : AnimatedTutorialVisualView
    {
        [SerializeField] private RectTransform _pointerRect;
        [SerializeField] private Vector2 _targetOffsetPixels = new(0f, -90f);
        [SerializeField] private Vector2 _screenPaddingPixels = new(48f, 48f);
        [SerializeField] private float _pulseScale = 1.15f;
        [SerializeField] private float _pulseDurationSeconds = 0.6f;

        private Tween _pulseTween;

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
            Vector3 desiredScreenPosition = targetScreenPosition + _targetOffsetPixels;
            Vector3 clampedScreenPosition = ClampToScreen(desiredScreenPosition, _screenPaddingPixels);

            Canvas visualCanvas = visualRoot.GetComponentInParent<Canvas>();
            Camera visualCamera = visualCanvas != null && visualCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? visualCanvas.worldCamera
                : null;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    visualRoot,
                    clampedScreenPosition,
                    visualCamera,
                    out Vector2 localPosition))
            {
                Echo.Warning($"Cannot position tutorial pointer for target '{target.name}' because its screen position could not be converted.");
                return;
            }

            _pointerRect.anchoredPosition = localPosition;
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

        private void StartPulse()
        {
            StopPulse();
            _pulseTween = _pointerRect
                .DOScale(_pulseScale, _pulseDurationSeconds)
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

            _pointerRect.localScale = Vector3.one;
        }
    }
}
