#if UNITY_PURCHASING

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using UnityEngine;
using UnityEngine.Purchasing;

namespace FakeMG.Shop.Services.Purchase
{
    public readonly struct IapProductFetchRequest
    {
        public string ListingId { get; }
        public bool IsNonConsumable { get; }

        public IapProductFetchRequest(string listingId, bool isNonConsumable)
        {
            ListingId = listingId;
            IsNonConsumable = isNonConsumable;
        }
    }

    public class UnityIapPurchaseService : IDisposable
    {
        private readonly SemaphoreSlim _storeConnectionSemaphore = new(1, 1);
        private readonly Dictionary<string, UniTaskCompletionSource<bool>> _pendingPurchaseCompletionByProductId =
            new(StringComparer.Ordinal);
        private readonly object _pendingPurchaseLock = new();

        private StoreController _storeController;
        private bool _isStoreConnected;

        public event Action<ConfirmedOrder> PurchaseConfirmed;

        #region VContainer Lifecycle

        public void Dispose()
        {
            if (_storeController != null)
            {
                _storeController.OnStoreConnected -= MarkStoreConnectedWhenStoreConnected;
                _storeController.OnStoreDisconnected -= MarkStoreDisconnectedWhenStoreDisconnected;
                _storeController.OnPurchasePending -= ConfirmOrderWhenPurchaseIsPending;
                _storeController.OnPurchaseConfirmed -= CompletePendingPurchaseWhenOrderIsConfirmed;
                _storeController.OnPurchaseFailed -= FailPendingPurchaseWhenOrderFails;
            }

            _storeConnectionSemaphore.Dispose();
        }

        #endregion

        #region Public Methods

        public async UniTask<bool> TryPurchaseProductAsync(string productId, ProductType productType, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                Echo.Warning($"{nameof(TryPurchaseProductAsync)} received an empty product id.");
                return false;
            }

            await EnsureStoreConnectedAsync(cancellationToken);

            bool wasProductFetched = await EnsureProductFetchedAsync(productId, productType, cancellationToken);
            if (!wasProductFetched)
            {
                Echo.Warning($"Product with id '{productId}' is not available for purchase.");
                return false;
            }

            if (!TryBeginPendingPurchase(productId, out UniTaskCompletionSource<bool> completionSource))
            {
                Echo.Warning($"Purchase is already in progress for product '{productId}'.");
                return false;
            }

            await using CancellationTokenRegistration cancellationRegistration =
                cancellationToken.RegisterWithoutCaptureExecutionContext(() => CancelPendingPurchase(productId, cancellationToken));

            _storeController.PurchaseProduct(productId);

            try
            {
                return await completionSource.Task;
            }
            finally
            {
                RemovePendingPurchase(productId);
            }
        }

