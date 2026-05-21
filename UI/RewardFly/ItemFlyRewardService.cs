using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace FakeMG.Framework.UI.RewardFly
{
    public sealed class ItemFlyRewardService : MonoBehaviour
    {
        [SerializeField] private RectTransform _screenSpaceCanvasRectTransform;
        [SerializeField] private Transform _worldTokenParent;
        [SerializeField] private Camera _conversionCamera;
        [SerializeField] private int _maxFlyTokenCount = 12;
        [SerializeField] private float _tokenStaggerDelaySeconds = 0.05f;
        [SerializeField] private float _screenScatterRadiusPixels = 80f;
        [SerializeField] private float _worldScatterRadiusUnits = 1f;
        [SerializeField] private float _spawnScatterDurationSeconds = 0.18f;
        [SerializeField] private float _spawnScaleDurationSeconds = 0.16f;
        [SerializeField] private float _travelDurationSeconds = 0.45f;

        #region Public Methods

        public int GetFlyTokenCount(int rewardAmount)
        {
            return Mathf.Min(Mathf.Max(1, rewardAmount), _maxFlyTokenCount);
        }

        public async UniTask PlayRewardFlyAsync(RewardFlyRequest request, CancellationToken cancellationToken)
        {
            if (!IsValidRequest(request))
            {
                return;
            }

            AsyncOperationHandle<Sprite> spriteHandle = Addressables.LoadAssetAsync<Sprite>(request.IdentitySO.IconSpriteAsset);

            try
            {
                Sprite rewardTokenSprite = await spriteHandle.ToUniTask(cancellationToken: cancellationToken);
                if (spriteHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"{nameof(ItemFlyRewardService)} failed to load icon sprite for item '{request.IdentitySO.name}'.");
                    return;
                }

                int tokenCount = GetFlyTokenCount(request.Amount);
                var tokenTasks = new List<UniTask>(tokenCount);

                for (int tokenIndex = 0; tokenIndex < tokenCount; tokenIndex++)
                {
                    float startDelaySeconds = _tokenStaggerDelaySeconds * tokenIndex;
                    tokenTasks.Add(PlayRewardTokenAsync(
                        request,
                        rewardTokenSprite,
                        startDelaySeconds,
                        cancellationToken));
                }

                await UniTask.WhenAll(tokenTasks);
            }
            finally
            {
                if (spriteHandle.IsValid())
                {
                    Addressables.Release(spriteHandle);
                }
            }
        }

        #endregion

        #region Private Methods

        private async UniTask PlayRewardTokenAsync(
            RewardFlyRequest request,
            Sprite rewardTokenSprite,
            float startDelaySeconds,
            CancellationToken cancellationToken)
        {
            if (startDelaySeconds > 0f)
            {
                TimeSpan startDelay = TimeSpan.FromSeconds(startDelaySeconds);
                await UniTask.Delay(startDelay, cancellationToken: cancellationToken);
            }

            RewardFlyTokenView rewardFlyTokenView = SpawnRewardFlyToken(request, rewardTokenSprite);

            try
            {
                await PlaySpawnAndScatterAsync(request, rewardFlyTokenView);
                await PlayFlyToTargetAsync(request, rewardFlyTokenView);

                request.OnTokenArrived?.Invoke();
            }
            finally
            {
                rewardFlyTokenView.DisposeAfterLanding();
            }
        }

        private RewardFlyTokenView SpawnRewardFlyToken(RewardFlyRequest request, Sprite rewardTokenSprite)
        {
            Transform parent = GetParentForSpace(request.FlySpace);
            RewardFlyTokenView rewardFlyTokenView = Instantiate(request.TokenPrefab, parent);
            rewardFlyTokenView.SetRewardSprite(rewardTokenSprite);

            if (request.FlySpace == RewardFlySpace.ScreenSpaceCanvas)
            {
                Vector3 spawnLocalPositionPixels = ConvertTransformToCanvasLocalPosition(request.SourceTransform);
                rewardFlyTokenView.SetCanvasLocalPosition(spawnLocalPositionPixels);
                return rewardFlyTokenView;
            }

            rewardFlyTokenView.SetWorldPosition(request.SourceTransform.position);
            return rewardFlyTokenView;
        }

        private async UniTask PlaySpawnAndScatterAsync(RewardFlyRequest request, RewardFlyTokenView rewardFlyTokenView)
        {
            UniTask scatterTask = request.FlySpace == RewardFlySpace.ScreenSpaceCanvas
                ? rewardFlyTokenView.PlayCanvasScatterAsync(ComputeCanvasScatterOffsetPixels(), _spawnScatterDurationSeconds)
                : rewardFlyTokenView.PlayWorldScatterAsync(ComputeWorldScatterOffset(), _spawnScatterDurationSeconds);

            await UniTask.WhenAll(
                scatterTask,
                rewardFlyTokenView.PlaySpawnScaleAsync(_spawnScaleDurationSeconds));
        }

        private UniTask PlayFlyToTargetAsync(RewardFlyRequest request, RewardFlyTokenView rewardFlyTokenView)
        {
            if (request.FlySpace == RewardFlySpace.ScreenSpaceCanvas)
            {
                Vector3 targetLocalPositionPixels = ConvertTransformToCanvasLocalPosition(request.TargetTransform);
                return rewardFlyTokenView.PlayCanvasFlyToTargetAsync(targetLocalPositionPixels, _travelDurationSeconds);
            }

            return rewardFlyTokenView.PlayWorldFlyToTargetAsync(request.TargetTransform.position, _travelDurationSeconds);
        }

        private Transform GetParentForSpace(RewardFlySpace flySpace)
        {
            return flySpace == RewardFlySpace.ScreenSpaceCanvas
                ? _screenSpaceCanvasRectTransform
                : _worldTokenParent;
        }

        private Vector3 ComputeCanvasScatterOffsetPixels()
        {
            Vector2 scatterOffsetPixels = UnityEngine.Random.insideUnitCircle * _screenScatterRadiusPixels;
            return new Vector3(scatterOffsetPixels.x, scatterOffsetPixels.y, 0f);
        }

        private Vector3 ComputeWorldScatterOffset()
        {
            Vector2 scatterOffset = UnityEngine.Random.insideUnitCircle * _worldScatterRadiusUnits;
            return new Vector3(scatterOffset.x, scatterOffset.y, 0f);
        }

        private Vector3 ConvertTransformToCanvasLocalPosition(Transform sourceTransform)
        {
            Vector3 sourceWorldPosition = GetTransformCenterWorldPosition(sourceTransform);
            Camera conversionCamera = GetCanvasConversionCamera();
            Vector2 sourceScreenPoint = RectTransformUtility.WorldToScreenPoint(conversionCamera, sourceWorldPosition);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _screenSpaceCanvasRectTransform,
                sourceScreenPoint,
                conversionCamera,
                out Vector2 sourceLocalPoint);

            return new Vector3(sourceLocalPoint.x, sourceLocalPoint.y, 0f);
        }

        private static Vector3 GetTransformCenterWorldPosition(Transform sourceTransform)
        {
            if (sourceTransform is RectTransform rectTransform)
            {
                return rectTransform.TransformPoint(rectTransform.rect.center);
            }

            return sourceTransform.position;
        }

        private Camera GetCanvasConversionCamera()
        {
            Canvas canvas = _screenSpaceCanvasRectTransform.GetComponentInParent<Canvas>();
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return canvas.worldCamera ? canvas.worldCamera : _conversionCamera;
        }

        private bool IsValidRequest(RewardFlyRequest request)
        {
            if (!request.IdentitySO)
            {
                Debug.LogError($"{nameof(ItemFlyRewardService)} cannot fly reward tokens for a null identity.");
                return false;
            }

            if (!request.TokenPrefab)
            {
                Debug.LogError($"{nameof(ItemFlyRewardService)} cannot fly reward tokens for item '{request.IdentitySO.name}' because no token prefab was provided.");
                return false;
            }

            if (request.Amount <= 0)
            {
                Debug.LogWarning($"{nameof(ItemFlyRewardService)} received a non-positive reward amount: {request.Amount}.");
                return false;
            }

            if (!request.SourceTransform || !request.TargetTransform)
            {
                Debug.LogError($"{nameof(ItemFlyRewardService)} requires both source and target transforms for item '{request.IdentitySO.name}'.");
                return false;
            }

            if (request.FlySpace == RewardFlySpace.ScreenSpaceCanvas && !HasValidScreenSpaceCanvas(request))
            {
                return false;
            }

            if (request.IdentitySO.IconSpriteAsset == null || !request.IdentitySO.IconSpriteAsset.RuntimeKeyIsValid())
            {
                Debug.LogError($"{nameof(ItemFlyRewardService)} cannot load an invalid icon sprite for item '{request.IdentitySO.name}'.");
                return false;
            }

            return true;
        }

        private bool HasValidScreenSpaceCanvas(RewardFlyRequest request)
        {
            if (!_screenSpaceCanvasRectTransform)
            {
                Debug.LogError($"{nameof(ItemFlyRewardService)} cannot fly item '{request.IdentitySO.name}' in {nameof(RewardFlySpace.ScreenSpaceCanvas)} because no canvas parent is assigned.");
                return false;
            }

            if (!_screenSpaceCanvasRectTransform.GetComponentInParent<Canvas>())
            {
                Debug.LogError($"{nameof(ItemFlyRewardService)} cannot fly item '{request.IdentitySO.name}' in {nameof(RewardFlySpace.ScreenSpaceCanvas)} because the assigned parent is not under a Canvas.");
                return false;
            }

            return true;
        }

        #endregion
    }
}
