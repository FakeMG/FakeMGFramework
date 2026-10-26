using UnityEngine;

namespace FakeMG.GridSystem
{
    /// <summary>Removes a placed occupant from the grid and its saved placement state, freeing its cells.</summary>
    public interface IGridOccupantDestroyer
    {
        /// <returns>False when no occupant could be removed at that position.</returns>
        bool DestroyStructure(Vector3 worldPosition);
    }
}