        public async UniTask<IReadOnlyCollection<string>> FetchAvailableProductIdsAsync(
            IReadOnlyList<IapProductFetchRequest> productFetchRequests,
            CancellationToken cancellationToken)
        {
            Echo.Log($"Fetching product info for {productFetchRequests?.Count ?? 0} products...");
            var availableProductIds = new HashSet<string>(StringComparer.Ordinal);
            if (productFetchRequests == null || productFetchRequests.Count == 0)
            {
                return availableProductIds;
            }

            await EnsureStoreConnectedAsync(cancellationToken);

            var productDefinitionsToFetch = new List<ProductDefinition>();
            foreach (var request in productFetchRequests)
            {
                if (string.IsNullOrWhiteSpace(request.ListingId))
                {
                    continue;
                }

                if (availableProductIds.Contains(request.ListingId))
                {
                    continue;
                }

                Product cachedProduct = _storeController.GetProductById(request.ListingId);
                if (cachedProduct != null)
                {
                    availableProductIds.Add(request.ListingId);
                    continue;
                }

                ProductType productType = request.IsNonConsumable
                    ? ProductType.NonConsumable
                    : ProductType.Consumable;

                productDefinitionsToFetch.Add(new ProductDefinition(request.ListingId, productType));
            }

            if (productDefinitionsToFetch.Count == 0)
            {
                Echo.Log("All products are already cached. No need to fetch from the store.");
                return availableProductIds;
            }

            var completionSource = new UniTaskCompletionSource<bool>();

            _storeController.OnProductsFetched += MarkFetchCompletedWhenProductsFetched;
            _storeController.OnProductsFetchFailed += MarkFetchCompletedWhenProductsFetchFails;

            try
            {
                _storeController.FetchProducts(productDefinitionsToFetch);

                await using CancellationTokenRegistration cancellationRegistration =
                    cancellationToken.RegisterWithoutCaptureExecutionContext(() => completionSource.TrySetCanceled(cancellationToken));

                await completionSource.Task;
            }
            finally
            {
                _storeController.OnProductsFetched -= MarkFetchCompletedWhenProductsFetched;
                _storeController.OnProductsFetchFailed -= MarkFetchCompletedWhenProductsFetchFails;
            }

            foreach (var request in productFetchRequests)
            {
                if (string.IsNullOrWhiteSpace(request.ListingId))
                {
                    continue;
                }

                Product fetchedProduct = _storeController.GetProductById(request.ListingId);
                if (fetchedProduct == null)
                {
                    continue;
                }

                Echo.Log($"Product with id '{fetchedProduct.definition.id}' '{fetchedProduct.metadata.localizedPriceString}' is available for purchase.");
                availableProductIds.Add(request.ListingId);
            }

            return availableProductIds;

            void MarkFetchCompletedWhenProductsFetchFails(ProductFetchFailed failure)
            {
                completionSource.TrySetResult(false);
            }

            void MarkFetchCompletedWhenProductsFetched(List<Product> products)
            {
                _storeController.FetchPurchases();
                completionSource.TrySetResult(true);
            }
        }

        public bool TryGetCachedLocalizedPriceString(string productId, out string localizedPriceString)
        {
            localizedPriceString = string.Empty;
            if (string.IsNullOrWhiteSpace(productId))
            {
                Echo.Warning($"{nameof(TryGetCachedLocalizedPriceString)} received an empty product id.");
                return false;
            }

            if (_storeController == null)
            {
                Echo.Warning($"{nameof(TryGetCachedLocalizedPriceString)} was called before the store controller was created.");
                return false;
            }

            Product cachedProduct = _storeController.GetProductById(productId);
            if (cachedProduct == null)
            {
                Echo.Warning($"Product with id '{productId}' was not cached when reading localized price.");
                return false;
            }

            localizedPriceString = cachedProduct.metadata.localizedPriceString;
            if (!string.IsNullOrWhiteSpace(localizedPriceString))
            {
                return true;
            }

            Echo.Warning($"Product with id '{productId}' does not have a localized price string.");
            return false;
        }

        #endregion

        #region Private Methods

