using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace FakeMG.Framework.UI.RewardFly
{
    public sealed class RewardFlyScatterAnimator
    {
        private readonly float _screenScatterRadiusPixels;
        private readonly float _worldScatterRadiusMeters;
        private readonly float _scatterDurationSeconds;

        public RewardFlyScatterAnimator(
            float screenScatterRadiusPixels,
            float worldScatterRadiusMeters,
            float scatterDurationSeconds)
        {
            _screenScatterRadiusPixels = screenScatterRadiusPixels;
            _worldScatterRadiusMeters = worldScatterRadiusMeters;
            _scatterDurationSeconds = scatterDurationSeconds;
        }

        #region Public Methods

        public UniTask PlayCanvasScatterAsync(RewardTokenView rewardFlyTokenView)
        {
            Transform flyTransform = rewardFlyTokenView.FlyTransform;
            Vector3 scatterTargetLocalPositionPixels = flyTransform.localPosition + ComputeCanvasScatterOffsetPixels();
            Tween scatterTween = flyTransform.DOLocalMove(scatterTargetLocalPositionPixels, _scatterDurationSeconds)
                .SetEase(Ease.OutQuad)
                .SetLink(rewardFlyTokenView.gameObject);

            return scatterTween.ToUniTask(cancellationToken: rewardFlyTokenView.GetCancellationTokenOnDestroy());
        }

        public UniTask PlayWorldScatterAsync(RewardTokenView rewardFlyTokenView)
        {
            Transform flyTransform = rewardFlyTokenView.FlyTransform;
            Vector3 scatterTargetWorldPosition = flyTransform.position + ComputeWorldScatterOffsetMeters();
            Tween scatterTween = flyTransform.DOMove(scatterTargetWorldPosition, _scatterDurationSeconds)
                .SetEase(Ease.OutQuad)
                .SetLink(rewardFlyTokenView.gameObject);

            return scatterTween.ToUniTask(cancellationToken: rewardFlyTokenView.GetCancellationTokenOnDestroy());
        }

        #endregion

        #region Private Methods

        private Vector3 ComputeCanvasScatterOffsetPixels()
        {
            Vector2 scatterOffsetPixels = Random.insideUnitCircle * _screenScatterRadiusPixels;
            return new Vector3(scatterOffsetPixels.x, scatterOffsetPixels.y, 0f);
        }

        private Vector3 ComputeWorldScatterOffsetMeters()
        {
            Vector2 scatterOffsetMeters = Random.insideUnitCircle * _worldScatterRadiusMeters;
            return new Vector3(scatterOffsetMeters.x, scatterOffsetMeters.y, 0f);
        }

        #endregion
    }
}
