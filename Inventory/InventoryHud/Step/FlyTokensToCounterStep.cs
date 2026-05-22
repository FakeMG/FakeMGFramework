using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using FakeMG.Framework.UI.RewardFly;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace FakeMG.Inventory.Hud
{
    public sealed class FlyTokensToCounterStep : IInventoryHudAnimationStep
    {
        private const int MAX_FLY_TOKEN_COUNT = 12;
        private const float TOKEN_STAGGER_DELAY_SECONDS = 0.05f;
        private const float SCREEN_SCATTER_RADIUS_PIXELS = 80f;
        private const float SPAWN_SCATTER_DURATION_SECONDS = 0.18f;
        private const float SPAWN_SCALE_DURATION_SECONDS = 0.16f;
        private const float TRAVEL_DURATION_SECONDS = 0.45f;
        private static readonly AnimationCurve TRAVEL_SPEED_CURVE = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private readonly ItemFlyRewardService _itemFlyRewardService;
        private readonly RewardFlyScatterAnimator _scatterAnimator;

        private InventoryChange _currentChange;
        private ItemCounterView _currentCounter;
        private int _arrivedTokenCount;
        private int _expectedTokenCount;

        public FlyTokensToCounterStep(ItemFlyRewardService itemFlyRewardService)
        {
            _itemFlyRewardService = itemFlyRewardService;
            _scatterAnimator = new RewardFlyScatterAnimator(
                SCREEN_SCATTER_RADIUS_PIXELS,
                0f,
                SPAWN_SCATTER_DURATION_SECONDS);
        }

        #region Public Methods

        public async UniTask PlayAsync(
            InventoryChange change,
            ItemCounterView counter,
            Transform rewardStartTransform,
            CancellationToken cancellationToken)
        {
            int countIncrease = change.NewCount - change.OldCount;
            if (countIncrease <= 0)
            {
                string itemName = change.IdentitySO ? change.IdentitySO.name : "<null>";
                Debug.LogWarning($"{nameof(FlyTokensToCounterStep)} received a non-positive inventory increase for item '{itemName}'.");
                return;
            }

            _currentChange = change;
            _currentCounter = counter;
            _arrivedTokenCount = 0;
            _expectedTokenCount = GetFlyTokenCount(countIncrease);

            await PlayRewardTokensAsync(
                change.IdentitySO,
                counter,
                countIncrease,
                rewardStartTransform,
                cancellationToken);
        }

        #endregion

        #region Private Methods

        private async UniTask PlayRewardTokensAsync(
            IdentitySO identitySO,
            ItemCounterView counter,
            int amount,
            Transform rewardStartTransform,
            CancellationToken cancellationToken)
        {
            if (!IsValidRequest(identitySO, counter, rewardStartTransform))
            {
                return;
            }

            AsyncOperationHandle<Sprite> spriteHandle = Addressables.LoadAssetAsync<Sprite>(identitySO.IconSpriteAsset);

            try
            {
                Sprite rewardTokenSprite = await spriteHandle.ToUniTask(cancellationToken: cancellationToken);
                if (spriteHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"{nameof(FlyTokensToCounterStep)} failed to load icon sprite for item '{identitySO.name}'.");
                    return;
                }

                int tokenCount = GetFlyTokenCount(amount);
                var tokenTasks = new List<UniTask>(tokenCount);

                for (int tokenIndex = 0; tokenIndex < tokenCount; tokenIndex++)
                {
                    float startDelaySeconds = TOKEN_STAGGER_DELAY_SECONDS * tokenIndex;
                    tokenTasks.Add(PlayRewardTokenAsync(
                        identitySO,
                        counter,
                        rewardStartTransform,
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

        private async UniTask PlayRewardTokenAsync(
            IdentitySO identitySO,
            ItemCounterView counter,
            Transform rewardStartTransform,
            Sprite rewardTokenSprite,
            float startDelaySeconds,
            CancellationToken cancellationToken)
        {
            if (startDelaySeconds > 0f)
            {
                TimeSpan startDelay = TimeSpan.FromSeconds(startDelaySeconds);
                await UniTask.Delay(startDelay, cancellationToken: cancellationToken);
            }

            RewardFlyTokenView rewardFlyTokenView = SpawnRewardFlyToken(
                counter,
                rewardStartTransform,
                rewardTokenSprite);

            try
            {
                await PlaySpawnAndScatterAsync(rewardFlyTokenView);
                await PlayFlyToCounterAsync(identitySO, counter, rewardFlyTokenView, cancellationToken);
                IncreaseCounterWhenTokenArrives();
            }
            finally
            {
                rewardFlyTokenView.Dispose();
            }
        }

        private RewardFlyTokenView SpawnRewardFlyToken(
            ItemCounterView counter,
            Transform rewardStartTransform,
            Sprite rewardTokenSprite)
        {
            RewardFlyTokenView rewardFlyTokenView = UnityEngine.Object.Instantiate(
                counter.RewardFlyTokenPrefab,
                _itemFlyRewardService.ScreenSpaceCanvasRectTransform);

            rewardFlyTokenView.SetRewardSprite(rewardTokenSprite);
            rewardFlyTokenView.SetCanvasLocalPosition(
                _itemFlyRewardService.ConvertTransformToCanvasLocalPosition(rewardStartTransform));

            return rewardFlyTokenView;
        }

        private async UniTask PlaySpawnAndScatterAsync(RewardFlyTokenView rewardFlyTokenView)
        {
            await UniTask.WhenAll(
                _scatterAnimator.PlayCanvasScatterAsync(rewardFlyTokenView),
                rewardFlyTokenView.PlaySpawnScaleAsync(SPAWN_SCALE_DURATION_SECONDS));
        }

        private UniTask PlayFlyToCounterAsync(
            IdentitySO identitySO,
            ItemCounterView counter,
            RewardFlyTokenView rewardFlyTokenView,
            CancellationToken cancellationToken)
        {
            if (rewardFlyTokenView.FlyTransform is not RectTransform flyingRectTransform)
            {
                Debug.LogError($"{nameof(FlyTokensToCounterStep)} cannot fly reward token for item '{identitySO.name}' because the token fly transform is not a {nameof(RectTransform)}.");
                return UniTask.CompletedTask;
            }

            if (counter.FlyTargetTransform is not RectTransform targetRectTransform)
            {
                Debug.LogError($"{nameof(FlyTokensToCounterStep)} cannot fly reward token for item '{identitySO.name}' because the counter target is not a {nameof(RectTransform)}.");
                return UniTask.CompletedTask;
            }

            return _itemFlyRewardService.PlayUiTransformToUiAsync(
                flyingRectTransform,
                targetRectTransform,
                TRAVEL_DURATION_SECONDS,
                TRAVEL_SPEED_CURVE,
                0f,
                Vector2.up,
                cancellationToken);
        }

        private int GetFlyTokenCount(int rewardAmount)
        {
            return Mathf.Min(Mathf.Max(1, rewardAmount), MAX_FLY_TOKEN_COUNT);
        }

        private bool IsValidRequest(
            IdentitySO identitySO,
            ItemCounterView counter,
            Transform rewardStartTransform)
        {
            if (!identitySO)
            {
                Debug.LogError($"{nameof(FlyTokensToCounterStep)} cannot fly reward tokens for a null identity.");
                return false;
            }

            if (!counter.RewardFlyTokenPrefab)
            {
                Debug.LogError($"{nameof(FlyTokensToCounterStep)} cannot fly reward tokens for item '{identitySO.name}' because no token prefab was provided.");
                return false;
            }

            if (!rewardStartTransform || !counter.FlyTargetTransform)
            {
                Debug.LogError($"{nameof(FlyTokensToCounterStep)} requires both source and target transforms for item '{identitySO.name}'.");
                return false;
            }

            if (!_itemFlyRewardService.ScreenSpaceCanvasRectTransform)
            {
                Debug.LogError($"{nameof(FlyTokensToCounterStep)} cannot fly item '{identitySO.name}' because no screen space canvas parent is assigned.");
                return false;
            }

            if (!_itemFlyRewardService.ScreenSpaceCanvasRectTransform.GetComponentInParent<Canvas>())
            {
                Debug.LogError($"{nameof(FlyTokensToCounterStep)} cannot fly item '{identitySO.name}' because the assigned parent is not under a Canvas.");
                return false;
            }

            if (identitySO.IconSpriteAsset == null || !identitySO.IconSpriteAsset.RuntimeKeyIsValid())
            {
                Debug.LogError($"{nameof(FlyTokensToCounterStep)} cannot load an invalid icon sprite for item '{identitySO.name}'.");
                return false;
            }

            return true;
        }

        private void IncreaseCounterWhenTokenArrives()
        {
            _arrivedTokenCount++;

            float arrivalProgress01 = Mathf.Clamp01((float)_arrivedTokenCount / _expectedTokenCount);
            int nextDisplayedCount = Mathf.RoundToInt(Mathf.Lerp(
                _currentChange.OldCount,
                _currentChange.NewCount,
                arrivalProgress01));

            if (nextDisplayedCount <= _currentCounter.DisplayedCount && _currentCounter.DisplayedCount < _currentChange.NewCount)
            {
                nextDisplayedCount = _currentCounter.DisplayedCount + 1;
            }

            nextDisplayedCount = Mathf.Min(nextDisplayedCount, _currentChange.NewCount);
            _currentCounter.PlayPop();
            _currentCounter.AnimateDisplayedCountToAsync(nextDisplayedCount).Forget();
        }

        #endregion
    }
}
