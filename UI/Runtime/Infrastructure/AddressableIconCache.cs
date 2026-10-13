using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace FakeMG.Framework.UI
{
    public sealed class AddressableIconCache : IDisposable
    {
        private readonly Dictionary<string, AsyncOperationHandle<Sprite>> _spriteHandles = new();
        private bool _isDisposed;

        #region Public Methods

        public async UniTask<Sprite> LoadIconAsync(IdentitySO identitySO, CancellationToken cancellationToken)
        {
            if (_isDisposed)
            {
                Echo.Warning("Icon cannot load because its cache has been disposed.");
                return null;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                Echo.Log("Icon request was canceled before loading began.");
                return null;
            }

            if (identitySO == null || identitySO.IconSpriteAsset == null || !identitySO.IconSpriteAsset.RuntimeKeyIsValid())
            {
                Echo.Warning($"Icon cannot load because identity '{identitySO?.name ?? "<missing>"}' has no valid icon reference.");
                return null;
            }

            string iconKey = identitySO.IconSpriteAsset.RuntimeKey.ToString();
            if (!_spriteHandles.TryGetValue(iconKey, out AsyncOperationHandle<Sprite> spriteHandle))
            {
                spriteHandle = Addressables.LoadAssetAsync<Sprite>(identitySO.IconSpriteAsset);
                _spriteHandles.Add(iconKey, spriteHandle);
            }

            try
            {
                // Each caller cancels its own wait; the cache retains the shared load for later selections.
                Sprite sprite = await spriteHandle.Task.AsUniTask().AttachExternalCancellation(cancellationToken);
                if (_isDisposed || !_spriteHandles.TryGetValue(iconKey, out AsyncOperationHandle<Sprite> cachedHandle) ||
                    !cachedHandle.Equals(spriteHandle))
                {
                    Echo.Log($"Icon load for '{identitySO.name}' completed after its cache entry was released.");
                    return null;
                }

                if (spriteHandle.Status != AsyncOperationStatus.Succeeded || sprite == null)
                {
                    Echo.Error($"Icon failed to load for identity '{identitySO.name}'.");
                    RemoveAndReleaseHandle(iconKey, spriteHandle);
                    return null;
                }

                return sprite;
            }
            catch (OperationCanceledException)
            {
                Echo.Log($"Icon request for '{identitySO.name}' was canceled; its cached load is retained.");
                return null;
            }
            catch (Exception exception)
            {
                Echo.Error($"Icon load for '{identitySO.name}' failed with {exception.GetType().Name}: {exception.Message}");
                RemoveAndReleaseHandle(iconKey, spriteHandle);
                return null;
            }
        }

        public void Dispose()
        {
            _isDisposed = true;
            Clear();
        }

        public void Clear()
        {
            foreach (AsyncOperationHandle<Sprite> spriteHandle in _spriteHandles.Values)
            {
                if (spriteHandle.IsValid())
                {
                    Addressables.Release(spriteHandle);
                }
            }

            _spriteHandles.Clear();
        }

        #endregion

        #region Private Methods

        private void RemoveAndReleaseHandle(string iconKey, AsyncOperationHandle<Sprite> spriteHandle)
        {
            if (_spriteHandles.TryGetValue(iconKey, out AsyncOperationHandle<Sprite> cachedHandle) && cachedHandle.Equals(spriteHandle))
            {
                _spriteHandles.Remove(iconKey);
                if (spriteHandle.IsValid())
                {
                    Addressables.Release(spriteHandle);
                }
            }
        }

        #endregion
    }
}
