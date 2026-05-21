using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace FakeMG.Inventory.Hud
{
    // TODO: subscribing to inventory changes can't know which space the reward should fly in (world vs screen).
    // Need to have some ways of determining it.
    // Currently the space is hardcoded in FlyTokensToCounterStep, which is not ideal.
    public sealed class InventoryHudAnimationController : MonoBehaviour
    {
        [SerializeField] private Transform _defaultRewardStartTransform;

        private IInventoryBalanceRepository _inventoryRepository;
        private ItemCounterRegistry _counterRegistry;
        private InventoryHudAnimationSequence _animationSequence;
        private bool _isSubscribed;

        #region Unity Lifecycle

        private void OnEnable()
        {
            SubscribeWhenReady();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        #endregion

        #region Public Methods

        [Inject]
        public void Construct(
            IInventoryBalanceRepository inventoryRepository,
            ItemCounterRegistry counterRegistry,
            InventoryHudAnimationSequence animationSequence)
        {
            _inventoryRepository = inventoryRepository;
            _counterRegistry = counterRegistry;
            _animationSequence = animationSequence;
            SubscribeWhenReady();
        }

        #endregion

        #region Private Methods

        private void SubscribeWhenReady()
        {
            if (!isActiveAndEnabled || _inventoryRepository == null || _isSubscribed)
            {
                return;
            }

            _inventoryRepository.OnBalanceChanged += AnimateHudWhenInventoryChanges;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed || _inventoryRepository == null)
            {
                return;
            }

            _inventoryRepository.OnBalanceChanged -= AnimateHudWhenInventoryChanges;
            _isSubscribed = false;
        }

        private void AnimateHudWhenInventoryChanges(InventoryChange change)
        {
            RunChangeAnimationAsync(change).Forget();
        }

        private async UniTaskVoid RunChangeAnimationAsync(InventoryChange change)
        {
            if (!_counterRegistry.TryGetCounter(change.IdentitySO, out ItemCounterView counter))
            {
                string itemName = change.IdentitySO ? change.IdentitySO.name : "<null>";
                Debug.LogWarning($"{nameof(InventoryHudAnimationController)} has no visible HUD counter for item '{itemName}'.");
                return;
            }

            if (change.NewCount > change.OldCount)
            {
                await _animationSequence.PlayAsync(
                    change,
                    counter,
                    _defaultRewardStartTransform,
                    this.GetCancellationTokenOnDestroy());

                return;
            }

            counter.AnimateDisplayedCountToAsync(change.NewCount).Forget();
        }

        #endregion
    }
}
