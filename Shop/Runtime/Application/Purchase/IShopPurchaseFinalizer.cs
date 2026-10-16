using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Shop.Config;
using FakeMG.Shop.RuntimeData;

namespace FakeMG.Shop.Services.Purchase
{
    public interface IShopPurchaseFinalizer
    {
        UniTask<bool> PreparePurchaseAsync(ShopListingSO listingSO, CancellationToken cancellationToken);
        UniTask<ShopPurchaseResult> FinalizePurchaseAsync(
            ShopListingSO listingSO,
            ShopPurchaseResult result,
            CancellationToken cancellationToken);
    }
}
