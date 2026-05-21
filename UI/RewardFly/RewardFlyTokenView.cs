using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace FakeMG.Framework.UI.RewardFly
{
    public sealed class RewardFlyTokenView : MonoBehaviour
    {
        [SerializeField] private RectTransform _tokenRectTransform;
        [SerializeField] private Image _tokenIconImage;
        [SerializeField] private SpriteRenderer _tokenSpriteRenderer;

        private Transform TargetTransform => _tokenRectTransform ? _tokenRectTransform : transform;

        #region Public Methods

        public void SetRewardSprite(Sprite rewardSprite)
        {
            if (_tokenIconImage)
            {
                _tokenIconImage.sprite = rewardSprite;
            }

            if (_tokenSpriteRenderer)
            {
                _tokenSpriteRenderer.sprite = rewardSprite;
            }
        }

        public void SetCanvasLocalPosition(Vector3 localPosition)
        {
            TargetTransform.localPosition = localPosition;
        }

        public void SetWorldPosition(Vector3 worldPosition)
        {
            TargetTransform.position = worldPosition;
        }

        public UniTask PlaySpawnScaleAsync(float scaleDurationSeconds)
        {
            TargetTransform.localScale = Vector3.zero;
            Tween scaleTween = TargetTransform.DOScale(Vector3.one, scaleDurationSeconds)
                .SetEase(Ease.OutBack)
                .SetLink(gameObject);

            return scaleTween.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());
        }

        public UniTask PlayCanvasScatterAsync(Vector3 scatterOffsetPixels, float scatterDurationSeconds)
        {
            Vector3 scatterTargetLocalPositionPixels = TargetTransform.localPosition + scatterOffsetPixels;
            Tween scatterTween = TargetTransform.DOLocalMove(scatterTargetLocalPositionPixels, scatterDurationSeconds)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject);

            return scatterTween.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());
        }

        public UniTask PlayWorldScatterAsync(Vector3 scatterOffset, float scatterDurationSeconds)
        {
            Vector3 scatterTargetWorldPosition = TargetTransform.position + scatterOffset;
            Tween scatterTween = TargetTransform.DOMove(scatterTargetWorldPosition, scatterDurationSeconds)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject);

            return scatterTween.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());
        }

        public UniTask PlayCanvasFlyToTargetAsync(Vector3 targetLocalPositionPixels, float flyDurationSeconds)
        {
            Vector3 startLocalPositionPixels = TargetTransform.localPosition;
            Vector3 curveControlPointPixels = BuildCurveControlPoint(startLocalPositionPixels, targetLocalPositionPixels);

            Tween flyTween = DOTween.To(
                    () => 0f,
                    progress01 => TargetTransform.localPosition = EvaluateQuadraticBezier(
                        startLocalPositionPixels,
                        curveControlPointPixels,
                        targetLocalPositionPixels,
                        progress01),
                    1f,
                    flyDurationSeconds)
                .SetEase(Ease.InOutQuad)
                .SetLink(gameObject);

            return flyTween.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());
        }

        public UniTask PlayWorldFlyToTargetAsync(Vector3 targetWorldPosition, float flyDurationSeconds)
        {
            Vector3 startWorldPosition = TargetTransform.position;
            Vector3 curveControlPoint = BuildCurveControlPoint(startWorldPosition, targetWorldPosition);

            Tween flyTween = DOTween.To(
                    () => 0f,
                    progress01 => TargetTransform.position = EvaluateQuadraticBezier(
                        startWorldPosition,
                        curveControlPoint,
                        targetWorldPosition,
                        progress01),
                    1f,
                    flyDurationSeconds)
                .SetEase(Ease.InOutQuad)
                .SetLink(gameObject);

            return flyTween.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());
        }

        public void DisposeAfterLanding()
        {
            TargetTransform.DOKill();
            Destroy(gameObject);
        }

        #endregion

        #region Private Methods

        private static Vector3 BuildCurveControlPoint(Vector3 startPosition, Vector3 targetPosition)
        {
            Vector3 offset = targetPosition - startPosition;
            if (offset.sqrMagnitude <= Mathf.Epsilon)
            {
                return (startPosition + targetPosition) * 0.5f;
            }

            Vector3 direction = offset.normalized;
            Vector3 perpendicularDirection = new(-direction.y, direction.x, 0f);
            float distance = offset.magnitude;
            float perpendicularOffset = Random.Range(distance * 0.15f, distance * 0.35f);
            Vector3 midpoint = (startPosition + targetPosition) * 0.5f;
            return midpoint + perpendicularDirection * perpendicularOffset;
        }

        private static Vector3 EvaluateQuadraticBezier(
            Vector3 startPosition,
            Vector3 controlPosition,
            Vector3 targetPosition,
            float progress01)
        {
            float inverseProgress01 = 1f - progress01;
            return inverseProgress01 * inverseProgress01 * startPosition
                   + 2f * inverseProgress01 * progress01 * controlPosition
                   + progress01 * progress01 * targetPosition;
        }

        #endregion
    }
}
