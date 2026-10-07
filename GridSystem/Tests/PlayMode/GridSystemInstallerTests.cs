using System.Collections;
using FakeMG.SceneLoading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VContainer;
using VContainer.Unity;

namespace FakeMG.GridSystem.Tests.PlayMode
{
    public sealed class GridSystemInstallerTests
    {
        private IObjectResolver _container;
        private GridManager _gridManager;
        private Camera _camera;

        #region Unity Lifecycle

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _container?.Dispose();
            if (_gridManager)
            {
                Object.Destroy(_gridManager.gameObject);
            }

            if (_camera)
            {
                Object.Destroy(_camera.gameObject);
            }
            yield return null;
        }

        #endregion

        #region Public Methods

        [Test]
        public void Register_FrameworkTestDependencies_ResolvesPlacementAndProjectionServices()
        {
            GridManager gridManagerPrefab = GridSystemPlayModeTestAssets.LoadPrefabComponent<GridManager>(
                GridSystemPlayModeTestAssets.GRID_MANAGER_PREFAB_GUID);
            Camera cameraPrefab = GridSystemPlayModeTestAssets.LoadPrefabComponent<Camera>(GridSystemPlayModeTestAssets.CAMERA_PREFAB_GUID);
            ContainerBuilder builder = new();
            builder.RegisterComponentInNewPrefab(gridManagerPrefab, Lifetime.Scoped);
            builder.RegisterComponentInNewPrefab(cameraPrefab, Lifetime.Scoped);
            builder.RegisterInstance(new PlacementState());
            GridSystemInstaller.Register(builder, 1 << 8);

            _container = builder.Build();
            _gridManager = _container.Resolve<GridManager>();
            _camera = _container.Resolve<Camera>();
            GridOccupantPlacementService placementService = _container.Resolve<GridOccupantPlacementService>();
            ILoadedSceneDataApplier dataApplier = _container.Resolve<ILoadedSceneDataApplier>();
            GridPointerProjector gridPointerProjector = _container.Resolve<GridPointerProjector>();

            Assert.IsNotNull(placementService);
            Assert.AreSame(placementService, dataApplier);
            Assert.IsNotNull(gridPointerProjector);
        }

        #endregion
    }
}
