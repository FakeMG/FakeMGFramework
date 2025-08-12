using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace FakeMG.FakeMGFramework.UI.Popup
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class PopupAnimator : MonoBehaviour
    {
        [Required]
        [SerializeField] private PopupManager popupManager;
        [SerializeField] protected CanvasGroup canvasGroup;

        [Header("Events")]
        [SerializeField] public UnityEvent onShowStart;
        [SerializeField] public UnityEvent onShowFinished;
        [SerializeField] public UnityEvent onHideStart;
        [SerializeField] public UnityEvent onHideFinished;

        private Sequence _currentSequence;
        public bool IsShowing { get; private set; } = true;

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

            onShowStart?.Invoke();

            if (animate)
            {
                canvasGroup.gameObject.SetActive(true);
                _currentSequence = GetShowSequence();
                _currentSequence.onComplete += () =>
                {
                    _currentSequence = null;
                    onShowFinished?.Invoke();
                };
            }
            else
            {
                ShowImmediate();
                onShowFinished?.Invoke();
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

            onHideStart?.Invoke();

            if (animate)
            {
                _currentSequence = GetHideSequence();
                _currentSequence.OnComplete(() =>
                {
                    _currentSequence = null;
                    canvasGroup.gameObject.SetActive(false);
                    onHideFinished?.Invoke();
                });
            }
            else
            {
                HideImmediate();
                onHideFinished?.Invoke();
            }
        }

        protected abstract void HideImmediate();
        protected abstract Sequence GetHideSequence();
    }
}