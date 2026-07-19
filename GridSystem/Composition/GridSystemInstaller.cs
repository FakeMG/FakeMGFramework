using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FakeMG.GridSystem
{
    /// <summary>
    /// Registers the grid building runtime (placement service, pointer projector and lifecycle entry point)
    /// into a container.
    /// </summary>
    public static class GridSystemInstaller
    {
        #region Public Methods

        public static void Register(IContainerBuilder builder, LayerMask placementLayerMask)
        {
            builder.Register(resolver => CreateStructurePlacementService(resolver), Lifetime.Scoped);

            builder.Register(resolver => new GridPointerProjector(
                resolver.Resolve<GridManager>(),
                placementLayerMask,
                resolver.Resolve<Camera>()), Lifetime.Scoped);

            builder.RegisterEntryPoint<GridSystemLifecycle>();
        }

        #endregion

        #region Private Methods

        private static GridOccupantPlacementService CreateStructurePlacementService(IObjectResolver resolver)
        {
            GridManager gridManager = resolver.Resolve<GridManager>();

            AddressableGridOccupantPlacementFactory structurePlacementFactory = new(true, gridManager, resolver);

            return new GridOccupantPlacementService(
                gridManager,
                resolver.Resolve<PlacementState>(),
                new GridOccupantRegistry(),
                structurePlacementFactory);
        }

        #endregion
    }
}
