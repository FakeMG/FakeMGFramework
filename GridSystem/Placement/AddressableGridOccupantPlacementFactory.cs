using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer;
using VContainer.Unity;

namespace FakeMG.GridSystem
{
    /// <summary>
    /// Loads structure prefabs through Addressables and owns matching instance/handle cleanup.
    /// </summary>
    internal sealed class AddressableGridOccupantPlacementFactory : IGridOccupantPlacementFactory
    {
        private readonly bool _enableLogging;
        private readonly UnityEngine.Object _logContext;
        private readonly IObjectResolver _resolver;

        public AddressableGridOccupantPlacementFactory(
            bool enableLogging,
            UnityEngine.Object logContext,
            IObjectResolver resolver)
        {
            _enableLogging = enableLogging;
            _logContext = logContext;
            _resolver = resolver;
        }

        #region Public Methods

        public async UniTask<GridOccupantPlacement> CreateStructureAsync(
            string instanceId,
            StructureSO structureSO,
            Vector3 gridWorldPosition,
            int rotationDegrees,
            CancellationToken cancellationToken,
            string loadFailureMessage,
            IGridOccupantPlacementProcessor placementProcessor = null)
        {
            AsyncOperationHandle<GameObject> structurePrefabHandle = Addressables.LoadAssetAsync<GameObject>(structureSO.StructureAsset);

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

                GameObject structureInstance = _resolver.Instantiate(
                    structurePrefab,
                    gridWorldPosition,
                    Quaternion.Euler(0f, rotationDegrees, 0f));

                placementProcessor?.Process(structureInstance);

                GridOccupantIdentity structureInstanceIdentity =
                    structureInstance.GetComponent<GridOccupantIdentity>();
                if (!structureInstanceIdentity)
                {
                    Echo.Error($"Cannot place structure '{structureSO.Id}' because its prefab is missing {nameof(GridOccupantIdentity)}.", _enableLogging, _logContext);
                    DestroyStructure(structureInstance, structurePrefabHandle);
                    return null;
                }

                GridFootprint structureFootprint = structureInstance.GetComponent<GridFootprint>();
                if (!structureFootprint || !structureFootprint.TryValidate(_logContext))
                {
                    Echo.Error($"Cannot place structure '{structureSO.Id}' because its prefab is missing a valid {nameof(GridFootprint)}.", _enableLogging, _logContext);
                    DestroyStructure(structureInstance, structurePrefabHandle);
                    return null;
                }

                structureInstanceIdentity.Initialize(instanceId);
                NotifyIdentityReceivers(structureInstance, structureInstanceIdentity);

                return new GridOccupantPlacement(
                    instanceId,
                    structureSO,
                    structureInstance,
                    structureFootprint,
                    structurePrefabHandle,
                    gridWorldPosition,
                    rotationDegrees);
            }
            catch (OperationCanceledException)
            {
                ReleasePrefabHandle(structurePrefabHandle);
                throw;
            }
        }

        public void DestroyStructure(GridOccupantPlacement structurePlacement)
        {
            DestroyStructure(structurePlacement.StructureInstance, structurePlacement.StructurePrefabHandle);
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

        #region Private Methods

        private static void NotifyIdentityReceivers(
            GameObject structureInstance,
            GridOccupantIdentity structureInstanceIdentity)
        {
            MonoBehaviour[] behaviours = structureInstance.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IGridOccupantIdentityReceiver identityReceiver)
                {
                    identityReceiver.SetGridOccupantIdentity(structureInstanceIdentity);
                }
            }
        }

        #endregion
    }
}
