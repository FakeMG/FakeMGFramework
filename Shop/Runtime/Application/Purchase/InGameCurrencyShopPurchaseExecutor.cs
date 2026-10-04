using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Inventory;
using FakeMG.Numbers;
using FakeMG.Shop.Config;
using FakeMG.Shop.RuntimeData;

namespace FakeMG.Shop.Services.Purchase
{
    public class InGameCurrencyShopPurchaseExecutor : IShopPurchaseExecutor
    {
        private readonly IInventoryBalanceRepository _inventoryBalanceRepository;

        public InGameCurrencyShopPurchaseExecutor(IInventoryBalanceRepository inventoryBalanceRepository)
        {
            _inventoryBalanceRepository = inventoryBalanceRepository;
        }

        #region Public Methods

        public bool CanExecute(ShopPurchaseType purchaseType)
        {
            return purchaseType == ShopPurchaseType.InGameCurrency;
        }

        public UniTask<ShopPurchaseResult> TryPurchaseAsync(
            ShopListingSO shopListingSO,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyList<ItemAmountEntry> priceEntries = shopListingSO.GetPrice();
            if (!CanAffordAllPriceEntries(priceEntries))
            {
                return UniTask.FromResult(ShopPurchaseResult.Failed("Not enough currency."));
            }

            if (!_inventoryBalanceRepository.TrySpend(priceEntries))
            {
                return UniTask.FromResult(ShopPurchaseResult.Failed("Purchase failed. Please try again."));
            }

            return UniTask.FromResult(ShopPurchaseResult.Succeeded(shopListingSO.GetAllItemsGranted()));
        }

        #endregion

        #region Private Methods

        private bool CanAffordAllPriceEntries(IReadOnlyList<ItemAmountEntry> priceEntries)
        {
            for (int entryIndex = 0; entryIndex < priceEntries.Count; entryIndex++)
            {
                ItemAmountEntry entry = priceEntries[entryIndex];
                if (!entry.IdentitySO || entry.Amount <= GameNumber.Zero)
                {
                    continue;
                }

                GameNumber balance = _inventoryBalanceRepository.GetBalance(entry.IdentitySO);
                if (balance < entry.Amount)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion
    }
}
