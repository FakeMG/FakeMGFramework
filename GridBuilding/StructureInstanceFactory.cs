using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace FakeMG.GridBuilding
{
    /// <summary>
    /// Loads structure prefabs through Addressables and owns matching instance/handle cleanup.
    /// </summary>
    internal sealed class StructureInstanceFactory
    {
        private readonly bool _enableLogging;
        private readonly UnityEngine.Object _logContext;

        public StructureInstanceFactory(
            bool enableLogging,
            UnityEngine.Object logContext)
        {
            _enableLogging = enableLogging;
            _logContext = logContext;
        }

        #region Public Methods

        public async UniTask<StructurePlacement> CreateStructureAsync(
            string instanceId,
            StructureSO structureSO,
            Vector3 gridWorldPosition,
            CancellationToken cancellationToken,
            string loadFailureMessage)
        {
            AsyncOperationHandle<GameObject> structurePrefabHandle =
                Addressables.LoadAssetAsync<GameObject>(structureSO.StructureAsset);

            try
            {
                GameObject structurePrefab = await structurePrefabHandle.ToUniTask(
                    cancellationToken: cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                if (structurePrefabHandle.Status != AsyncOperationStatus.Succeeded || !structurePrefab)
                {
                    Echo.Error(loadFailureMessage, _enableLogging, _logContext);
                    ReleasePrefabHandle(structurePrefabHandle);
                    return null;
                }

                GameObject structureInstance = UnityEngine.Object.Instantiate(
                    structurePrefab,
                    gridWorldPosition,
                    Quaternion.identity);

                return new StructurePlacement(
                    instanceId,
                    structureSO,
                    structureInstance,
                    structurePrefabHandle);
            }
            catch (OperationCanceledException)
            {
                ReleasePrefabHandle(structurePrefabHandle);
                throw;
            }
        }

        public void DestroyStructure(
            StructurePlacement structurePlacement)
        {
            DestroyStructure(
                structurePlacement.StructureInstance,
                structurePlacement.StructurePrefabHandle);
        }

        public void DestroyStructure(
            GameObject structureInstance,
            AsyncOperationHandle<GameObject> structurePrefabHandle)
        {
            if (structureInstance)
            {
                UnityEngine.Object.Destroy(structureInstance);
            }

            ReleasePrefabHandle(structurePrefabHandle);
        }

        public void ReleasePrefabHandle(AsyncOperationHandle<GameObject> structurePrefabHandle)
        {
            if (structurePrefabHandle.IsValid())
            {
                Addressables.Release(structurePrefabHandle);
            }
        }

        #endregion
    }
}
