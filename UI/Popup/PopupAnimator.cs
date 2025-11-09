using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FakeMG.Framework.UI.Popup
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class PopupAnimator : MonoBehaviour
    {
        [Required]
        [SerializeField] protected CanvasGroup _canvasGroup;
        [Header("Debug")]
        [SerializeField] private bool _enableDebugging = false;

        public event Action OnShowStart;
        public event Action OnShowFinished;
        public event Action OnHideStart;
        public event Action OnHideFinished;

        public bool IsShowing { get; private set; } = true;
        private Sequence _showSequence;
        private Sequence _hideSequence;
        private Sequence _currentSequence;

        private CancellationTokenSource _animationCts;

        private void Reset()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnDestroy()
        {
            _animationCts?.Cancel();
            _animationCts?.Dispose();
            _showSequence?.Kill();
            _hideSequence?.Kill();
            _currentSequence?.Kill();
        }

        private void KillCurrentAnimation()
        {
            if (_currentSequence != null)
            {
                _currentSequence.Kill();
                _currentSequence.onKill = null;
                _currentSequence = null;
            }
        }

        [Button]
        public async UniTask Show(bool animate = true)
        {
            Echo.Log("Show called", _enableDebugging);
            if (IsShowing) return;

            _animationCts?.Cancel();
            _animationCts?.Dispose();
            _animationCts = new CancellationTokenSource();

            KillCurrentAnimation();
            Echo.Log("Finished completing current animation", _enableDebugging);

            IsShowing = true;

            OnShowStart?.Invoke();

            _canvasGroup.gameObject.SetActive(true);

            if (animate)
            {
                _currentSequence = GetShowSequence();
                _currentSequence.Restart();
                Echo.Log("Started show animation", _enableDebugging);

                bool wasCancelled = await _currentSequence.AsyncWaitForCompletion().AsUniTask()
                    .AttachExternalCancellation(_animationCts.Token)
                    .SuppressCancellationThrow();

                if (wasCancelled)
                {
                    Echo.Log("Show animation was cancelled", _enableDebugging);
                }
            }
            else
            {
                ShowImmediate();
            }

            Echo.Log("Show finished", _enableDebugging);

            OnShowFinished?.Invoke();
        }

        protected abstract void ShowImmediate();

        private Sequence GetShowSequence()
        {
            if (_showSequence.IsActive()) return _showSequence;

            _showSequence = CreateShowSequence();
            _showSequence.SetAutoKill(false);
            return _showSequence;
        }

        protected abstract Sequence CreateShowSequence();

        [Button]
        public async UniTask Hide(bool animate = true)
        {
            Echo.Log("Hide called", _enableDebugging);
            if (!IsShowing) return;

            _animationCts?.Cancel();
            _animationCts?.Dispose();
            _animationCts = new CancellationTokenSource();

            KillCurrentAnimation();
            Echo.Log("Finished completing current animation", _enableDebugging);

            IsShowing = false;

            OnHideStart?.Invoke();

            if (animate)
            {
                _currentSequence = GetHideSequence();
                _currentSequence.Restart();
                Echo.Log("Started hide animation", _enableDebugging);

                bool wasCancelled = await _currentSequence.AsyncWaitForCompletion().AsUniTask()
                    .AttachExternalCancellation(_animationCts.Token)
                    .SuppressCancellationThrow();

                if (wasCancelled)
                {
                    Echo.Log("Hide animation was cancelled", _enableDebugging);
                }
            }
            else
            {
                HideImmediate();
            }

            _canvasGroup.gameObject.SetActive(false);

            Echo.Log("Hide finished", _enableDebugging);

            OnHideFinished?.Invoke();
        }

        protected abstract void HideImmediate();

        private Sequence GetHideSequence()
        {
            if (_hideSequence.IsActive()) return _hideSequence;

            _hideSequence = CreateHideSequence();
            _hideSequence.SetAutoKill(false);
            return _hideSequence;
        }

        protected abstract Sequence CreateHideSequence();
    }
}