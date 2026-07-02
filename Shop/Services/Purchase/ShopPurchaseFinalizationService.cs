using FakeMG.Inventory;
using FakeMG.Shop.Config;
using FakeMG.Shop.RuntimeData;

namespace FakeMG.Shop.Services.Purchase
{
    public class ShopPurchaseFinalizationService
    {
        private readonly IInventoryBalanceRepository _inventoryBalanceRepository;
        private readonly ShopOwnershipStateRepository _shopOwnershipStateRepository;

        public ShopPurchaseFinalizationService(
            IInventoryBalanceRepository inventoryBalanceRepository,
            ShopOwnershipStateRepository shopOwnershipStateRepository)
        {
            _inventoryBalanceRepository = inventoryBalanceRepository;
            _shopOwnershipStateRepository = shopOwnershipStateRepository;
        }

        #region Public Methods

        public void FinalizeSuccessfulPurchase(ShopListingSO shopListingSO, ShopPurchaseResult shopPurchaseResult)
        {
            _inventoryBalanceRepository.Add(shopPurchaseResult.GrantedItems);

            if (shopListingSO.IsNonConsumable)
            {
                _shopOwnershipStateRepository.MarkOwned(shopListingSO.Id);
            }
        }

        #endregion
    }
}
