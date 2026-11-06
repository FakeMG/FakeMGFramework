using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Pulses a target's scale to draw attention to it for the duration of a step. The target is
    /// resolved every frame, so a target that appears later (e.g. inside a popup) starts pulsing as
    /// soon as it registers. The original scale is restored when the step ends or the target changes.
    /// </summary>
    [Serializable]
    public sealed class TargetPulseBehaviorModule : ITutorialModule
    {
        [SerializeField] private TutorialTargetKeySO _targetKey;
        [SerializeField, Min(1f)] private float _pulseScale = 1.12f;
        [SerializeField, Min(0.05f)] private float _halfPulseDurationSeconds = 0.4f;

        private CancellationTokenSource _trackingCts;
        private Transform _pulsedTransform;
        private Vector3 _originalScale;
        private Tween _pulseTween;

        public bool BlocksCompletion => false;
        public bool IsRequired => false;

        public UniTask<bool> ActivateAsync(TutorialContext context, CancellationToken cancellationToken)
        {
            _trackingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            TrackTargetAsync(context, _trackingCts.Token).Forget();
            return UniTask.FromResult(true);
        }

        public UniTask DeactivateAsync(CancellationToken cancellationToken)
        {
            _trackingCts?.Cancel();
            _trackingCts?.Dispose();
            _trackingCts = null;
            StopPulse();
            return UniTask.CompletedTask;
        }

        private async UniTaskVoid TrackTargetAsync(TutorialContext context, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Transform current = context.TargetRegistry.TryGet(_targetKey, out ITutorialTarget target) &&
                                    !target.IsDestroyed() && target.IsAvailable
                    ? target.InteractionTransform
                    : null;
                if (current != _pulsedTransform)
                {
                    StopPulse();
                    StartPulse(current);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken).SuppressCancellationThrow();
            }
        }

        private void StartPulse(Transform target)
        {
            if (target == null) return;
            _pulsedTransform = target;
            _originalScale = target.localScale;
            _pulseTween = target.DOScale(_originalScale * _pulseScale, _halfPulseDurationSeconds)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true)
                .SetLink(target.gameObject);
        }

        private void StopPulse()
        {
            _pulseTween?.Kill();
            _pulseTween = null;
            if (_pulsedTransform != null)
            {
                _pulsedTransform.localScale = _originalScale;
            }

            _pulsedTransform = null;
        }
    }
}
