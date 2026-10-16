#if UNITY_PURCHASING
using System;
using UnityEngine.Purchasing;

namespace FakeMG.Shop.Services.Purchase
{
    public sealed class UnityIapStoreSubscriber : IDisposable
    {
        private readonly StoreController _store;
        private readonly UnityIapPurchaseService _service;

        public UnityIapStoreSubscriber(StoreController store, UnityIapPurchaseService service)
        {
            _store = store;
            _service = service;
            _store.OnStoreConnected += _service.MarkConnected;
            _store.OnStoreDisconnected += _service.MarkDisconnected;
            _store.OnProductsFetched += _service.CompleteProductFetch;
            _store.OnProductsFetchFailed += _service.FailProductFetch;
            _store.OnPurchasesFetched += _service.RecoverFetchedPurchases;
            _store.OnPurchasesFetchFailed += _service.ReportPurchaseFetchFailure;
            _store.OnPurchasePending += _service.BeginFulfillment;
            _store.OnPurchaseConfirmed += _service.CompleteConfirmation;
            _store.OnPurchaseFailed += _service.FailPurchase;
            _store.OnPurchaseDeferred += _service.DeferPurchase;
        }

        #region Public Methods

        public void Dispose()
        {
            _store.OnStoreConnected -= _service.MarkConnected;
            _store.OnStoreDisconnected -= _service.MarkDisconnected;
            _store.OnProductsFetched -= _service.CompleteProductFetch;
            _store.OnProductsFetchFailed -= _service.FailProductFetch;
            _store.OnPurchasesFetched -= _service.RecoverFetchedPurchases;
            _store.OnPurchasesFetchFailed -= _service.ReportPurchaseFetchFailure;
            _store.OnPurchasePending -= _service.BeginFulfillment;
            _store.OnPurchaseConfirmed -= _service.CompleteConfirmation;
            _store.OnPurchaseFailed -= _service.FailPurchase;
            _store.OnPurchaseDeferred -= _service.DeferPurchase;
        }

        #endregion
    }
}
#endif
