using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using System;

namespace FakeMG.FakeMGFramework.UI.Popup
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class PopupAnimator : MonoBehaviour
    {
        [Required]
        [SerializeField] private PopupManager popupManager;
        [SerializeField] protected CanvasGroup canvasGroup;

        public event Action OnShowStart;
        public event Action OnShowFinished;
        public event Action OnHideStart;
        public event Action OnHideFinished;

        public bool IsShowing { get; private set; } = true;
        private Sequence _currentSequence;

        [Button]
        public void Show(bool animate = true)
        {
            if (IsShowing) return;

            if (_currentSequence != null)
            {
                _currentSequence.Kill();
                _currentSequence.onComplete = null;
            }

            IsShowing = true;

            OnShowStart?.Invoke();

            if (animate)
            {
                canvasGroup.gameObject.SetActive(true);
                _currentSequence = GetShowSequence();
                _currentSequence.onComplete += () =>
                {
                    _currentSequence = null;
                    OnShowFinished?.Invoke();
                };
            }
            else
            {
                ShowImmediate();
                OnShowFinished?.Invoke();
            }
        }

        protected abstract void ShowImmediate();
        protected abstract Sequence GetShowSequence();

        [Button]
        public void Hide(bool animate = true)
        {
            if (!IsShowing) return;

            if (_currentSequence != null)
            {
                _currentSequence.Kill();
                _currentSequence.onComplete = null;
            }

            IsShowing = false;

            OnHideStart?.Invoke();

            if (animate)
            {
                _currentSequence = GetHideSequence();
                _currentSequence.OnComplete(() =>
                {
                    _currentSequence = null;
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
        protected abstract Sequence GetHideSequence();
    }
}