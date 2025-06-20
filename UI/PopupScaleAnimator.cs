using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace FakeMG.FakeMGFramework.UI {
    public class PopupScaleAnimator : MonoBehaviour {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float animationDuration = 0.3f;
        [SerializeField] private Ease showEase = Ease.OutBack;
        [SerializeField] private Ease hideEase = Ease.InBack;
        [SerializeField] private Vector3 targetScale = Vector3.one;
        
        [Header("Events")]
        [SerializeField] private UnityEvent OnStart;
        [SerializeField] private UnityEvent OnFinished;
        
        private readonly Vector3 _initialScale = Vector3.zero;
        private bool _isShowing;

        private void Start() {
            canvasGroup.alpha = 0f;
            canvasGroup.transform.localScale = _initialScale;
        }

        [Button]
        public void Show(bool animate = true) {
            if (_isShowing) return;
            _isShowing = true;
            
            OnStart?.Invoke();

            if (animate) {
                canvasGroup.gameObject.SetActive(true);
                canvasGroup.DOFade(1f, animationDuration).SetLink(canvasGroup.gameObject);
                canvasGroup.transform.DOScale(targetScale, animationDuration)
                    .SetEase(showEase)
                    .SetLink(canvasGroup.gameObject)
                    .OnComplete(() => OnFinished?.Invoke());
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
                canvasGroup.DOFade(0f, animationDuration).SetLink(canvasGroup.gameObject).SetDelay(animationDuration * 0.5f);
                canvasGroup.transform.DOScale(_initialScale, animationDuration)
                    .SetEase(hideEase)
                    .SetLink(canvasGroup.gameObject)
                    .OnComplete(() => {
                        canvasGroup.gameObject.SetActive(false);
                        OnFinished?.Invoke();
                    });
            } else {
                HideImmediate();
                OnFinished?.Invoke();
            }
        }
        
        private void ShowImmediate() {
            canvasGroup.gameObject.SetActive(true);
            canvasGroup.alpha = 1f;
            canvasGroup.transform.localScale = targetScale;
        }
        
        private void HideImmediate() {
            canvasGroup.alpha = 0f;
            canvasGroup.transform.localScale = _initialScale;
            canvasGroup.gameObject.SetActive(false);
        }
    }
}