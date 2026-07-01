using System;
using VContainer.Unity;

namespace FakeMG.Inventory.Hud
{
    public sealed class CounterUpdateInformerSubscriber : IStartable, IDisposable
    {
        private readonly IInventoryBalanceRepository _inventoryBalanceRepository;
        private readonly CounterUpdateInformer _counterUpdateInformer;

        public CounterUpdateInformerSubscriber(
            IInventoryBalanceRepository inventoryBalanceRepository,
            CounterUpdateInformer counterUpdateInformer)
        {
            _inventoryBalanceRepository = inventoryBalanceRepository;
            _counterUpdateInformer = counterUpdateInformer;
        }

        #region Unity Lifecycle (VContainer entry point)

        public void Start()
        {
            _inventoryBalanceRepository.OnBalanceChanged += InformCounterOfBalanceChange;
        }

        public void Dispose()
        {
            _inventoryBalanceRepository.OnBalanceChanged -= InformCounterOfBalanceChange;
        }

        #endregion

        #region Private Methods

        private void InformCounterOfBalanceChange(InventoryChange change)
        {
            _counterUpdateInformer.Inform(change.IdentitySO, change.NewCount);
        }

        #endregion
    }
}
