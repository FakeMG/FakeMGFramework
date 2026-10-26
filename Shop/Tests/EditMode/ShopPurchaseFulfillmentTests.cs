using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using FakeMG.Inventory;
using FakeMG.Shop.Config;
using FakeMG.Shop.RuntimeData;
using FakeMG.Shop.Services.Purchase;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace FakeMG.Shop.Tests
{
    public sealed class ShopPurchaseFulfillmentTests
    {
        private readonly List<UnityEngine.Object> _objects = new();
        private readonly Dictionary<string, ShopRewardDelivery> _deliveries = new();
        private readonly HashSet<string> _receipts = new();
        private IShopFulfillmentPersistence _persistence;
        private IShopPurchaseConfirmation _confirmation;
        private InventoryBalanceState _inventory;
        private IdentitySO _gemSO;
        private ShopListingSO _listingSO;
        private FulfillmentTestCatalogSO _catalogSO;
        private ShopPurchaseFinalizationService _granter;
        private ShopPurchaseFulfillmentService _fulfillment;
        private int _confirmationAttemptCount;

        #region Public Methods

        [SetUp]
        public void SetUp()
        {
            _gemSO = Create<IdentitySO>();
            _gemSO.ConfigureIdentityForEditor("gem", "Gem", null);
            _listingSO = Create<ShopListingSO>();
            _listingSO.ConfigureForEditor("gems", null, new List<ItemAmountEntry>(),
                new List<ItemAmountEntry> { new(_gemSO, 30) }, ShopPurchaseType.IAP);
            _catalogSO = Create<FulfillmentTestCatalogSO>();
            _catalogSO.AddListing(_listingSO);
            _inventory = new InventoryBalanceState();
            _granter = new ShopPurchaseFinalizationService(_inventory, new ShopOwnershipStateSaveable(Array.Empty<string>()));
            _persistence = Substitute.For<IShopFulfillmentPersistence>();
            _persistence.AcquireTransactionAsync(Arg.Any<CancellationToken>())
                .Returns(UniTask.FromResult(Substitute.For<IDisposable>()));
            _persistence.PreparePurchaseAsync(Arg.Any<ShopListingSO>(), Arg.Any<CancellationToken>())
                .Returns(UniTask.FromResult(true));
            _persistence.PendingDeliveries.Returns(_deliveries.Values.Where(IsPending));
            _persistence.FindDelivery(Arg.Any<string>()).Returns(call =>
                _deliveries.TryGetValue(call.Arg<string>(), out ShopRewardDelivery delivery) ? delivery : null);
            _persistence.TryCreateDelivery(Arg.Any<ShopListingSO>(), Arg.Any<string>(), out _, out _).Returns(call =>
            {
                var delivery = new ShopRewardDelivery { ListingId = call.Arg<ShopListingSO>().Id, TransactionId = call.ArgAt<string>(1) };
                _deliveries.Add(delivery.TransactionId, delivery);
                call[2] = delivery;
                call[3] = string.Empty;
                return true;
            });
            _persistence.CanDeliver(Arg.Any<ShopRewardDelivery>(), out _).Returns(call => { call[1] = ""; return true; });
            _persistence.ContainsRewardReceipt(Arg.Any<string>()).Returns(call => _receipts.Contains(call.Arg<string>()));
            _persistence.When(store => store.RecordRewardReceipt(Arg.Any<string>())).Do(call => _receipts.Add(call.Arg<string>()));
            _persistence.SaveJournalAsync(Arg.Any<ShopRewardDelivery>(), Arg.Any<CancellationToken>()).Returns(UniTask.FromResult(true));
            _persistence.SaveRewardsAsync(Arg.Any<CancellationToken>()).Returns(UniTask.FromResult(true));
            _confirmation = Substitute.For<IShopPurchaseConfirmation>();
            _confirmationAttemptCount = 0;
            _fulfillment = NewFulfillment();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (UnityEngine.Object instance in _objects) UnityEngine.Object.DestroyImmediate(instance);
            _objects.Clear();
            _deliveries.Clear();
            _receipts.Clear();
        }

        [Test]
        public async Task RepeatedStoreCallback_GrantsAndSavesOnce_ButCanRetryConfirmation()
        {
            await FulfillAndConfirmAsync();
            await FulfillAndConfirmAsync();
            Assert.That(_inventory.GetBalance(_gemSO).ToString(), Is.EqualTo("30"));
            await _persistence.Received(1).SaveRewardsAsync(Arg.Any<CancellationToken>());
            _confirmation.Received(2).ConfirmPurchase("transaction");
        }

        [Test]
        public async Task JournalSaveFailure_DoesNotGrantOrConfirm_RetryRecovers()
        {
            _persistence.SaveJournalAsync(Arg.Any<ShopRewardDelivery>(), Arg.Any<CancellationToken>())
                .Returns(UniTask.FromResult(false), UniTask.FromResult(true));
            Assert.That((await FulfillAndConfirmAsync()).IsSuccess, Is.False);
            Assert.That(_inventory.GetBalance(_gemSO).ToString(), Is.EqualTo("0"));
            _confirmation.DidNotReceive().ConfirmPurchase(Arg.Any<string>());
            Assert.That((await FulfillAndConfirmAsync()).IsFinalized, Is.True);
            Assert.That(_inventory.GetBalance(_gemSO).ToString(), Is.EqualTo("30"));
        }

        [Test]
        public async Task RewardSaveFailure_DoesNotConfirm_RetryDoesNotGrantTwice()
        {
            _persistence.SaveRewardsAsync(Arg.Any<CancellationToken>())
                .Returns(UniTask.FromResult(false), UniTask.FromResult(true));
            Assert.That((await FulfillAndConfirmAsync()).IsSuccess, Is.False);
            _confirmation.DidNotReceive().ConfirmPurchase(Arg.Any<string>());
            Assert.That((await FulfillAndConfirmAsync()).IsSuccess, Is.True);
            Assert.That(_inventory.GetBalance(_gemSO).ToString(), Is.EqualTo("30"));
            _confirmation.Received(1).ConfirmPurchase("transaction");
        }

        [Test]
        public async Task CommitSaveFailure_RetainsReceiptAndRetriesBeforeConfirmation()
        {
            _persistence.SaveJournalAsync(Arg.Any<ShopRewardDelivery>(), Arg.Any<CancellationToken>()).Returns(
                UniTask.FromResult(true), UniTask.FromResult(false), UniTask.FromResult(true));
            Assert.That((await FulfillAndConfirmAsync()).IsSuccess, Is.False);
            Assert.That(_deliveries["transaction"].IsCommitted, Is.False);
            Assert.That(_receipts.Contains("transaction"), Is.True);
            _confirmation.DidNotReceive().ConfirmPurchase(Arg.Any<string>());
            Assert.That((await FulfillAndConfirmAsync()).IsSuccess, Is.True);
            Assert.That(_inventory.GetBalance(_gemSO).ToString(), Is.EqualTo("30"));
        }

        [Test]
        public async Task ConfirmationFailure_RetryDoesNotGrantOrSaveRewardsAgain()
        {
            _confirmation.When(store => store.ConfirmPurchase("transaction")).Do(FailFirstConfirmation);
            Assert.That(async () => await FulfillAndConfirmAsync(), Throws.TypeOf<IOException>());
            Assert.That((await FulfillAndConfirmAsync()).IsSuccess, Is.True);
            Assert.That(_inventory.GetBalance(_gemSO).ToString(), Is.EqualTo("30"));
            await _persistence.Received(1).SaveRewardsAsync(Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task Confirmation_OnlyRunsAfterRewardsAndCommitAreSaved()
        {
            _confirmation.When(store => store.ConfirmPurchase("transaction")).Do(AssertCommittedReceipt);
            Assert.That((await FulfillAndConfirmAsync()).IsFinalized, Is.True);
            await _persistence.Received(1).SaveRewardsAsync(Arg.Any<CancellationToken>());
            await _persistence.Received(2).SaveJournalAsync(Arg.Any<ShopRewardDelivery>(), Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task PendingRewardSave_WaitsBeforeConfirmingStoreOrder()
        {
            var saveCompletion = new UniTaskCompletionSource<bool>();
            _persistence.SaveRewardsAsync(Arg.Any<CancellationToken>()).Returns(saveCompletion.Task);
            UniTask<ShopPurchaseResult> purchase = FulfillAndConfirmAsync();
            _confirmation.DidNotReceive().ConfirmPurchase(Arg.Any<string>());
            saveCompletion.TrySetResult(true);
            Assert.That((await purchase).IsSuccess, Is.True);
            _confirmation.Received(1).ConfirmPurchase("transaction");
        }

        [Test]
        public async Task RestartBeforeRewardSave_RecoversPendingDelivery()
        {
            _deliveries.Add("transaction", new ShopRewardDelivery { ListingId = _listingSO.Id, TransactionId = "transaction" });
            await NewFulfillment().RetryDeliveriesAsync(CancellationToken.None);
            Assert.That(_inventory.GetBalance(_gemSO).ToString(), Is.EqualTo("30"));
            Assert.That(_deliveries["transaction"].IsCommitted, Is.True);
        }

        [Test]
        public async Task RestartAfterRewardSave_PersistedReceiptPreventsSecondGrant()
        {
            _deliveries.Add("transaction", new ShopRewardDelivery { ListingId = _listingSO.Id, TransactionId = "transaction" });
            _receipts.Add("transaction");
            _inventory.Add(_gemSO, 30);
            await NewFulfillment().RetryDeliveriesAsync(CancellationToken.None);
            Assert.That(_inventory.GetBalance(_gemSO).ToString(), Is.EqualTo("30"));
            Assert.That(_deliveries["transaction"].IsCommitted, Is.True);
        }

        [Test]
        public async Task FailedPurchase_DoesNotCreateDeliveryOrGrant()
        {
            ShopPurchaseResult result = await _fulfillment.FinalizePurchaseAsync(_listingSO,
                ShopPurchaseResult.Failed("cancelled"), CancellationToken.None);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(_deliveries, Is.Empty);
            Assert.That(_inventory.GetBalance(_gemSO).ToString(), Is.EqualTo("0"));
        }

        [Test]
        public async Task DuplicateTransactionForDifferentProduct_IsRejected()
        {
            _deliveries.Add("transaction", new ShopRewardDelivery
                { TransactionId = "transaction", ListingId = "other-product", IsCommitted = true });
            UnityEngine.TestTools.LogAssert.Expect(LogType.Error,
                new Regex("Transaction 'transaction' was already recorded for listing 'other-product'."));
            Assert.That((await FulfillAndConfirmAsync()).IsSuccess, Is.False);
            _confirmation.DidNotReceive().ConfirmPurchase(Arg.Any<string>());
        }

        #endregion

        #region Private Methods

        private ShopPurchaseFulfillmentService NewFulfillment() => new(_persistence, _granter, _inventory, _catalogSO);
        private UniTask<ShopPurchaseResult> FulfillAndConfirmAsync() => _fulfillment.FulfillAndConfirmAsync(_listingSO,
            ShopPurchaseResult.Succeeded(_listingSO.GetAllItemsGranted(), "transaction"), _confirmation, CancellationToken.None);
        private static bool IsPending(ShopRewardDelivery delivery) => !delivery.IsCommitted;
        private void FailFirstConfirmation(NSubstitute.Core.CallInfo call)
        {
            if (_confirmationAttemptCount++ == 0) throw new IOException("store unavailable");
        }

        private void AssertCommittedReceipt(NSubstitute.Core.CallInfo call)
        {
            Assert.That(_receipts.Contains("transaction"), Is.True);
            Assert.That(_deliveries["transaction"].IsCommitted, Is.True);
        }

        private T Create<T>() where T : ScriptableObject
        {
            T instance = ScriptableObject.CreateInstance<T>();
            _objects.Add(instance);
            return instance;
        }

        #endregion
    }
}
