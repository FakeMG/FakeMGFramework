using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using FakeMG.Shop.Config;
using FakeMG.Shop.RuntimeData;

namespace FakeMG.Shop.Services.Purchase
{
    public class ShopPurchaseOrchestrator
    {
        private const string PURCHASE_FAILED_MESSAGE = "Purchase failed. Please try again.";
        private const string PURCHASE_UNAVAILABLE_MESSAGE = "Purchase unavailable.";

        private readonly IReadOnlyList<IShopPurchaseExecutor> _shopPurchaseExecutors;

        public ShopPurchaseOrchestrator(IEnumerable<IShopPurchaseExecutor> shopPurchaseExecutors)
        {
            _shopPurchaseExecutors = shopPurchaseExecutors.ToList();
        }

        #region Public Methods

        public async UniTask<ShopPurchaseResult> TryPurchaseAsync(
            ShopListingSO shopListingSO,
            CancellationToken cancellationToken)
        {
            if (shopListingSO == null)
            {
                Echo.Error("Cannot purchase because the shop listing is null.");
                return ShopPurchaseResult.Failed(PURCHASE_UNAVAILABLE_MESSAGE);
            }

            foreach (IShopPurchaseExecutor shopPurchaseExecutor in _shopPurchaseExecutors)
            {
                if (shopPurchaseExecutor == null)
                {
                    Echo.Warning("Skipping a null shop purchase executor.");
                    continue;
                }

                if (!shopPurchaseExecutor.CanExecute(shopListingSO.PurchaseType))
                {
                    continue;
                }

                return await TryPurchaseWithExecutorAsync(shopPurchaseExecutor, shopListingSO, cancellationToken);
            }

            Echo.Error($"No executor registered for purchase type: {shopListingSO.PurchaseType}");
            return ShopPurchaseResult.Failed(PURCHASE_UNAVAILABLE_MESSAGE);
        }

        #endregion

        #region Private Methods

        private static async UniTask<ShopPurchaseResult> TryPurchaseWithExecutorAsync(
            IShopPurchaseExecutor shopPurchaseExecutor,
            ShopListingSO shopListingSO,
            CancellationToken cancellationToken)
        {
            try
            {
                return await shopPurchaseExecutor.TryPurchaseAsync(shopListingSO, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                Echo.Error(
                    $"Shop purchase executor '{shopPurchaseExecutor.GetType().Name}' failed while purchasing listing " +
                    $"'{shopListingSO.Id}' with purchase type '{shopListingSO.PurchaseType}': {exception}");
                return ShopPurchaseResult.Failed(PURCHASE_FAILED_MESSAGE);
            }
        }

        #endregion
    }
}
