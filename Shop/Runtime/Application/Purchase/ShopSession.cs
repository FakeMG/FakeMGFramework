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
        private readonly ShopPurchaseFinalizationService _shopPurchaseFinalizationService;
        private readonly CancellationTokenSource _disposeCancellationTokenSource = new();

        public event Action<IReadOnlyList<ShopListingSO>> OnCatalogLoaded;
        public event Action<ShopPurchaseResult> OnPurchaseCompleted;
        public event Action<string> OnListingHidden;

        public IReadOnlyList<ShopListingSO> CurrentVisibleListingSOs { get; private set; }

        public ShopSession(
            ShopListingDatabaseSO shopListingDatabaseSo,
#if UNITY_PURCHASING
            ShopUnavailableIapProductsVisibilityFilter unavailableIapProductsVisibilityFilter,
#endif
            ShopOwnedNonConsumableVisibilityFilter ownedNonConsumableVisibilityFilter,
            ShopPurchaseOrchestrator shopPurchaseOrchestrator,
            ShopPurchaseFinalizationService shopPurchaseFinalizationService)
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
            if (shopListingSO.PurchaseType == ShopPurchaseType.IAP && string.IsNullOrWhiteSpace(shopListingSO.Id))
            {
                Echo.Warning("Purchase skipped for an IAP listing without a product id.");
                return;
            }

            ShopPurchaseResult shopPurchaseResult =
                await _shopPurchaseOrchestrator.TryPurchaseAsync(shopListingSO, cancellationToken);
            if (!shopPurchaseResult.IsSuccess)
            {
                OnPurchaseCompleted?.Invoke(shopPurchaseResult);
                return;
            }

            _shopPurchaseFinalizationService.FinalizeSuccessfulPurchase(shopListingSO, shopPurchaseResult);

            if (shopListingSO.IsNonConsumable)
            {
                await RefreshVisibleCatalogAsync(cancellationToken);
                OnListingHidden?.Invoke(shopListingSO.Id);
            }

            OnPurchaseCompleted?.Invoke(shopPurchaseResult);
        }

        #endregion

        #region Private Methods

        private async UniTask RefreshVisibleCatalogAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<ShopListingSO> listings = _shopListingDatabaseSo.GetAllAssets();
#if UNITY_PURCHASING
            listings = await _unavailableIapProductsVisibilityFilter.FilterAsync(listings, cancellationToken);
#endif
            CurrentVisibleListingSOs = _ownedNonConsumableVisibilityFilter.Filter(listings);
            OnCatalogLoaded?.Invoke(CurrentVisibleListingSOs);
        }

        #endregion
    }
}
