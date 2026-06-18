using System.Numerics;
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

        private BigInteger _displayedCount;
        private IInventoryBalanceRepository _inventoryRepository;
        private bool _isAnimatingManually;

        public IdentitySO IdentitySO => _identitySO;
        public Transform FlyTargetTransform => _flyTargetTransform;
        public RewardFlyTokenView RewardFlyTokenPrefab => _rewardFlyTokenPrefab;
        public BigInteger DisplayedCount => _displayedCount;

        #region Unity Lifecycle

        private void OnDestroy()
        {
            if (_inventoryRepository != null)
            {
                _inventoryRepository.OnBalanceChanged -= SyncDisplayWhenBalanceDrops;
            }
        }

        #endregion

        #region Public Methods

        public async UniTask InitializeAsync(IInventoryBalanceRepository inventoryRepository)
        {
            _inventoryRepository = inventoryRepository;
            _inventoryRepository.OnBalanceChanged -= SyncDisplayWhenBalanceDrops;
            _inventoryRepository.OnBalanceChanged += SyncDisplayWhenBalanceDrops;

            BigInteger currentCount = inventoryRepository.GetBalance(_identitySO);
            _displayedCount = currentCount;

            await _itemIconUiUpdater.UpdateUIAsync(_identitySO, currentCount);
        }

        public void PlayPop()
        {
            _pulseAnimator.PlayAdditivePulse();
        }

        public async UniTask AnimateDisplayedCountToAsync(BigInteger targetCount)
        {
            BigInteger fromCount = _displayedCount;
            _displayedCount = targetCount;

            _isAnimatingManually = true;
            try
            {
                await _countAnimator.AnimateAsync(
                    fromCount,
                    targetCount,
                    ApplyDisplayedCount);
            }
            finally
            {
                _isAnimatingManually = false;
            }
        }

        public void SetCountImmediately(BigInteger count)
        {
            _displayedCount = count;
            _itemIconUiUpdater.UpdateCount(count);
        }

        #endregion

        #region Private Methods

        // Reward gains stay driven by the manual reward path; this only reconciles drops that bypass it
        // (spending, rebirth resets, etc.) so the counter never shows a stale, too-high value.
        private void SyncDisplayWhenBalanceDrops(InventoryChange change)
        {
            if (change.IdentitySO != _identitySO || _isAnimatingManually)
            {
                return;
            }

            if (change.NewCount < _displayedCount)
            {
                SetCountImmediately(change.NewCount);
            }
        }

        private void ApplyDisplayedCount(BigInteger count)
        {
            _itemIconUiUpdater.UpdateCount(count);
        }

        #endregion
    }
}
