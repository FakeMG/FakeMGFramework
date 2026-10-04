using FakeMG.SceneLoading;
using UnityEngine;
using VContainer;

namespace FakeMG.GridSystem
{
    public static class GridSystemInstaller
    {
        #region Public Methods

        public static void Register(IContainerBuilder builder, LayerMask placementLayerMask)
        {
            builder.Register(resolver => CreateStructurePlacementService(resolver), Lifetime.Scoped)
                .AsSelf()
                .As<ILoadedSceneDataApplier>();

            builder.Register(resolver => new GridPointerProjector(
                resolver.Resolve<GridManager>(),
                placementLayerMask,
                resolver.Resolve<Camera>()), Lifetime.Scoped);
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
