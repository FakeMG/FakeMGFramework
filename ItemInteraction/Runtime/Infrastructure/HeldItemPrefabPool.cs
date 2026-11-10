using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FakeMG.ItemInteraction
{
    public sealed class HeldItemPrefabPool : IAsyncStartable, IDisposable
    {
        private readonly IInteractionItemCatalog _itemCatalog;
        private readonly IObjectResolver _objectResolver;
        private readonly IHeldItemPrefabProvider _prefabProvider;
        private readonly Dictionary<IUsableItem, IHeldItemPrefabHandle> _prefabHandles = new();
        private bool _isDisposed;

        public bool ArePresentationAssetsReady { get; private set; }
        public event Action OnPresentationAssetsReady;

        public HeldItemPrefabPool(IInteractionItemCatalog itemCatalog, IObjectResolver objectResolver, IHeldItemPrefabProvider prefabProvider)
        {
            _itemCatalog = itemCatalog;
            _objectResolver = objectResolver;
            _prefabProvider = prefabProvider;
        }

        #region Public Methods

        public async UniTask StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                foreach (IInteractionItem item in _itemCatalog.Items)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (_isDisposed)
                    {
                        Echo.Warning("Held-prefab preload stopped because its pool was disposed.");
                        return;
                    }

                    if (item is not IUsableItem usableItem)
                    {
                        continue;
                    }

                    await PreloadAsync(usableItem, cancellationToken);
                }

                if (_isDisposed || cancellationToken.IsCancellationRequested)
                {
                    Echo.Warning("Held-prefab preload completed during teardown; presentation remains unavailable.");
                    return;
                }

                ArePresentationAssetsReady = true;
                OnPresentationAssetsReady?.Invoke();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Echo.Warning("Canceled held-prefab pool initialization.");
                Dispose();
            }
        }

        public GameObject Instantiate(IUsableItem item, Transform parent)
        {
            if (!_prefabHandles.TryGetValue(item, out IHeldItemPrefabHandle handle) || !handle.IsSucceeded)
            {
                Echo.Warning($"Cannot show {item.DisplayName} because its held prefab was not preloaded.");
                return null;
            }

            return _objectResolver.Instantiate(handle.Prefab, parent);
        }

        public void ReleaseInstance(GameObject itemInstance)
        {
            if (itemInstance == null)
            {
                Echo.Warning("Ignored release of a missing held-item instance.");
                return;
            }

            UnityEngine.Object.Destroy(itemInstance);
        }

        public void Dispose()
        {
            _isDisposed = true;
            ArePresentationAssetsReady = false;
            foreach (IHeldItemPrefabHandle handle in _prefabHandles.Values)
            {
                handle.Dispose();
            }

            _prefabHandles.Clear();
        }

        #endregion

        #region Private Methods

        private async UniTask PreloadAsync(IUsableItem item, CancellationToken cancellationToken)
        {
            IHeldItemPrefabHandle handle = null;
            bool hasTransferredOwnership = false;
            try
            {
                handle = await _prefabProvider.LoadAsync(item.HeldPrefabReference, cancellationToken);
                if (_isDisposed || cancellationToken.IsCancellationRequested)
                {
                    Echo.Warning($"Releasing held-prefab preload for {item.DisplayName} because its pool is shutting down.");
                    return;
                }

                if (handle == null || !handle.IsSucceeded)
                {
                    Echo.Error($"Failed to preload held prefab for {item.DisplayName}.");
                    return;
                }

                if (_prefabHandles.ContainsKey(item))
                {
                    Echo.Warning($"Ignored duplicate held-prefab preload for {item.DisplayName}.");
                    return;
                }

                _prefabHandles.Add(item, handle);
                hasTransferredOwnership = true;
            }
            catch (OperationCanceledException)
            {
                Echo.Warning($"Canceled held-prefab preload for {item.DisplayName}.");
                throw;
            }
            catch (Exception exception)
            {
                Echo.Error($"Held-prefab preload for {item.DisplayName} failed: {exception.Message}");
            }
            finally
            {
                if (!hasTransferredOwnership)
                {
                    handle?.Dispose();
                }
            }
        }

        #endregion
    }
}
