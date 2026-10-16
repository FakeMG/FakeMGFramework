using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using FakeMG.Inventory;
using FakeMG.Shop.Config;
using FakeMG.Shop.RuntimeData;

namespace FakeMG.Shop.Services.Purchase
{
    /// <summary>
    /// Sole owner of durable reward fulfillment: journal, charge/grant plus receipt, save rewards,
    /// then commit the journal. A successful finalized result is safe for a store to confirm.
    /// </summary>
    public sealed class ShopPurchaseFulfillmentService : IShopPurchaseFinalizer
    {
        private readonly IShopFulfillmentPersistence _persistence;
        private readonly ShopPurchaseFinalizationService _rewardGranter;
        private readonly IInventoryBalanceRepository _inventory;
        private readonly ShopListingDatabaseSO _catalogSO;

        public ShopPurchaseFulfillmentService(
            IShopFulfillmentPersistence persistence,
            ShopPurchaseFinalizationService rewardGranter,
            IInventoryBalanceRepository inventory,
            ShopListingDatabaseSO catalogSO)
        {
            _persistence = persistence;
            _rewardGranter = rewardGranter;
            _inventory = inventory;
            _catalogSO = catalogSO;
        }

        #region Public Methods

        public async UniTask<bool> PreparePurchaseAsync(ShopListingSO listingSO, CancellationToken cancellationToken)
        {
            using (await _persistence.AcquireTransactionAsync(cancellationToken))
            {
                await RetryDeliveriesUnderTransactionAsync(cancellationToken);
                if (_persistence.PendingDeliveries.Any())
                {
                    Echo.Warning("Purchase blocked while a reward awaits a successful save.");
                    return false;
                }
                return await _persistence.PreparePurchaseAsync(listingSO, cancellationToken);
            }
        }

        public async UniTask<ShopPurchaseResult> FinalizePurchaseAsync(
            ShopListingSO listingSO,
            ShopPurchaseResult result,
            CancellationToken cancellationToken)
        {
            if (!result.IsSuccess || result.IsFinalized)
            {
                return result;
            }
            using (await _persistence.AcquireTransactionAsync(cancellationToken))
            {
                return await DeliverRewardAsync(listingSO, result, cancellationToken);
            }
        }

        public async UniTask RetryDeliveriesAsync(CancellationToken cancellationToken)
        {
            using (await _persistence.AcquireTransactionAsync(cancellationToken))
            {
                await RetryDeliveriesUnderTransactionAsync(cancellationToken);
            }
        }

        public async UniTask<bool> RemovePurchaseAttemptsAsync(IReadOnlyCollection<string> listingIds,
            CancellationToken cancellationToken)
        {
            using (await _persistence.AcquireTransactionAsync(cancellationToken))
            {
                return await _persistence.RemovePurchaseAttemptsAsync(listingIds, cancellationToken);
            }
        }

        public async UniTask<ShopPurchaseResult> FulfillAndConfirmAsync(
            ShopListingSO listingSO,
            ShopPurchaseResult result,
            IShopPurchaseConfirmation confirmation,
            CancellationToken cancellationToken)
        {
            ShopPurchaseResult finalized = await FinalizePurchaseAsync(listingSO, result, cancellationToken);
            if (finalized.IsSuccess && finalized.IsFinalized)
            {
                confirmation.ConfirmPurchase(finalized.TransactionId);
            }
            return finalized;
        }

        #endregion

        #region Private Methods

        private async UniTask RetryDeliveriesUnderTransactionAsync(CancellationToken cancellationToken)
        {
            foreach (ShopRewardDelivery delivery in _persistence.PendingDeliveries.ToArray())
            {
                ShopListingSO listingSO = _catalogSO.GetAssetByID(delivery.ListingId);
                if (listingSO == null)
                {
                    Echo.Error($"Cannot recover reward '{delivery.TransactionId}': listing '{delivery.ListingId}' is missing.");
                    continue;
                }
                await DeliverRewardAsync(listingSO,
                    ShopPurchaseResult.Succeeded(listingSO.GetAllItemsGranted(), delivery.TransactionId), cancellationToken);
            }
        }

        private async UniTask<ShopPurchaseResult> DeliverRewardAsync(
            ShopListingSO listingSO,
            ShopPurchaseResult result,
            CancellationToken cancellationToken)
        {
            string transactionId = string.IsNullOrEmpty(result.TransactionId)
                ? "exchange-" + Guid.NewGuid().ToString("N") : result.TransactionId;
            ShopRewardDelivery delivery = _persistence.FindDelivery(transactionId);
            if (delivery != null && delivery.ListingId != listingSO.Id)
            {
                Echo.Error($"Transaction '{transactionId}' was already recorded for listing '{delivery.ListingId}'.");
                return ShopPurchaseResult.Failed("Transaction does not match the recorded product.");
            }

            if (delivery?.IsCommitted == true)
            {
                // Also lets persistence retire a committed delivery from an older journal scope without granting again.
                if (!await _persistence.SaveJournalAsync(delivery, cancellationToken))
                {
                    return ShopPurchaseResult.Failed("Reward saved. Delivery confirmation pending.");
                }
                return ShopPurchaseResult.Succeeded(result.GrantedItems, transactionId, true);
            }

            if (delivery == null && !_persistence.TryCreateDelivery(listingSO, transactionId, out delivery, out string createFailure))
            {
                return ShopPurchaseResult.Failed(createFailure);
            }

            if (!_persistence.CanDeliver(delivery, out string destinationFailure))
            {
                return ShopPurchaseResult.Failed(destinationFailure);
            }

            if (!await _persistence.SaveJournalAsync(delivery, cancellationToken))
            {
                return ShopPurchaseResult.Failed("Reward pending. Could not save the delivery record.");
            }

            if (!_persistence.ContainsRewardReceipt(transactionId))
            {
                if (listingSO.PurchaseType == ShopPurchaseType.InGameCurrency && !_inventory.TrySpend(listingSO.GetPrice()))
                {
                    Echo.Warning($"Unpaid exchange '{transactionId}' cancelled: insufficient currency in the loaded save.");
                    _persistence.RemoveDelivery(delivery);
                    if (!await _persistence.SaveJournalAsync(delivery, cancellationToken))
                    {
                        _persistence.RestoreDelivery(delivery);
                    }
                    return ShopPurchaseResult.Failed("Not enough currency.");
                }
                _rewardGranter.FinalizeSuccessfulPurchase(listingSO, result);
                _persistence.RecordRewardReceipt(transactionId);
            }

            if (!await _persistence.SaveRewardsAsync(cancellationToken))
            {
                return ShopPurchaseResult.Failed("Reward pending save. Reopen the shop to retry.");
            }

            delivery.IsCommitted = true;

            if (!await _persistence.SaveJournalAsync(delivery, cancellationToken))
            {
                delivery.IsCommitted = false;
                return ShopPurchaseResult.Failed("Reward saved. Delivery confirmation pending.");
            }

            return ShopPurchaseResult.Succeeded(result.GrantedItems, transactionId, true);
        }

        #endregion
    }
}
