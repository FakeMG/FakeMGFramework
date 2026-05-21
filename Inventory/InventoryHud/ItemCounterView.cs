using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using FakeMG.Framework.UI;
using FakeMG.Framework.UI.RewardFly;
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
        [SerializeField] private RewardFlyTokenView _rewardFlyTokenPrefab;

        private int _displayedCount;

        public IdentitySO IdentitySO => _identitySO;
        public Transform FlyTargetTransform => _flyTargetTransform;
        public RewardFlyTokenView RewardFlyTokenPrefab => _rewardFlyTokenPrefab;
        public int DisplayedCount => _displayedCount;

        #region Public Methods

        public async UniTask InitializeAsync(IInventoryBalanceRepository inventoryRepository)
        {
            int currentCount = inventoryRepository.GetBalance(_identitySO);
            _displayedCount = currentCount;

            await _itemIconUiUpdater.UpdateUIAsync(_identitySO, currentCount);
        }

        public void PlayPop()
        {
            _pulseAnimator.PlayAdditivePulse();
        }

        public async UniTask AnimateDisplayedCountToAsync(int targetCount)
        {
            int fromCount = _displayedCount;
            _displayedCount = targetCount;

            await _countAnimator.AnimateAsync(
                fromCount,
                targetCount,
                ApplyDisplayedCount);
        }

        public void SetCountImmediately(int count)
        {
            _displayedCount = count;
            _itemIconUiUpdater.UpdateCount(count);
        }

        #endregion

        #region Private Methods

        private void ApplyDisplayedCount(int count)
        {
            _itemIconUiUpdater.UpdateCount(count);
        }

        #endregion
    }
}
