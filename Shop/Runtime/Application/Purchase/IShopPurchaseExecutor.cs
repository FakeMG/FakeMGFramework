using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Shop.Config;
using FakeMG.Shop.RuntimeData;

namespace FakeMG.Shop.Services.Purchase
{
    public interface IShopPurchaseExecutor
    {
        bool CanExecute(ShopPurchaseType purchaseType);
        UniTask<ShopPurchaseResult> TryPurchaseAsync(
            ShopListingSO shopListingSO,
            CancellationToken cancellationToken);
    }
}
