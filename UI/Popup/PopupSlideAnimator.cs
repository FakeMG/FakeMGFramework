using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FakeMG.FakeMGFramework.UI.Popup
{
    public class PopupSlideAnimator : PopupAnimator
    {
        [Header("Target UI")]
        [SerializeField] protected RectTransform canvasRect;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform rectTransform;

        [Header("Animation Settings")]
        [SerializeField] private float animationDuration = 0.3f;
        [SerializeField] private Vector3 targetPosition = Vector3.zero;
        [SerializeField] private Ease showEase = Ease.OutBack;
        [SerializeField] private Ease hideEase = Ease.InBack;
        [SerializeField] private Direction slideDirection = Direction.Bottom;

        public enum Direction
        {
            Top,
            Right,
            Bottom,
            Left
        }

        private Vector3 _hiddenPosition;
        private bool _isShowing;

        private void Start()
        {
            _hiddenPosition = CalculateHiddenPosition();
            HideImmediate();
        }

        private Vector3 CalculateHiddenPosition()
        {
            Vector3 hidden = targetPosition;

            Vector2 canvasSize = canvasRect.rect.size;

            switch (slideDirection)
            {
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
        public override void Show(bool animate = true)
        {
            if (_isShowing) return;

            CurrentSequence?.Kill();
            
            _isShowing = true;

            onShowStart?.Invoke();
            if (animate)
            {
                canvasGroup.gameObject.SetActive(true);
                CurrentSequence = DOTween.Sequence();
                CurrentSequence.Append(canvasGroup.transform.DOLocalMove(targetPosition, animationDuration).SetEase(showEase)
                    .SetLink(rectTransform.gameObject));
                CurrentSequence.Join(canvasGroup.DOFade(1f, animationDuration).SetLink(canvasGroup.gameObject));
                CurrentSequence.OnComplete(() => 
                {
                    CurrentSequence = null;
                    onShowFinished?.Invoke();
                });
            }
            else
            {
                ShowImmediate();
                onShowFinished?.Invoke();
            }
        }

        [Button]
        public override void Hide(bool animate = true)
        {
            if (!_isShowing) return;

            CurrentSequence?.Kill();
            
            _isShowing = false;

            onHideStart?.Invoke();
            if (animate)
            {
                CurrentSequence = DOTween.Sequence();
                CurrentSequence.Append(canvasGroup.transform.DOLocalMove(_hiddenPosition, animationDuration).SetEase(hideEase)
                    .SetLink(rectTransform.gameObject));
                CurrentSequence.Join(canvasGroup.DOFade(0f, animationDuration).SetLink(canvasGroup.gameObject)
                    .SetDelay(animationDuration * 0.5f));
                CurrentSequence.OnComplete(() =>
                {
                    CurrentSequence = null;
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

        private void ShowImmediate()
        {
            canvasGroup.gameObject.SetActive(true);
            canvasGroup.transform.localPosition = targetPosition;
            canvasGroup.alpha = 1f;
        }

        private void HideImmediate()
        {
            canvasGroup.transform.localPosition = _hiddenPosition;
            canvasGroup.alpha = 0f;
            canvasGroup.gameObject.SetActive(false);
        }
    }
}