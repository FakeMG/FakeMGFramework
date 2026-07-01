using System.Collections.Generic;
using FakeMG.Framework;
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
            GrantItems(shopPurchaseResult.GrantedItemsByItem);

            if (shopListingSO.IsNonConsumable)
            {
                _shopOwnershipStateRepository.MarkOwned(shopListingSO.Id);
            }
        }

        #endregion

        #region Private Methods

        private void GrantItems(IReadOnlyDictionary<IdentitySO, int> grantedItemsByItem)
        {
            foreach ((IdentitySO itemSo, int amount) in grantedItemsByItem)
            {
                if (!itemSo || amount <= 0)
                {
                    continue;
                }

                _inventoryBalanceRepository.Add(itemSo, amount);
            }
        }

        #endregion
    }
}
