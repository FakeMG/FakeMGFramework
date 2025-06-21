using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FakeMG.FakeMGFramework.UI {
    public class PopupScaleAnimator : PopupAnimator {
        [Header("Target UI")]
        [SerializeField] private CanvasGroup canvasGroup;
        
        [Header("Animation Settings")]
        [SerializeField] private float animationDuration = 0.3f;
        [SerializeField] private Ease showEase = Ease.OutBack;
        [SerializeField] private Ease hideEase = Ease.InBack;
        [SerializeField] private Vector3 targetScale = Vector3.one;

        private readonly Vector3 _initialScale = Vector3.zero;
        private bool _isShowing;

        private void Start() {
            canvasGroup.alpha = 0f;
            canvasGroup.transform.localScale = _initialScale;
        }

        [Button]
        public override void Show(bool animate = true) {
            if (_isShowing) return;
            _isShowing = true;

            onShowStart?.Invoke();

            if (animate) {
                canvasGroup.gameObject.SetActive(true);
                var sequence = DOTween.Sequence();
                sequence.Join(canvasGroup.transform.DOScale(targetScale, animationDuration)
                    .SetEase(showEase)
                    .SetLink(canvasGroup.gameObject));
                sequence.Join(canvasGroup.DOFade(1f, animationDuration).SetLink(canvasGroup.gameObject));
                sequence.OnComplete(() => onShowFinished?.Invoke());
            } else {
                ShowImmediate();
                onShowFinished?.Invoke();
            }
        }

        [Button]
        public override void Hide(bool animate = true) {
            if (!_isShowing) return;
            _isShowing = false;

            onHideStart?.Invoke();

            if (animate) {
                var sequence = DOTween.Sequence();
                sequence.Join(canvasGroup.transform.DOScale(_initialScale, animationDuration)
                    .SetEase(hideEase)
                    .SetLink(canvasGroup.gameObject));
                sequence.Join(canvasGroup.DOFade(0f, animationDuration).SetLink(canvasGroup.gameObject)
                    .SetDelay(animationDuration * 0.5f));
                sequence.OnComplete(() => {
                    canvasGroup.gameObject.SetActive(false);
                    onHideFinished?.Invoke();
                });
            } else {
                HideImmediate();
                onHideFinished?.Invoke();
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