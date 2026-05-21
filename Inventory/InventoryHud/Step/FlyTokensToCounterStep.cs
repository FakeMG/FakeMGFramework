using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework.UI.RewardFly;
using UnityEngine;

namespace FakeMG.Inventory.Hud
{
    public sealed class FlyTokensToCounterStep : IInventoryHudAnimationStep
    {
        private readonly ItemFlyRewardService _itemFlyRewardService;

        private InventoryChange _currentChange;
        private ItemCounterView _currentCounter;
        private int _arrivedTokenCount;
        private int _expectedTokenCount;

        public FlyTokensToCounterStep(ItemFlyRewardService itemFlyRewardService)
        {
            _itemFlyRewardService = itemFlyRewardService;
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
            _expectedTokenCount = _itemFlyRewardService.GetFlyTokenCount(countIncrease);

            var request = new RewardFlyRequest(
                change.IdentitySO,
                counter.RewardFlyTokenPrefab,
                countIncrease,
                rewardStartTransform,
                counter.FlyTargetTransform,
                RewardFlySpace.ScreenSpaceCanvas,
                IncreaseCounterWhenTokenArrives);

            await _itemFlyRewardService.PlayRewardFlyAsync(request, cancellationToken);
        }

        #endregion

        #region Private Methods

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