        private async UniTask EnsureStoreConnectedAsync(CancellationToken cancellationToken)
        {
            if (_isStoreConnected)
            {
                return;
            }

            await _storeConnectionSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (_isStoreConnected)
                {
                    return;
                }

                if (_storeController == null)
                {
                    _storeController = UnityIAPServices.StoreController();

                    _storeController.OnStoreConnected += MarkStoreConnectedWhenStoreConnected;
                    _storeController.OnStoreDisconnected += MarkStoreDisconnectedWhenStoreDisconnected;
                    _storeController.OnPurchasePending += ConfirmOrderWhenPurchaseIsPending;
                    _storeController.OnPurchaseConfirmed += CompletePendingPurchaseWhenOrderIsConfirmed;
                    _storeController.OnPurchaseFailed += FailPendingPurchaseWhenOrderFails;
                }

                await _storeController.Connect();
                _isStoreConnected = true;
            }
            finally
            {
                _storeConnectionSemaphore.Release();
            }
        }

        private async UniTask<bool> EnsureProductFetchedAsync(string productId, ProductType productType, CancellationToken cancellationToken)
        {
            Product cachedProduct = _storeController.GetProductById(productId);
            if (cachedProduct != null)
            {
                return true;
            }

            var completionSource = new UniTaskCompletionSource<bool>();

            void CompleteFetchWhenProductsAreFetched(List<Product> products)
            {
                bool foundProduct = products.Any(product => product != null && product.definition.id == productId);
                completionSource.TrySetResult(foundProduct);
            }

            void CompleteFetchWhenProductsFetchFails(ProductFetchFailed failure)
            {
                completionSource.TrySetResult(false);
            }

            _storeController.OnProductsFetched += CompleteFetchWhenProductsAreFetched;
            _storeController.OnProductsFetchFailed += CompleteFetchWhenProductsFetchFails;

            try
            {
                _storeController.FetchProducts(new List<ProductDefinition>
                {
                    new(productId, productType),
                });

                using CancellationTokenRegistration cancellationRegistration =
                    cancellationToken.RegisterWithoutCaptureExecutionContext(() => completionSource.TrySetCanceled(cancellationToken));

                return await completionSource.Task;
            }
            finally
            {
                _storeController.OnProductsFetched -= CompleteFetchWhenProductsAreFetched;
                _storeController.OnProductsFetchFailed -= CompleteFetchWhenProductsFetchFails;
            }
        }

        private bool TryBeginPendingPurchase(string productId, out UniTaskCompletionSource<bool> completionSource)
        {
            lock (_pendingPurchaseLock)
            {
                if (_pendingPurchaseCompletionByProductId.ContainsKey(productId))
                {
                    completionSource = null;
                    return false;
                }

                completionSource = new UniTaskCompletionSource<bool>();
                _pendingPurchaseCompletionByProductId[productId] = completionSource;
                return true;
            }
        }

        private void MarkStoreConnectedWhenStoreConnected()
        {
            _isStoreConnected = true;
        }

        private void MarkStoreDisconnectedWhenStoreDisconnected(StoreConnectionFailureDescription description)
        {
            _isStoreConnected = false;
            Echo.Warning($"Store disconnected: {description.message}");
        }

        private void ConfirmOrderWhenPurchaseIsPending(PendingOrder order)
        {
            _storeController.ConfirmPurchase(order);
        }

        private void CompletePendingPurchaseWhenOrderIsConfirmed(Order order)
        {
            string productId = ExtractFirstProductId(order);
            if (string.IsNullOrWhiteSpace(productId))
            {
                return;
            }

            switch (order)
            {
                case ConfirmedOrder confirmedOrder:
                    CompletePendingPurchase(productId, true);
                    PurchaseConfirmed?.Invoke(confirmedOrder);
                    return;
                case FailedOrder:
                    CompletePendingPurchase(productId, false);
                    break;
            }
        }

        private void FailPendingPurchaseWhenOrderFails(FailedOrder order)
        {
            string productId = ExtractFirstProductId(order);
            if (string.IsNullOrWhiteSpace(productId))
            {
                return;
            }

            CompletePendingPurchase(productId, false);
        }

        private void CompletePendingPurchase(string productId, bool isSuccess)
        {
            lock (_pendingPurchaseLock)
            {
                if (_pendingPurchaseCompletionByProductId.TryGetValue(productId, out UniTaskCompletionSource<bool> completionSource))
                {
                    completionSource.TrySetResult(isSuccess);
                }
            }
        }

        private void CancelPendingPurchase(string productId, CancellationToken cancellationToken)
        {
            lock (_pendingPurchaseLock)
            {
                if (_pendingPurchaseCompletionByProductId.TryGetValue(productId, out UniTaskCompletionSource<bool> completionSource))
                {
                    completionSource.TrySetCanceled(cancellationToken);
                }
            }
        }

        private void RemovePendingPurchase(string productId)
        {
            lock (_pendingPurchaseLock)
            {
                _pendingPurchaseCompletionByProductId.Remove(productId);
            }
        }

        private static string ExtractFirstProductId(Order order)
        {
            return order.CartOrdered.Items().FirstOrDefault()?.Product?.definition?.id;
        }

        #endregion

    }
}

#endif
