#if UNITY_PURCHASING

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Shop.Config;
using FakeMG.Shop.RuntimeData;
using UnityEngine.Purchasing;

namespace FakeMG.Shop.Services.Purchase
{
    public class IapShopPurchaseExecutor : IShopPurchaseExecutor
    {
        private readonly UnityIapPurchaseService _unityIapPurchaseService;

        public event Action<string> PurchaseStarted;
        public event Action<string> PurchaseFailed;
        public event Action<string> PurchaseSucceeded;

        public IapShopPurchaseExecutor(UnityIapPurchaseService unityIapPurchaseService)
        {
            _unityIapPurchaseService = unityIapPurchaseService;
        }

        public bool CanExecute(ShopPurchaseType purchaseType)
        {
            return purchaseType == ShopPurchaseType.IAP;
        }

        public async UniTask<ShopPurchaseResult> TryPurchaseAsync(
            ShopListingSO shopListingSO,
            CancellationToken cancellationToken)
        {
            PurchaseStarted?.Invoke(shopListingSO.Id);

            if (string.IsNullOrWhiteSpace(shopListingSO.Id))
            {
                PurchaseFailed?.Invoke(shopListingSO.Id);
                return ShopPurchaseResult.Failed("Purchase failed. Please try again.");
            }

            bool wasPurchased = await _unityIapPurchaseService.TryPurchaseProductAsync(
                shopListingSO.Id,
                ResolveProductType(shopListingSO),
                cancellationToken);

            if (!wasPurchased)
            {
                PurchaseFailed?.Invoke(shopListingSO.Id);
                return ShopPurchaseResult.Failed("Purchase failed. Please try again.");
            }

            PurchaseSucceeded?.Invoke(shopListingSO.Id);
            return ShopPurchaseResult.Succeeded(shopListingSO.GetAllItemsGranted());
        }

        private static ProductType ResolveProductType(ShopListingSO shopListingSO)
        {
            return shopListingSO.IsNonConsumable
                ? ProductType.NonConsumable
                : ProductType.Consumable;
        }
    }
}

#endif
