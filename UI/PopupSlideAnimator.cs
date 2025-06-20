using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace FakeMG.FakeMGFramework.UI {
    public class PopupSlideAnimator : MonoBehaviour {
        [Header("Root Canvas")]
        [SerializeField] private RectTransform canvasRect;
        
        [Header("Target UI")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform rectTransform;
        
        [Header("Animation Settings")]
        [SerializeField] private float animationDuration = 0.3f;
        [SerializeField] private Vector3 targetPosition = Vector3.zero;
        [SerializeField] private Ease showEase = Ease.OutBack;
        [SerializeField] private Ease hideEase = Ease.InBack;
        [SerializeField] private Direction slideDirection = Direction.Bottom;
        
        [Header("Events")]
        [SerializeField] private UnityEvent OnStart;
        [SerializeField] private UnityEvent OnFinished;
        
        public enum Direction {
            Top,
            Right,
            Bottom,
            Left
        }
        
        
        private Vector3 _hiddenPosition;
        private bool _isShowing;
        
        private void Start() {
            _hiddenPosition = CalculateHiddenPosition();
            HideImmediate();
        }
        
        private Vector3 CalculateHiddenPosition() {
            Vector3 hidden = targetPosition;
            
            Vector2 canvasSize = canvasRect.rect.size;
            
            switch (slideDirection) {
                case Direction.Top:
                    hidden.y = targetPosition.y + canvasSize.y + rectTransform.rect.height;
                    break;
                case Direction.Right:
                    hidden.x = targetPosition.x + canvasSize.x + rectTransform.rect.width;
                    break;
                case Direction.Bottom:
                    hidden.y = targetPosition.y - canvasSize.y - rectTransform.rect.height;
                    break;
                case Direction.Left:
                    hidden.x = targetPosition.x - canvasSize.x - rectTransform.rect.width;
                    break;
            }
            
            return hidden;
        }
        
        [Button]
        public void Show(bool animate = true) {
            if (_isShowing) return;
            _isShowing = true;
            
            OnStart?.Invoke();
            
            if (animate) {
                canvasGroup.gameObject.SetActive(true);
                canvasGroup.transform.DOLocalMove(targetPosition, animationDuration).SetEase(showEase).SetLink(rectTransform.gameObject)
                    .OnComplete(() => OnFinished?.Invoke());
                canvasGroup.DOFade(1f, animationDuration).SetLink(canvasGroup.gameObject);
            } else {
                ShowImmediate();
                OnFinished?.Invoke();
            }
        }
        
        [Button]
        public void Hide(bool animate = true) {
            if (!_isShowing) return;
            _isShowing = false;
            
            OnStart?.Invoke();
            
            if (animate) {
                canvasGroup.transform.DOLocalMove(_hiddenPosition, animationDuration).SetEase(hideEase).SetLink(rectTransform.gameObject)
                    .OnComplete(() => {
                        canvasGroup.gameObject.SetActive(false);
                        OnFinished?.Invoke();
                    });
                canvasGroup.DOFade(0f, animationDuration).SetLink(canvasGroup.gameObject).SetDelay(animationDuration * 0.5f);
            } else {
                HideImmediate();
                OnFinished?.Invoke();
            }
        }
        
        private void ShowImmediate() {
            canvasGroup.gameObject.SetActive(true);
            canvasGroup.transform.localPosition = targetPosition;
            canvasGroup.alpha = 1f;
        }
        
        private void HideImmediate() {
            canvasGroup.transform.localPosition = _hiddenPosition;
            canvasGroup.alpha = 0f;
            canvasGroup.gameObject.SetActive(false);
        }
    }
}
