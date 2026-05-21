using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

namespace FakeMG.Inventory.Hud
{
    public sealed class ItemCounterLifetimeScope : LifetimeScope
    {
        [SerializeField] private ItemCounterView _counterView;

        private ItemCounterRegistry _counterRegistry;
        private IInventoryBalanceRepository _inventoryRepository;
        private bool _isRegistered;

        #region Unity Lifecycle

        private void OnDisable()
        {
            UnregisterCounter();
        }

        protected override void OnDestroy()
        {
            UnregisterCounter();
            base.OnDestroy();
        }

        #endregion

        #region Protected Methods

        protected override void Configure(VContainer.IContainerBuilder builder)
        {
            builder.RegisterBuildCallback(RegisterCounterWhenContainerBuilds);
        }

        #endregion

        #region Private Methods

        private void RegisterCounterWhenContainerBuilds(VContainer.IObjectResolver resolver)
        {
            _counterRegistry = (ItemCounterRegistry)resolver.Resolve(typeof(ItemCounterRegistry));
            _inventoryRepository = (IInventoryBalanceRepository)resolver.Resolve(typeof(IInventoryBalanceRepository));
            RegisterCounterWhenReady();
        }

        private void RegisterCounterWhenReady()
        {
            if (!isActiveAndEnabled || _counterRegistry == null || _inventoryRepository == null || _isRegistered)
            {
                return;
            }

            _counterRegistry.Register(_counterView);
            _isRegistered = true;
            _counterView.InitializeAsync(_inventoryRepository).Forget();
        }

        private void UnregisterCounter()
        {
            if (!_isRegistered || _counterRegistry == null)
            {
                return;
            }

            _counterRegistry.Unregister(_counterView);
            _isRegistered = false;
        }

        #endregion
    }
}
