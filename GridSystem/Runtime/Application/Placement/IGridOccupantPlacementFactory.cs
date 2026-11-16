using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FakeMG.GridSystem
{
    public interface IGridOccupantPlacementFactory
    {
        #region Public Methods

        UniTask<GridOccupantPlacement> CreateStructureAsync(
            string instanceId,
            StructureSO structureSO,
            Vector3 gridWorldPosition,
            int rotationDegrees,
            CancellationToken cancellationToken,
            string loadFailureMessage,
            IGridOccupantPlacementProcessor placementProcessor = null);

        void DestroyStructure(GridOccupantPlacement structurePlacement);

        #endregion
    }
}
