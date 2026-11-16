using UnityEngine;

namespace FakeMG.GridSystem
{
    public interface IGridOccupantPlacementProcessor
    {
        void Process(GameObject structureInstance);
    }
}
