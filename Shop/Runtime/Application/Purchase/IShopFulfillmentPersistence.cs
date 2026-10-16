using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Shop.Config;
using FakeMG.Shop.RuntimeData;

namespace FakeMG.Shop.Services.Purchase
{
    /// <summary>
    /// Adapts a game's journal, reward receipts and save boundaries. PendingDeliveries includes only
    /// deliveries whose destination is currently loaded. Receipts must be saved atomically with balances.
    /// </summary>
    public interface IShopFulfillmentPersistence
    {
        IEnumerable<ShopRewardDelivery> PendingDeliveries { get; }
        UniTask<IDisposable> AcquireTransactionAsync(CancellationToken cancellationToken);
        UniTask<bool> PreparePurchaseAsync(ShopListingSO listingSO, CancellationToken cancellationToken);
        UniTask<bool> RemovePurchaseAttemptsAsync(IReadOnlyCollection<string> listingIds, CancellationToken cancellationToken);
        ShopRewardDelivery FindDelivery(string transactionId);
        bool TryCreateDelivery(ShopListingSO listingSO, string transactionId, out ShopRewardDelivery delivery, out string failureReason);
        bool CanDeliver(ShopRewardDelivery delivery, out string failureReason);
        void RemoveDelivery(ShopRewardDelivery delivery);
        void RestoreDelivery(ShopRewardDelivery delivery);
        bool ContainsRewardReceipt(string transactionId);
        void RecordRewardReceipt(string transactionId);
        // The adapter chooses the journal's scope. Exchanges can share the world's save boundary with balances.
        UniTask<bool> SaveJournalAsync(ShopRewardDelivery delivery, CancellationToken cancellationToken);
        UniTask<bool> SaveRewardsAsync(CancellationToken cancellationToken);
    }
}
