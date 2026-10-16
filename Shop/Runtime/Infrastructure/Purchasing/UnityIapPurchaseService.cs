#if UNITY_PURCHASING
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using FakeMG.Shop.Config;
using FakeMG.Shop.RuntimeData;
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

    public sealed class UnityIapPurchaseService : IDisposable, IShopPurchaseConfirmation
    {
        private const int STORE_TIMEOUT_SECONDS = 20;
        private readonly ShopListingDatabaseSO _catalogSO;
        private readonly ShopPurchaseFulfillmentService _finalizer;
        private readonly SemaphoreSlim _storeLock = new(1, 1);
        private readonly CancellationTokenSource _lifetimeCancellation = new();
        private readonly Dictionary<string, UniTaskCompletionSource<ShopPurchaseResult>> _purchaseCompletions = new();
        private readonly Dictionary<string, PendingOrder> _pendingOrders = new();
        private readonly HashSet<string> _processingTransactionIds = new();
        private StoreController _storeController;
        private UnityIapStoreSubscriber _subscriber;
        private UniTaskCompletionSource<bool> _productsCompletion;
        private UniTaskCompletionSource<bool> _purchasesCompletion;
        private bool _isStoreConnected;

        public event Action<ConfirmedOrder> PurchaseConfirmed;

        public UnityIapPurchaseService(ShopListingDatabaseSO catalogSO, ShopPurchaseFulfillmentService finalizer)
        {
            _catalogSO = catalogSO;
            _finalizer = finalizer;
        }

        #region Public Methods

        public async UniTask<ShopPurchaseResult> TryPurchaseProductAsync(string productId, ProductType productType,
            CancellationToken cancellationToken)
        {
            if (_purchaseCompletions.ContainsKey(productId) || _pendingOrders.Values.Any(order => GetProductId(order) == productId))
            {
                Echo.Warning($"Product '{productId}' already has an unfinished purchase.");
                return ShopPurchaseResult.Failed("A purchase is already pending.");
            }
            bool hasIssuedPurchase = false;
            try
            {
                await EnsureStoreConnectedAsync(cancellationToken);
                Product product = _storeController.GetProductById(productId);
                if (product == null || !product.availableToPurchase)
                {
                    Echo.Warning($"Store product '{productId}' is unavailable.");
                    await _finalizer.RemovePurchaseAttemptsAsync(new[] { productId }, CancellationToken.None);
                    return ShopPurchaseResult.Failed("Product unavailable.");
                }
                var completion = new UniTaskCompletionSource<ShopPurchaseResult>();
                _purchaseCompletions.Add(productId, completion);
                hasIssuedPurchase = true;
                _storeController.PurchaseProduct(productId);
                // Once a purchase starts, only the service lifetime cancels waiting. Closing UI must not cancel fulfillment.
                return await completion.Task.AttachExternalCancellation(_lifetimeCancellation.Token);
            }
            catch (Exception exception)
            {
                Echo.Warning($"Store purchase '{productId}' interrupted: {exception}");
                if (!hasIssuedPurchase)
                {
                    await _finalizer.RemovePurchaseAttemptsAsync(new[] { productId }, CancellationToken.None);
                }
                throw;
            }
            finally
            {
                _purchaseCompletions.Remove(productId);
            }
        }

        public async UniTask<IReadOnlyCollection<string>> FetchAvailableProductIdsAsync(
            IReadOnlyList<IapProductFetchRequest> requests, CancellationToken cancellationToken)
        {
            await _storeLock.WaitAsync(cancellationToken);
            try
            {
                await EnsureStoreConnectedAsync(cancellationToken);
                _productsCompletion = new UniTaskCompletionSource<bool>();
                var definitions = requests.Select(request => new ProductDefinition(request.ListingId,
                    request.IsNonConsumable ? ProductType.NonConsumable : ProductType.Consumable)).ToList();
                if (definitions.Count == 0)
                {
                    return Array.Empty<string>();
                }
                _storeController.FetchProductsWithNoRetries(definitions);
                await _productsCompletion.Task.AttachExternalCancellation(cancellationToken)
                    .Timeout(TimeSpan.FromSeconds(STORE_TIMEOUT_SECONDS));
                _purchasesCompletion = new UniTaskCompletionSource<bool>();
                _storeController.FetchPurchases();
                bool didRecoverPurchases = await _purchasesCompletion.Task.AttachExternalCancellation(cancellationToken)
                    .Timeout(TimeSpan.FromSeconds(STORE_TIMEOUT_SECONDS));
                if (!didRecoverPurchases)
                {
                    return Array.Empty<string>();
                }
                return requests.Where(request => _storeController.GetProductById(request.ListingId)?.availableToPurchase == true)
                    .Select(request => request.ListingId).ToArray();
            }
            finally
            {
                _productsCompletion = null;
                _purchasesCompletion = null;
                _storeLock.Release();
            }
        }

        public bool TryGetCachedLocalizedPriceString(string productId, out string localizedPriceString)
        {
            localizedPriceString = _storeController?.GetProductById(productId)?.metadata.localizedPriceString ?? string.Empty;
            return !string.IsNullOrWhiteSpace(localizedPriceString);
        }

        public async UniTask RetryPendingPurchasesAsync()
        {
            foreach (PendingOrder order in _pendingOrders.Values.ToArray())
            {
                await FulfillPendingOrderAsync(order);
            }
        }

        public void MarkConnected()
        {
            _isStoreConnected = true;
        }

        public void MarkDisconnected(StoreConnectionFailureDescription description)
        {
            _isStoreConnected = false;
            Echo.Warning($"Store disconnected: {description.message}");
            foreach (var completion in _purchaseCompletions.Values.ToArray())
            {
                completion.TrySetResult(ShopPurchaseResult.Failed("Store disconnected. Pending rewards will be recovered."));
            }
        }

        public void CompleteProductFetch(List<Product> products)
        {
            _productsCompletion?.TrySetResult(true);
        }

        public void FailProductFetch(ProductFetchFailed failure)
        {
            Echo.Warning($"Store product fetch failed: {failure.FailureReason}");
            _productsCompletion?.TrySetResult(false);
        }

        public void RecoverFetchedPurchases(Orders orders)
        {
            ReconcileFetchedPurchasesAsync(orders).Forget();
        }

        public void ReportPurchaseFetchFailure(PurchasesFetchFailureDescription failure)
        {
            Echo.Warning($"Store purchase recovery failed: {failure.Message}");
            _purchasesCompletion?.TrySetResult(false);
        }

        public void BeginFulfillment(PendingOrder order)
        {
            FulfillPendingOrderAsync(order).Forget();
        }

        public void ConfirmPurchase(string transactionId)
        {
            _storeController.ConfirmPurchase(_pendingOrders[transactionId]);
        }

        public void CompleteConfirmation(Order order)
        {
            string productId = GetProductId(order);
            if (order is ConfirmedOrder confirmedOrder)
            {
                _pendingOrders.Remove(order.Info.TransactionID);
                ShopListingSO listingSO = _catalogSO.GetAssetByID(productId);
                if (listingSO == null)
                {
                    Echo.Warning($"Confirmed store product '{productId}' belongs to another catalog.");
                    return;
                }
                PurchaseConfirmed?.Invoke(confirmedOrder);
                CompleteSettledPurchaseAsync(productId, ShopPurchaseResult.Succeeded(
                    listingSO.GetAllItemsGranted(), order.Info.TransactionID, true)).Forget();
            }
            else
            {
                Echo.Warning($"Store confirmation failed for '{productId}'. Reward retained; confirmation will be retried.");
                CompletePurchase(productId, ShopPurchaseResult.Failed("Reward delivered. Store confirmation pending."));
            }
        }

        public void FailPurchase(FailedOrder order)
        {
            Echo.Warning($"Store purchase failed: {order.FailureReason} - {order.Details}");
            ResolveFailedPurchaseAsync(GetProductId(order), order.FailureReason).Forget();
        }

        public async UniTask ResolveFailedPurchaseAsync(string productId, PurchaseFailureReason failureReason)
        {
            ShopPurchaseResult result = ShopPurchaseResult.Failed("Purchase cancelled or failed.");
            if (IsTerminalFailure(failureReason))
            {
                await CompleteSettledPurchaseAsync(productId, result);
            }
            else
            {
                Echo.Warning($"Retaining purchase attempt '{productId}' after '{failureReason}' until the store resolves its order state.");
                CompletePurchase(productId, result);
            }
        }

        public void DeferPurchase(DeferredOrder order)
        {
            Echo.Warning($"Store purchase '{GetProductId(order)}' requires approval.");
            CompletePurchase(GetProductId(order), ShopPurchaseResult.Failed("Purchase awaiting approval."));
        }

        public void Dispose()
        {
            _subscriber?.Dispose();
            _lifetimeCancellation.Cancel();
            _lifetimeCancellation.Dispose();
            foreach (var completion in _purchaseCompletions.Values.ToArray())
            {
                completion.TrySetResult(ShopPurchaseResult.Failed("Shop session ended. Pending purchases will be recovered."));
            }
        }

        #endregion

        #region Private Methods

        private async UniTask ReconcileFetchedPurchasesAsync(Orders orders)
        {
            var retainedProductIds = new HashSet<string>(orders.PendingOrders.Select(GetProductId));
            retainedProductIds.UnionWith(orders.DeferredOrders.Select(GetProductId));
            retainedProductIds.UnionWith(_pendingOrders.Values.Select(GetProductId));
            retainedProductIds.UnionWith(_purchaseCompletions.Keys);
            string[] settledProductIds = _catalogSO.GetAllAssets().Where(listing => listing.PurchaseType == ShopPurchaseType.IAP &&
                !retainedProductIds.Contains(listing.Id)).Select(listing => listing.Id).ToArray();
            await _finalizer.RemovePurchaseAttemptsAsync(settledProductIds, CancellationToken.None);
            foreach (PendingOrder order in orders.PendingOrders)
            {
                BeginFulfillment(order);
            }
            _purchasesCompletion?.TrySetResult(true);
        }

        private async UniTask CompleteSettledPurchaseAsync(string productId, ShopPurchaseResult result)
        {
            await _finalizer.RemovePurchaseAttemptsAsync(new[] { productId }, CancellationToken.None);
            CompletePurchase(productId, result);
        }

        private static bool IsTerminalFailure(PurchaseFailureReason reason) => reason is
            PurchaseFailureReason.UserCancelled or PurchaseFailureReason.OrderCancelled or PurchaseFailureReason.PaymentDeclined or
            PurchaseFailureReason.ProductUnavailable or PurchaseFailureReason.PurchasingUnavailable or
            PurchaseFailureReason.StoreNotConnected or PurchaseFailureReason.UserNotAuthenticated or PurchaseFailureReason.NotSupported;

        private async UniTask EnsureStoreConnectedAsync(CancellationToken cancellationToken)
        {
            if (_isStoreConnected)
            {
                return;
            }
            if (_storeController == null)
            {
                _storeController = UnityIAPServices.StoreController();
                _storeController.ProcessPendingOrdersOnPurchasesFetched(false);
                _subscriber = new UnityIapStoreSubscriber(_storeController, this);
            }
            await _storeController.Connect().AsUniTask().AttachExternalCancellation(cancellationToken)
                .Timeout(TimeSpan.FromSeconds(STORE_TIMEOUT_SECONDS));
        }

        private async UniTask FulfillPendingOrderAsync(PendingOrder order)
        {
            string transactionId = order.Info.TransactionID;
            string productId = GetProductId(order);
            if (string.IsNullOrWhiteSpace(transactionId) || string.IsNullOrWhiteSpace(productId))
            {
                Echo.Error("Cannot fulfill a store order without product and transaction IDs.");
                return;
            }
            _pendingOrders[transactionId] = order;
            if (!_processingTransactionIds.Add(transactionId))
            {
                Echo.Warning($"Store transaction '{transactionId}' is already being fulfilled.");
                return;
            }
            try
            {
                ShopListingSO listingSO = _catalogSO.GetAssetByID(productId);
                if (listingSO == null)
                {
                    Echo.Error($"Pending store product '{productId}' is absent from the shop catalog. Order remains unconfirmed.");
                    return;
                }
                ShopPurchaseResult result = await _finalizer.FulfillAndConfirmAsync(listingSO,
                    ShopPurchaseResult.Succeeded(listingSO.GetAllItemsGranted(), transactionId), this, _lifetimeCancellation.Token);
                // Presentation can finish once rewards are durable. A delayed confirmation callback must not lock the shop.
                // The unconfirmed order stays in _pendingOrders for confirmation retries, without granting again.
                CompletePurchase(productId, result);
            }
            catch (Exception exception)
            {
                Echo.Error($"Pending purchase '{transactionId}' remains unconfirmed: {exception}");
                CompletePurchase(productId, ShopPurchaseResult.Failed("Reward pending. Reopen the shop to retry."));
            }
            finally
            {
                _processingTransactionIds.Remove(transactionId);
            }
        }

        private void CompletePurchase(string productId, ShopPurchaseResult result)
        {
            if (_purchaseCompletions.TryGetValue(productId, out var completion))
            {
                completion.TrySetResult(result);
            }
        }

        private static string GetProductId(Order order)
        {
            return order.CartOrdered.Items().FirstOrDefault()?.Product?.definition?.id ?? string.Empty;
        }

        #endregion
    }
}
#endif
