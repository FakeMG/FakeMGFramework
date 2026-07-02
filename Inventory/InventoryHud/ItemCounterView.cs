using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using FakeMG.Framework.UI;
using FakeMG.Framework.UI.RewardFly;
using FakeMG.Numbers;
using UnityEngine;

namespace FakeMG.Inventory.Hud
{
    public sealed class ItemCounterView : MonoBehaviour
    {
        [SerializeField] private IdentitySO _identitySO;
        [SerializeField] private Transform _flyTargetTransform;
        [SerializeField] private ItemIconUIUpdater _itemIconUiUpdater;
        [SerializeField] private HudAdditivePulseAnimator _pulseAnimator;
        [SerializeField] private CounterCountAnimator _countAnimator;
        [SerializeField] private RewardTokenView _rewardFlyTokenPrefab;
        [SerializeField] private CounterUpdateDelayGroupSO _delayGroupSO;

        private GameNumber _displayedCount;

        public IdentitySO IdentitySO => _identitySO;
        public Transform FlyTargetTransform => _flyTargetTransform;
        public RewardTokenView RewardFlyTokenPrefab => _rewardFlyTokenPrefab;
        public GameNumber DisplayedCount => _displayedCount;
        public CounterUpdateDelayGroupSO DelayGroupSO => _delayGroupSO;

        #region Public Methods

        public async UniTask InitializeAsync(IInventoryBalanceRepository inventoryRepository)
        {
            GameNumber currentCount = inventoryRepository.GetBalance(_identitySO);
            _displayedCount = currentCount;

            await _itemIconUiUpdater.UpdateUIAsync(_identitySO, currentCount);
        }

        public void PlayPop()
        {
            _pulseAnimator.PlayAdditivePulse();
        }

        public async UniTask AnimateDisplayedCountToAsync(GameNumber targetCount)
        {
            GameNumber fromCount = _displayedCount;
            _displayedCount = targetCount;

            await _countAnimator.AnimateAsync(
                fromCount,
                targetCount,
                ApplyDisplayedCount);
        }

        public void SetCountImmediately(GameNumber count)
        {
            _displayedCount = count;
            _itemIconUiUpdater.UpdateCount(count);
        }

        #endregion

        #region Private Methods

        private void ApplyDisplayedCount(GameNumber count)
        {
            _itemIconUiUpdater.UpdateCount(count);
        }

        #endregion
    }
}
