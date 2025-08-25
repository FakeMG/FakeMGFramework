using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FakeMG.FakeMGFramework.UI.Popup
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class PopupAnimator : MonoBehaviour
    {
        [Required]
        [SerializeField] protected CanvasGroup canvasGroup;

        public event Action OnShowStart;
        public event Action OnShowFinished;
        public event Action OnHideStart;
        public event Action OnHideFinished;

        public bool IsShowing { get; private set; } = true;
        private Sequence _showSequence;
        private Sequence _hideSequence;
        private Sequence _currentSequence;

        private void OnDestroy()
        {
            _showSequence?.Kill();
            _hideSequence?.Kill();
            _currentSequence?.Kill();
        }

        private void CompleteCurrentAnimation()
        {
            if (_currentSequence != null)
            {
                _currentSequence.Complete();
                _currentSequence.onComplete = null;
                _currentSequence = null;
            }
        }

        [Button]
        public void Show(bool animate = true)
        {
            if (IsShowing) return;

            CompleteCurrentAnimation();

            IsShowing = true;

            OnShowStart?.Invoke();

            if (animate)
            {
                canvasGroup.gameObject.SetActive(true);
                _currentSequence = GetShowSequence();
                _currentSequence.Restart();
                _currentSequence.onComplete += () => { OnShowFinished?.Invoke(); };
            }
            else
            {
                ShowImmediate();
                OnShowFinished?.Invoke();
            }
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
        public void Hide(bool animate = true)
        {
            if (!IsShowing) return;

            CompleteCurrentAnimation();

            IsShowing = false;

            OnHideStart?.Invoke();

            if (animate)
            {
                _currentSequence = GetHideSequence();
                _currentSequence.Restart();
                _currentSequence.OnComplete(() =>
                {
                    canvasGroup.gameObject.SetActive(false);
                    OnHideFinished?.Invoke();
                });
            }
            else
            {
                HideImmediate();
                OnHideFinished?.Invoke();
            }
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