using DG.Tweening;
using UnityEngine;

namespace FakeMG.Framework.UI.Popup
{
    public class PopupSlideAnimator : PopupAnimator
    {
        [Header("Target UI")]
        [SerializeField] protected RectTransform canvasRect;
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

        private void Start()
        {
            _hiddenPosition = CalculateHiddenPosition();
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

        protected override Sequence CreateShowSequence()
        {
            var sequence = DOTween.Sequence();
            sequence.Append(canvasGroup.transform.DOLocalMove(targetPosition, animationDuration)
                .SetEase(showEase)
                .SetLink(rectTransform.gameObject));
            sequence.Join(canvasGroup.DOFade(1f, animationDuration).SetLink(canvasGroup.gameObject));

            return sequence;
        }

        protected override Sequence CreateHideSequence()
        {
            var sequence = DOTween.Sequence();
            sequence.Append(canvasGroup.transform.DOLocalMove(_hiddenPosition, animationDuration)
                .SetEase(hideEase)
                .SetLink(rectTransform.gameObject));
            sequence.Join(canvasGroup.DOFade(0f, animationDuration)
                .SetLink(canvasGroup.gameObject)
                .SetDelay(animationDuration * 0.5f));
            return sequence;
        }

        protected override void ShowImmediate()
        {
            canvasGroup.gameObject.SetActive(true);
            canvasGroup.transform.localPosition = targetPosition;
            canvasGroup.alpha = 1f;
        }

        protected override void HideImmediate()
        {
            canvasGroup.transform.localPosition = _hiddenPosition;
            canvasGroup.alpha = 0f;
            canvasGroup.gameObject.SetActive(false);
        }
    }
}