using FakeMG.Inventory;
using FakeMG.Shop.Config;
using FakeMG.Shop.RuntimeData;

namespace FakeMG.Shop.Services.Purchase
{
    public class ShopPurchaseFinalizationService
    {
        private readonly IInventoryBalanceRepository _inventoryBalanceRepository;
        private readonly ShopOwnershipStateSaveable _shopOwnershipStateSaveable;

        public ShopPurchaseFinalizationService(
            IInventoryBalanceRepository inventoryBalanceRepository,
            ShopOwnershipStateSaveable shopOwnershipStateSaveable)
        {
            _inventoryBalanceRepository = inventoryBalanceRepository;
            _shopOwnershipStateSaveable = shopOwnershipStateSaveable;
        }

        #region Public Methods

        public void FinalizeSuccessfulPurchase(ShopListingSO shopListingSO, ShopPurchaseResult shopPurchaseResult)
        {
            _inventoryBalanceRepository.Add(shopPurchaseResult.GrantedItems);

            if (shopListingSO.IsNonConsumable)
            {
                _shopOwnershipStateSaveable.MarkOwned(shopListingSO.Id);
            }
        }

        #endregion
    }
}
