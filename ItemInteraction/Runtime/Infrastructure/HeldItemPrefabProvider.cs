using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace FakeMG.ItemInteraction
{
    public interface IHeldItemPrefabHandle : IDisposable
    {
        bool IsSucceeded { get; }
        GameObject Prefab { get; }
    }

    public interface IHeldItemPrefabProvider
    {
        UniTask<IHeldItemPrefabHandle> LoadAsync(AssetReferenceGameObject prefabReference,
            CancellationToken cancellationToken);
    }

    public sealed class AddressablesHeldItemPrefabProvider : IHeldItemPrefabProvider
    {
        public async UniTask<IHeldItemPrefabHandle> LoadAsync(AssetReferenceGameObject prefabReference,
            CancellationToken cancellationToken)
        {
            AsyncOperationHandle<GameObject> operationHandle = prefabReference.LoadAssetAsync<GameObject>();
            try
            {
                await operationHandle.ToUniTask(cancellationToken: cancellationToken);
                return new AddressablesHeldItemPrefabHandle(operationHandle);
            }
            catch (OperationCanceledException)
            {
                Release(operationHandle);
                throw;
            }
            catch (Exception exception)
            {
                Echo.Error($"Held-item Addressables load threw {exception.GetType().Name}: {exception.Message}");
                Release(operationHandle);
                return null;
            }
        }

        private static void Release(AsyncOperationHandle<GameObject> operationHandle)
        {
            if (operationHandle.IsValid())
            {
                Addressables.Release(operationHandle);
            }
        }

        private sealed class AddressablesHeldItemPrefabHandle : IHeldItemPrefabHandle
        {
            private AsyncOperationHandle<GameObject> _operationHandle;
            private bool _isDisposed;

            public bool IsSucceeded => !_isDisposed && _operationHandle.Status == AsyncOperationStatus.Succeeded;
            public GameObject Prefab => IsSucceeded ? _operationHandle.Result : null;

            public AddressablesHeldItemPrefabHandle(AsyncOperationHandle<GameObject> operationHandle)
            {
                _operationHandle = operationHandle;
            }

            public void Dispose()
            {
                if (_isDisposed)
                {
                    Echo.Warning("Held-item Addressables handle was released more than once.");
                    return;
                }

                _isDisposed = true;
                Release(_operationHandle);
            }
        }
    }
}
