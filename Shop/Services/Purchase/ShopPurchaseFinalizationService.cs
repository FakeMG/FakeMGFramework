using System.Collections.Generic;
using FakeMG.Framework;
using FakeMG.Framework.EventBus;
using FakeMG.Inventory;
using FakeMG.Shop.Config;
using FakeMG.Shop.RuntimeData;

namespace FakeMG.Shop.Services.Purchase
{
    public class ShopPurchaseFinalizationService
    {
        private readonly InventoryDataManager _inventoryDataManager;
        private readonly ShopOwnershipStateRepository _shopOwnershipStateRepository;

        public ShopPurchaseFinalizationService(
            InventoryDataManager inventoryDataManager,
            ShopOwnershipStateRepository shopOwnershipStateRepository)
        {
            _inventoryDataManager = inventoryDataManager;
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

            // EventBus<GameplayAutoSaveEvent>.Raise(new GameplayAutoSaveEvent());
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

                _inventoryDataManager.Add(itemSo, amount);
            }
        }

        #endregion
    }
}
