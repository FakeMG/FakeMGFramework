using DG.Tweening;
using UnityEngine;

namespace FakeMG.Framework.UI.Popup
{
    public class PopupSlideAnimator : PopupAnimator
    {
        [Header("Target UI")]
        [SerializeField] protected RectTransform _canvasRect;
        [SerializeField] private RectTransform _rectTransform;

        [Header("Animation Settings")]
        [SerializeField] private float _animationDuration = 0.3f;
        [SerializeField] private Vector3 _targetPosition = Vector3.zero;
        [SerializeField] private Ease _showEase = Ease.OutBack;
        [SerializeField] private Ease _hideEase = Ease.InBack;
        [SerializeField] private Direction _slideDirection = Direction.Bottom;

        public enum Direction
        {
            Top,
            Right,
            Bottom,
            Left
        }

        private Vector3 _hiddenPosition;

        private void Start()
        {
            _hiddenPosition = CalculateHiddenPosition();
        }

        private Vector3 CalculateHiddenPosition()
        {
            Vector3 hidden = _targetPosition;

            Vector2 canvasSize = _canvasRect.rect.size;

            switch (_slideDirection)
            {
                case Direction.Top:
                    hidden.y = _targetPosition.y + canvasSize.y + _rectTransform.rect.height;
                    break;
                case Direction.Right:
                    hidden.x = _targetPosition.x + canvasSize.x + _rectTransform.rect.width;
                    break;
                case Direction.Bottom:
                    hidden.y = _targetPosition.y - canvasSize.y - _rectTransform.rect.height;
                    break;
                case Direction.Left:
                    hidden.x = _targetPosition.x - canvasSize.x - _rectTransform.rect.width;
                    break;
            }

            return hidden;
        }

        protected override Sequence CreateShowSequence()
        {
            var sequence = DOTween.Sequence();
            sequence.Append(_canvasGroup.transform.DOLocalMove(_targetPosition, _animationDuration)
                .SetEase(_showEase)
                .SetLink(_rectTransform.gameObject));
            sequence.Join(_canvasGroup.DOFade(1f, _animationDuration).SetLink(_canvasGroup.gameObject));

            return sequence;
        }

        protected override Sequence CreateHideSequence()
        {
            var sequence = DOTween.Sequence();
            sequence.Append(_canvasGroup.transform.DOLocalMove(_hiddenPosition, _animationDuration)
                .SetEase(_hideEase)
                .SetLink(_rectTransform.gameObject));
            sequence.Join(_canvasGroup.DOFade(0f, _animationDuration)
                .SetLink(_canvasGroup.gameObject)
                .SetDelay(_animationDuration * 0.5f));
            return sequence;
        }

        protected override void ShowImmediate()
        {
            _canvasGroup.transform.localPosition = _targetPosition;
            _canvasGroup.alpha = 1f;
        }

        protected override void HideImmediate()
        {
            _canvasGroup.transform.localPosition = _hiddenPosition;
            _canvasGroup.alpha = 0f;
        }
    }
}