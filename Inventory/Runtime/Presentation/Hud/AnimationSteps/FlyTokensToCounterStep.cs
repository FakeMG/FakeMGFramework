using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using FakeMG.Framework.UI.RewardFly;
using FakeMG.Numbers;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Vector2 = UnityEngine.Vector2;

namespace FakeMG.Inventory.Hud
{
    public sealed class FlyTokensToCounterStep : IInventoryHudAnimationStep
    {
        private const int MAX_FLY_TOKEN_COUNT = 12;
        private const float TOKEN_STAGGER_DELAY_SECONDS = 0.05f;
        private const float SCREEN_SCATTER_RADIUS_PIXELS = 160f;
        private const float SPAWN_SCATTER_DURATION_SECONDS = 0.18f;
        private const float SPAWN_SCALE_DURATION_SECONDS = 0.16f;
        private const float TRAVEL_DURATION_SECONDS = 0.7f;
        private static readonly AnimationCurve TRAVEL_SPEED_CURVE = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private readonly ItemFlyRewardService _itemFlyRewardService;
        private readonly RewardFlyScatterAnimator _scatterAnimator;
        private readonly int _maxFlyTokenCount;

        private GameNumber _rewardAmount;
        private ItemCounterView _currentCounter;
        private int _arrivedTokenCount;
        private int _expectedTokenCount;
        private GameNumber _appliedRewardAmount;

        public FlyTokensToCounterStep(ItemFlyRewardService itemFlyRewardService, float feedbackIntensity01 = 1f)
        {
            _itemFlyRewardService = itemFlyRewardService;
            float intensity01 = Mathf.Clamp01(feedbackIntensity01);
            _maxFlyTokenCount = Mathf.Max(1, Mathf.CeilToInt(MAX_FLY_TOKEN_COUNT * intensity01));
            _scatterAnimator = new RewardFlyScatterAnimator(
                SCREEN_SCATTER_RADIUS_PIXELS * intensity01,
                0f,
                SPAWN_SCATTER_DURATION_SECONDS);
        }

        #region Public Methods

        public UniTask PlayAsync(
            InventoryChange change,
            ItemCounterView counter,
            Transform rewardStartTransform,
            CancellationToken cancellationToken)
        {
            Vector3 sourceCanvasPositionPixels = _itemFlyRewardService.ConvertTransformToCanvasLocalPosition(rewardStartTransform);
            return PlayAsync(change.IdentitySO, change.NewCount - change.OldCount, counter, sourceCanvasPositionPixels, cancellationToken);
        }

        public async UniTask PlayAsync(IdentitySO identitySO, GameNumber amount, ItemCounterView counter,
            Vector3 sourceCanvasPositionPixels, CancellationToken cancellationToken)
        {
            if (amount <= GameNumber.Zero)
            {
                string itemName = identitySO ? identitySO.name : "<null>";
                Debug.LogWarning($"{nameof(FlyTokensToCounterStep)} received a non-positive inventory increase for item '{itemName}'.");
                return;
            }

            _rewardAmount = amount;
            _currentCounter = counter;
            _arrivedTokenCount = 0;
            _expectedTokenCount = GetFlyTokenCount(amount);
            _appliedRewardAmount = GameNumber.Zero;

            await PlayRewardTokensAsync(
                identitySO,
                counter,
                amount,
                sourceCanvasPositionPixels,
                cancellationToken);
        }

        #endregion

        #region Private Methods

        private async UniTask PlayRewardTokensAsync(
            IdentitySO identitySO,
            ItemCounterView counter,
            GameNumber amount,
            Vector3 sourceCanvasPositionPixels,
            CancellationToken cancellationToken)
        {
            if (!IsValidRequest(identitySO, counter))
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
                        sourceCanvasPositionPixels,
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
            Vector3 sourceCanvasPositionPixels,
            Sprite rewardTokenSprite,
            float startDelaySeconds,
            CancellationToken cancellationToken)
        {
            if (startDelaySeconds > 0f)
            {
                TimeSpan startDelay = TimeSpan.FromSeconds(startDelaySeconds);
                await UniTask.Delay(startDelay, ignoreTimeScale: true, cancellationToken: cancellationToken);
            }

            RewardTokenView rewardFlyTokenView = SpawnRewardFlyToken(
                counter,
                sourceCanvasPositionPixels,
                rewardTokenSprite);

            try
            {
                await PlaySpawnAndScatterAsync(rewardFlyTokenView).AttachExternalCancellation(cancellationToken);
                await PlayFlyToCounterAsync(identitySO, counter, rewardFlyTokenView, cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                {
                    Echo.Log($"Reward token for '{identitySO.Id}' was canceled before arrival; its counter will use the granted balance.");
                    return;
                }
                IncreaseCounterWhenTokenArrives();
            }
            finally
            {
                rewardFlyTokenView.Dispose();
            }
        }

        private RewardTokenView SpawnRewardFlyToken(
            ItemCounterView counter,
            Vector3 sourceCanvasPositionPixels,
            Sprite rewardTokenSprite)
        {
            RewardTokenView rewardFlyTokenView = UnityEngine.Object.Instantiate(
                counter.RewardFlyTokenPrefab,
                _itemFlyRewardService.ScreenSpaceCanvasRectTransform);

            rewardFlyTokenView.SetRewardSprite(rewardTokenSprite);
            rewardFlyTokenView.SetCanvasLocalPosition(sourceCanvasPositionPixels);

            return rewardFlyTokenView;
        }

        private async UniTask PlaySpawnAndScatterAsync(RewardTokenView rewardFlyTokenView)
        {
            await UniTask.WhenAll(
                _scatterAnimator.PlayCanvasScatterAsync(rewardFlyTokenView),
                rewardFlyTokenView.PlaySpawnScaleAsync(SPAWN_SCALE_DURATION_SECONDS));
        }

        private UniTask PlayFlyToCounterAsync(
            IdentitySO identitySO,
            ItemCounterView counter,
            RewardTokenView rewardFlyTokenView,
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

        private int GetFlyTokenCount(GameNumber rewardAmount)
        {
            if (rewardAmount >= _maxFlyTokenCount)
            {
                return _maxFlyTokenCount;
            }

            return rewardAmount <= GameNumber.One ? 1 : (int)rewardAmount;
        }

        private bool IsValidRequest(
            IdentitySO identitySO,
            ItemCounterView counter)
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

            if (!counter.FlyTargetTransform)
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

            double arrivalProgress01 = Mathf.Clamp01((float)_arrivedTokenCount / _expectedTokenCount);
            GameNumber countIncrease = _rewardAmount;
            GameNumber arrivedRewardAmount = _arrivedTokenCount == _expectedTokenCount
                ? countIncrease
                : GameNumber.FromDouble(Math.Floor(countIncrease.ToDouble() * arrivalProgress01));
            GameNumber arrivedTokenAmount = arrivedRewardAmount - _appliedRewardAmount;
            _appliedRewardAmount = arrivedRewardAmount;
            GameNumber nextDisplayedCount = _currentCounter.DisplayedCount + arrivedTokenAmount;
            _currentCounter.PlayPop();
            _currentCounter.AnimateDisplayedCountToAsync(nextDisplayedCount).Forget();
        }

        #endregion
    }
}
