using UnityEngine;

namespace FakeMG.GridSystem
{
    public interface IGridOccupantDestroyer
    {
        /// <returns>False when no occupant could be removed at that position.</returns>
        bool DestroyStructure(Vector3 worldPosition);
    }
}
