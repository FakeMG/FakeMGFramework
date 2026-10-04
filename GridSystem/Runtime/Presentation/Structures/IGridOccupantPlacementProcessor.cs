using UnityEngine;

namespace FakeMG.GridSystem
{
    /// <summary>
    /// Extension point for configuring a loaded structure instance before grid validation and registration.
    /// </summary>
    public interface IGridOccupantPlacementProcessor
    {
        void Process(GameObject structureInstance);
    }
}
