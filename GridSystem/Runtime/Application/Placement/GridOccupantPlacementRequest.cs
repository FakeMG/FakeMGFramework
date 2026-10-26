using UnityEngine;

namespace FakeMG.GridSystem
{
    /// <summary>
    /// One structure to place as part of a batch, such as procedurally generated world content.
    /// </summary>
    public readonly struct GridOccupantPlacementRequest
    {
        public GridOccupantPlacementRequest(StructureSO structureSO, Vector3 worldPosition, int rotationDegrees)
        {
            StructureSO = structureSO;
            WorldPosition = worldPosition;
            RotationDegrees = rotationDegrees;
        }

        public StructureSO StructureSO { get; }
        public Vector3 WorldPosition { get; }
        public int RotationDegrees { get; }
    }
}
