#if UNITY_PURCHASING

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Shop.Config;
using FakeMG.Shop.RuntimeData;
using FakeMG.Shop.Services.Purchase;

namespace FakeMG.Shop.Services.Catalog
{
    public class ShopUnavailableIapProductsVisibilityFilter
    {
        private const string UNAVAILABLE_IAP_PRICE_TEXT = "???";

        private readonly UnityIapPurchaseService _unityIapPurchaseService;

        public ShopUnavailableIapProductsVisibilityFilter(UnityIapPurchaseService unityIapPurchaseService)
        {
            _unityIapPurchaseService = unityIapPurchaseService;
        }

        #region Public Methods

        public async UniTask<IReadOnlyList<ShopListingSO>> FilterAsync(
            IReadOnlyList<ShopListingSO> sourceListings,
            CancellationToken cancellationToken)
        {
            if (sourceListings == null)
            {
                return Array.Empty<ShopListingSO>();
            }

            IReadOnlyList<IapProductFetchRequest> iapProductFetchRequests = CollectIapProductFetchRequests(sourceListings);
            IReadOnlyCollection<string> availableIapListingIds = iapProductFetchRequests.Count == 0
                ? Array.Empty<string>()
                : await _unityIapPurchaseService.FetchAvailableProductIdsAsync(iapProductFetchRequests, cancellationToken);
            var availableIapListingIdSet = new HashSet<string>(availableIapListingIds, StringComparer.Ordinal);

            var filteredListings = new List<ShopListingSO>();
            foreach (var listingSO in sourceListings)
            {
                if (listingSO == null)
                {
                    continue;
                }

                if (listingSO.PurchaseType != ShopPurchaseType.IAP)
                {
                    filteredListings.Add(listingSO);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(listingSO.Id) || !availableIapListingIdSet.Contains(listingSO.Id))
                {
                    SetupListingWithLocalizedIapPrice(listingSO, UNAVAILABLE_IAP_PRICE_TEXT);
                    continue;
                }

                SetupListingWithLocalizedIapPrice(listingSO, listingSO.LocalizedIapPriceText);

                filteredListings.Add(listingSO);
            }

            return filteredListings;
        }

        #endregion

        #region Private Methods

        private static IReadOnlyList<IapProductFetchRequest> CollectIapProductFetchRequests(
            IReadOnlyList<ShopListingSO> sourceListingSOs)
        {
            var requests = new List<IapProductFetchRequest>();
            var seenListingIds = new HashSet<string>();

            foreach (var listingSO in sourceListingSOs)
            {
                if (listingSO == null)
                {
                    continue;
                }

                if (listingSO.PurchaseType != ShopPurchaseType.IAP
                    || string.IsNullOrWhiteSpace(listingSO.Id))
                {
                    continue;
                }

                if (!seenListingIds.Add(listingSO.Id))
                {
                    continue;
                }

                requests.Add(new IapProductFetchRequest(listingSO.Id, listingSO.IsNonConsumable));
            }

            return requests;
        }

        private void SetupListingWithLocalizedIapPrice(
            ShopListingSO shopListingSO,
            string fallbackLocalizedIapPriceText)
        {
            if (!_unityIapPurchaseService.TryGetCachedLocalizedPriceString(shopListingSO.Id, out string localizedIapPriceText))
            {
                if (!string.IsNullOrWhiteSpace(fallbackLocalizedIapPriceText))
                {
                    shopListingSO.LocalizedIapPriceText = fallbackLocalizedIapPriceText;
                }
            }
            else
            {
                shopListingSO.LocalizedIapPriceText = localizedIapPriceText;
            }
        }

        #endregion
    }
}

#endif
