using System;
using FakeMG.Shop.Config;
using FakeMG.Shop.Filters;
using FakeMG.Shop.Services.Purchase;
using VContainer;
using VContainer.Unity;

namespace FakeMG.Shop
{
    /// <summary>
    /// Registers the shop purchase backend (catalog filters, purchase executors, orchestrator, finalization
    /// and the shop session) into a container. Shared by <see cref="ShopLifetimeScope"/> and by game-side
    /// scopes that need the shop services visible to the same resolver that injects the shop UI.
    /// </summary>
    public static class ShopInstaller
    {
        #region Public Methods

        public static void Register(IContainerBuilder builder, ShopListingDatabaseSO shopListingDatabaseSo)
        {
            builder.RegisterInstance(shopListingDatabaseSo);

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

        #endregion
    }
}
