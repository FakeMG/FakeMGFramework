using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using FakeMG.Shop.Config;
using FakeMG.Shop.Filters;
using FakeMG.Shop.RuntimeData;
using VContainer.Unity;

namespace FakeMG.Shop.Services.Purchase
{
    public class ShopSession : IInitializable, IDisposable
    {
        private readonly ShopListingDatabaseSO _shopListingDatabaseSo;
#if UNITY_PURCHASING
        private readonly ShopUnavailableIapProductsVisibilityFilter _unavailableIapProductsVisibilityFilter;
#endif
        private readonly ShopOwnedNonConsumableVisibilityFilter _ownedNonConsumableVisibilityFilter;
        private readonly ShopPurchaseOrchestrator _shopPurchaseOrchestrator;
        private readonly IShopPurchaseFinalizer _shopPurchaseFinalizationService;
        private readonly CancellationTokenSource _disposeCancellationTokenSource = new();

        public event Action<IReadOnlyList<ShopListingSO>> OnCatalogLoaded;
        public event Action<ShopPurchaseResult> OnPurchaseCompleted;
        public event Action<string> OnListingHidden;
        public event Action OnPurchaseStateChanged;

        public bool IsPurchaseInProgress { get; private set; }

        public IReadOnlyList<ShopListingSO> CurrentVisibleListingSOs { get; private set; }

        public ShopSession(
            ShopListingDatabaseSO shopListingDatabaseSo,
#if UNITY_PURCHASING
            ShopUnavailableIapProductsVisibilityFilter unavailableIapProductsVisibilityFilter,
#endif
            ShopOwnedNonConsumableVisibilityFilter ownedNonConsumableVisibilityFilter,
            ShopPurchaseOrchestrator shopPurchaseOrchestrator,
            IShopPurchaseFinalizer shopPurchaseFinalizationService)
        {
            _shopListingDatabaseSo = shopListingDatabaseSo;
#if UNITY_PURCHASING
            _unavailableIapProductsVisibilityFilter = unavailableIapProductsVisibilityFilter;
#endif
            _ownedNonConsumableVisibilityFilter = ownedNonConsumableVisibilityFilter;
            _shopPurchaseOrchestrator = shopPurchaseOrchestrator;
            _shopPurchaseFinalizationService = shopPurchaseFinalizationService;
        }

        #region Public Methods

        public void Initialize()
        {
            Echo.Log("Initializing ShopSession and loading catalog.");
            LoadAsync(_disposeCancellationTokenSource.Token).Forget();
        }

        public void Dispose()
        {
            _disposeCancellationTokenSource.Cancel();
            _disposeCancellationTokenSource.Dispose();
        }

        public async UniTask LoadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await RefreshVisibleCatalogAsync(cancellationToken);
        }

        public async UniTask TryPurchaseAsync(ShopListingSO shopListingSO, CancellationToken cancellationToken)
        {
            if (IsPurchaseInProgress)
            {
                Echo.Warning("Shop purchase ignored because another purchase is in progress.");
                return;
            }
            IsPurchaseInProgress = true;
            OnPurchaseStateChanged?.Invoke();
            try
            {
                if (!await _shopPurchaseFinalizationService.PreparePurchaseAsync(shopListingSO, cancellationToken))
                {
                    OnPurchaseCompleted?.Invoke(ShopPurchaseResult.Failed("Purchase unavailable. A reward is awaiting delivery."));
                    return;
                }
                ShopPurchaseResult result = await _shopPurchaseOrchestrator.TryPurchaseAsync(shopListingSO, cancellationToken);
                if (result.IsSuccess && !result.IsFinalized)
                {
                    result = await _shopPurchaseFinalizationService.FinalizePurchaseAsync(shopListingSO, result, cancellationToken);
                }

                if (result.IsSuccess && shopListingSO.IsNonConsumable)
                {
                    await RefreshVisibleCatalogAsync(cancellationToken);
                    OnListingHidden?.Invoke(shopListingSO.Id);
                }
                OnPurchaseCompleted?.Invoke(result);
            }
            catch (OperationCanceledException)
            {
                Echo.Warning("Shop purchase was cancelled before completion.");
                OnPurchaseCompleted?.Invoke(ShopPurchaseResult.Failed("Purchase cancelled."));
            }
            finally
            {
                // Store dialogs can complete inside their own click handler. Keep that click away from the shop,
                // including the next input update, until the dialog's deferred destruction has finished.
                await UniTask.NextFrame(PlayerLoopTiming.LastPostLateUpdate);
                IsPurchaseInProgress = false;
                OnPurchaseStateChanged?.Invoke();
            }
        }

        #endregion

        #region Private Methods

        private async UniTask RefreshVisibleCatalogAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<ShopListingSO> listings = _shopListingDatabaseSo.GetAllAssets();
            foreach (ShopListingSO listingSO in listings)
            {
                if (listingSO.PurchaseType == ShopPurchaseType.IAP)
                {
                    listingSO.IsIapAvailable = false;
                    listingSO.LocalizedIapPriceText = string.Empty;
                }
            }
            CurrentVisibleListingSOs = _ownedNonConsumableVisibilityFilter.Filter(listings);
            OnCatalogLoaded?.Invoke(CurrentVisibleListingSOs);
#if UNITY_PURCHASING
            try
            {
                listings = await _unavailableIapProductsVisibilityFilter.FilterAsync(listings, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                Echo.Warning($"Store catalog unavailable; currency and rewarded listings remain usable: {exception}");
            }
#endif
            CurrentVisibleListingSOs = _ownedNonConsumableVisibilityFilter.Filter(listings);
            OnCatalogLoaded?.Invoke(CurrentVisibleListingSOs);
        }

        #endregion
    }
}
