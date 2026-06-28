using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace FakeMG.GridBuilding
{
    /// <summary>
    /// Owns one structure placement's stable identity, runtime instance, Addressables handle, and held-position state.
    /// </summary>
    public sealed class StructurePlacement
    {
        internal StructurePlacement(
            string instanceId,
            StructureSO structureSO,
            GameObject structureInstance,
            AsyncOperationHandle<GameObject> structurePrefabHandle)
        {
            InstanceId = instanceId;
            StructureSO = structureSO;
            StructureInstance = structureInstance;
            StructurePrefabHandle = structurePrefabHandle;
        }

        public string InstanceId { get; }
        public StructureSO StructureSO { get; }
        public Vector3 OriginalWorldPosition { get; private set; }
        public Vector3 WorldPosition => StructureInstance.transform.position;
        public Bounds WorldBounds => StructureInstance.GetComponentInChildren<Collider>().bounds;

        internal GameObject StructureInstance { get; }
        internal AsyncOperationHandle<GameObject> StructurePrefabHandle { get; }

        #region Internal Methods

        internal void MarkHeldAtCurrentPosition()
        {
            OriginalWorldPosition = WorldPosition;
            StructureInstance.SetActive(false);
        }

        internal void PlaceAt(Vector3 gridWorldPosition)
        {
            StructureInstance.transform.position = gridWorldPosition;
            StructureInstance.SetActive(true);
        }

        #endregion
    }
}
