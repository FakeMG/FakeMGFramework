using UnityEngine;

namespace FakeMG.GridSystem
{
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
