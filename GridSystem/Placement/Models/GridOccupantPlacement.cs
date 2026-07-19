using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace FakeMG.GridSystem
{
    /// <summary>
    /// Owns one structure placement's stable identity, runtime instance, footprint, Addressables handle, and held-position state.
    /// </summary>
    public sealed class GridOccupantPlacement
    {
        internal GridOccupantPlacement(
            string instanceId,
            StructureSO structureSO,
            GameObject structureInstance,
            GridFootprint footprint,
            AsyncOperationHandle<GameObject> structurePrefabHandle,
            Vector3 gridWorldPosition,
            int rotationDegrees)
        {
            InstanceId = instanceId;
            StructureSO = structureSO;
            StructureInstance = structureInstance;
            Footprint = footprint;
            StructurePrefabHandle = structurePrefabHandle;
            WorldPosition = gridWorldPosition;
            RotationDegrees = rotationDegrees;
        }

        public string InstanceId { get; }
        public StructureSO StructureSO { get; }
        public GridFootprint Footprint { get; }
        public Vector3 OriginalWorldPosition { get; private set; }

        // Bottom-center anchor of the pivot cell. The instance transform sits at this exact position.
        public Vector3 WorldPosition { get; private set; }

        public int RotationDegrees { get; private set; }
        public GameObject RuntimeInstance => StructureInstance;

        internal GameObject StructureInstance { get; }
        internal AsyncOperationHandle<GameObject> StructurePrefabHandle { get; }

        #region Internal Methods

        internal void MarkHeldAtCurrentPosition()
        {
            OriginalWorldPosition = WorldPosition;
            StructureInstance.SetActive(false);
        }

        internal void PlaceAt(Vector3 gridWorldPosition, int rotationDegrees)
        {
            WorldPosition = gridWorldPosition;
            RotationDegrees = rotationDegrees;
            StructureInstance.transform.SetPositionAndRotation(
                gridWorldPosition,
                Quaternion.Euler(0f, rotationDegrees, 0f));
            StructureInstance.SetActive(true);
        }

        #endregion
    }
}
