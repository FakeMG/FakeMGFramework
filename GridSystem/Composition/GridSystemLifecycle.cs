using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

namespace FakeMG.GridSystem
{
    /// <summary>
    /// Drives the grid building runtime lifecycle: restores committed placements when the owning scope
    /// starts and tears down runtime structure instances when the scope is disposed.
    /// </summary>
    public sealed class GridSystemLifecycle : IAsyncStartable, IDisposable
    {
        private readonly GridOccupantPlacementService _placementService;

        public GridSystemLifecycle(GridOccupantPlacementService placementService)
        {
            _placementService = placementService;
        }

        #region Public Methods

        public async UniTask StartAsync(CancellationToken cancellation)
        {
            await _placementService.RestoreCommittedStateAsync(cancellation);
        }

        public void Dispose()
        {
            _placementService.ClearRuntimeStructures(false);
        }

        #endregion
    }
}
