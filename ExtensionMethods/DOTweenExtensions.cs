using DG.Tweening;
using UnityEngine;

namespace FakeMG.FakeMGFramework.ExtensionMethods
{
    public static class DOTweenUIExtensions
    {
        public static Sequence DOScaleMoveSequence(this Transform transform,
            Vector3 endScale, Vector3 endPosition,
            float duration,
            Ease scaleEase = Ease.OutBack,
            Ease moveEase = Ease.InOutQuart)
        {
            //TODO: how to kill previous animation sequence? transform.DOKill() doesn't work because transform is in a Sequence
            Sequence animationSequence = DOTween.Sequence();
        
            animationSequence.Append(transform.DOScale(endScale, duration * 0.3f).SetEase(scaleEase)
                .SetLink(transform.gameObject));
            animationSequence.Append(transform.DOMove(endPosition, duration * 0.7f).SetEase(moveEase)
                .SetLink(transform.gameObject));

            return animationSequence;
        }

        public static Sequence DOScaleMoveUpFadeOutSequence(this CanvasGroup canvasGroup,
            Vector3 endScale, float moveScreenPercentage,
            float duration,
            Ease scaleEase = Ease.OutBack,
            Ease moveEase = Ease.InOutQuart,
            Ease fadeEase = Ease.InQuart)
        {
            var initialPosition = canvasGroup.transform.position;

            Sequence animationSequence = DOTween.Sequence();
        
            // Scale up animation
            animationSequence.Join(
                canvasGroup.transform.DOScale(endScale, duration * 0.15f).SetEase(scaleEase)
            );
        
            // Fly up animation (starts during scale up)
            // Use screen percentage instead of fixed height
            float adaptiveFlyHeight = moveScreenPercentage;
            var canvas = canvasGroup.GetComponentInParent<Canvas>();
            if (canvas && canvas.renderMode != RenderMode.WorldSpace)
            {
                var rectTransform = canvas.GetComponent<RectTransform>();
                if (rectTransform)
                {
                    adaptiveFlyHeight = rectTransform.rect.height * rectTransform.localScale.y * moveScreenPercentage;
                }
            }
            animationSequence.Join(
                canvasGroup.transform.DOMoveY(initialPosition.y + adaptiveFlyHeight, duration).SetEase(moveEase)
            );
        
            // Fade out animation (starts after scale up is halfway done)
            animationSequence.Insert(duration * 0.3f,
                canvasGroup.DOFade(0f, duration * 0.4f).SetEase(fadeEase)
            );

            return animationSequence;
        }
    }
}