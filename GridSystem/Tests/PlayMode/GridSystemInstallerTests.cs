using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.TestTools;
using VContainer;
using VContainer.Unity;

namespace FakeMG.GridSystem.Tests.PlayMode
{
    public sealed class GridSystemInstallerTests
    {
        private IObjectResolver _container;
        private AsyncOperationHandle<GameObject> _gridManagerPrefabHandle;
        private AsyncOperationHandle<GameObject> _cameraPrefabHandle;

        #region Unity Lifecycle

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            GridSystemTestAssetConfigSO testAssetConfig = GridSystemPlayModeTestAssets.LoadConfig();

            _gridManagerPrefabHandle =
                Addressables.LoadAssetAsync<GameObject>(testAssetConfig.GridManagerPrefab);
            _cameraPrefabHandle =
                Addressables.LoadAssetAsync<GameObject>(testAssetConfig.CameraPrefab);
            yield return _gridManagerPrefabHandle;
            yield return _cameraPrefabHandle;

            Assert.AreEqual(AsyncOperationStatus.Succeeded, _gridManagerPrefabHandle.Status);
            Assert.AreEqual(AsyncOperationStatus.Succeeded, _cameraPrefabHandle.Status);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _container?.Dispose();
            ReleaseHandle(_gridManagerPrefabHandle);
            ReleaseHandle(_cameraPrefabHandle);
            yield return null;
        }

        #endregion

        #region Public Methods

        [Test]
        public void Register_FrameworkTestDependencies_ResolvesPlacementAndProjectionServices()
        {
            GridManager gridManagerPrefab =
                _gridManagerPrefabHandle.Result.GetComponent<GridManager>();
            Camera cameraPrefab =
                _cameraPrefabHandle.Result.GetComponentInChildren<Camera>(true);
            Assert.IsNotNull(gridManagerPrefab);
            Assert.IsNotNull(cameraPrefab);
            ContainerBuilder builder = new();
            builder.RegisterComponentInNewPrefab(gridManagerPrefab, Lifetime.Scoped);
            builder.RegisterComponentInNewPrefab(cameraPrefab, Lifetime.Scoped);
            builder.RegisterInstance(new PlacementState());
            GridSystemInstaller.Register(builder, 1 << 8);

            _container = builder.Build();
            GridOccupantPlacementService placementService =
                _container.Resolve<GridOccupantPlacementService>();
            GridPointerProjector gridPointerProjector =
                _container.Resolve<GridPointerProjector>();

            Assert.IsNotNull(placementService);
            Assert.IsNotNull(gridPointerProjector);
        }

        #endregion

        #region Private Methods

        private static void ReleaseHandle(AsyncOperationHandle<GameObject> assetHandle)
        {
            if (assetHandle.IsValid())
            {
                Addressables.Release(assetHandle);
            }
        }

        #endregion
    }
}
