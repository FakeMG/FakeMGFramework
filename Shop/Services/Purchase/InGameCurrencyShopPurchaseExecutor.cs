using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using FakeMG.Inventory;
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

            IReadOnlyDictionary<IdentitySO, int> priceByItem = shopListingSO.GetPrice();
            if (!CanAffordAllPriceEntries(priceByItem))
            {
                return UniTask.FromResult(ShopPurchaseResult.Failed("Not enough currency."));
            }

            if (!TrySpendAllPriceEntries(priceByItem))
            {
                return UniTask.FromResult(ShopPurchaseResult.Failed("Purchase failed. Please try again."));
            }

            return UniTask.FromResult(ShopPurchaseResult.Succeeded(shopListingSO.GetAllItemsGranted()));
        }

        #endregion

        #region Private Methods

        private bool CanAffordAllPriceEntries(IReadOnlyDictionary<IdentitySO, int> priceByItem)
        {
            foreach ((IdentitySO itemSo, int amount) in priceByItem)
            {
                if (!itemSo || amount <= 0)
                {
                    continue;
                }

                BigInteger balance = _inventoryBalanceRepository.GetBalance(itemSo);
                if (balance < amount)
                {
                    return false;
                }
            }

            return true;
        }

        private bool TrySpendAllPriceEntries(IReadOnlyDictionary<IdentitySO, int> priceByItem)
        {
            var spentEntries = new List<KeyValuePair<IdentitySO, int>>();

            foreach ((IdentitySO itemSo, int amount) in priceByItem)
            {
                if (!itemSo || amount <= 0)
                {
                    continue;
                }

                bool wasSpent = _inventoryBalanceRepository.TrySpend(itemSo, amount);
                if (!wasSpent)
                {
                    RollbackSpentEntries(spentEntries);
                    return false;
                }

                spentEntries.Add(new KeyValuePair<IdentitySO, int>(itemSo, amount));
            }

            return true;
        }

        private void RollbackSpentEntries(IReadOnlyList<KeyValuePair<IdentitySO, int>> spentEntries)
        {
            for (int entryIndex = 0; entryIndex < spentEntries.Count; entryIndex++)
            {
                KeyValuePair<IdentitySO, int> spentEntry = spentEntries[entryIndex];
                _inventoryBalanceRepository.Add(spentEntry.Key, spentEntry.Value);
            }
        }

        #endregion
    }
}
