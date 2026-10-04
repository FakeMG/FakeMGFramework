using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Loads visual module assets through Addressables and tracks every handle so they
    /// can all be released when the tutorial ends. Load failures throw; the step decides
    /// whether a failure is fatal (required) or recoverable (optional).
    /// </summary>
    public sealed class TutorialAddressableLoader
    {
        private readonly List<AsyncOperationHandle> _handles = new();

        public async UniTask<TLoaded> LoadAsync<TLoaded>(AssetReferenceT<TLoaded> assetReference,
            CancellationToken cancellationToken) where TLoaded : Object
        {
            AsyncOperationHandle<TLoaded> handle = Addressables.LoadAssetAsync<TLoaded>(assetReference.RuntimeKey);
            _handles.Add(handle);

            return await handle.ToUniTask(cancellationToken: cancellationToken);
        }

        public void ReleaseAll()
        {
            for (int i = 0; i < _handles.Count; i++)
            {
                if (_handles[i].IsValid())
                {
                    Addressables.Release(_handles[i]);
                }
            }

            _handles.Clear();
        }
    }
}
