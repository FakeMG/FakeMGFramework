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

        public UniTask<Sprite> LoadIconAsync(IdentitySO identitySO, CancellationToken cancellationToken)
        {
            if (identitySO == null || identitySO.IconSpriteAsset == null || !identitySO.IconSpriteAsset.RuntimeKeyIsValid())
            {
                Echo.Warning($"Icon cannot load because identity '{identitySO?.name ?? "<missing>"}' has no valid icon reference.");
                return UniTask.FromResult<Sprite>(null);
            }

            return LoadSpriteAsync(identitySO.IconSpriteAsset, cancellationToken);
        }

        public async UniTask<Sprite> LoadSpriteAsync(AssetReferenceSprite spriteReference, CancellationToken cancellationToken)
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

            if (spriteReference == null || !spriteReference.RuntimeKeyIsValid())
            {
                Echo.Warning("Sprite cannot load because its addressable reference is invalid.");
                return null;
            }

            string iconKey = spriteReference.RuntimeKey.ToString();
            if (!_spriteHandles.TryGetValue(iconKey, out AsyncOperationHandle<Sprite> spriteHandle))
            {
                spriteHandle = Addressables.LoadAssetAsync<Sprite>(spriteReference);
                _spriteHandles.Add(iconKey, spriteHandle);
            }

            try
            {
                // Each caller cancels its own wait; the cache retains the shared load for later selections.
                Sprite sprite = await spriteHandle.Task.AsUniTask().AttachExternalCancellation(cancellationToken);
                if (_isDisposed || !_spriteHandles.TryGetValue(iconKey, out AsyncOperationHandle<Sprite> cachedHandle) ||
                    !cachedHandle.Equals(spriteHandle))
                {
                    Echo.Log($"Sprite load for '{spriteReference.AssetGUID}' completed after its cache entry was released.");
                    return null;
                }

                if (spriteHandle.Status != AsyncOperationStatus.Succeeded || sprite == null)
                {
                    Echo.Error($"Sprite failed to load for reference '{spriteReference.AssetGUID}'.");
                    RemoveAndReleaseHandle(iconKey, spriteHandle);
                    return null;
                }

                return sprite;
            }
            catch (OperationCanceledException)
            {
                Echo.Log($"Sprite request for '{spriteReference.AssetGUID}' was canceled; its cached load is retained.");
                return null;
            }
            catch (Exception exception)
            {
                Echo.Error($"Sprite load for '{spriteReference.AssetGUID}' failed with {exception.GetType().Name}: {exception.Message}");
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
