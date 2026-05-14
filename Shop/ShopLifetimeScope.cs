using System;
using FakeMG.Shop.Config;
using FakeMG.Shop.Services.Catalog;
using FakeMG.Shop.Services.Purchase;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FakeMG.Shop
{
    public class ShopLifetimeScope : LifetimeScope
    {
        [SerializeField] private ShopListingDatabaseSO _shopListingDatabaseSo;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(_shopListingDatabaseSo);

#if UNITY_PURCHASING
            builder.Register<ShopUnavailableIapProductsVisibilityFilter>(Lifetime.Singleton);
#endif
            builder.Register<ShopOwnedNonConsumableVisibilityFilter>(Lifetime.Singleton);

#if UNITY_PURCHASING
            builder.Register<UnityIapPurchaseService>(Lifetime.Singleton)
                .AsSelf()
                .As<IDisposable>();

            builder.Register<IapShopPurchaseExecutor>(Lifetime.Singleton)
                .AsSelf()
                .As<IShopPurchaseExecutor>();
#endif
            builder.Register<InGameCurrencyShopPurchaseExecutor>(Lifetime.Singleton).As<IShopPurchaseExecutor>();
            builder.Register<ShopPurchaseOrchestrator>(Lifetime.Singleton);

            builder.Register<ShopPurchaseFinalizationService>(Lifetime.Singleton);

            builder.Register<ShopSession>(Lifetime.Singleton)
                .AsSelf()
                .As<IInitializable>()
                .As<IDisposable>();
        }
    }
}
