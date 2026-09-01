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
    public sealed class GridPointerProjectorTests
    {
        private const int PROJECTION_LAYER = 8;
        private const float CAMERA_HEIGHT_METERS = 10f;

        private IObjectResolver _container;
        private AsyncOperationHandle<GameObject> _gridManagerPrefabHandle;
        private AsyncOperationHandle<GameObject> _cameraPrefabHandle;
        private AsyncOperationHandle<GameObject> _projectionSurfacePrefabHandle;
        private GridManager _gridManager;
        private Camera _camera;
        private GridFootprint _projectionSurfaceFootprint;
        private Collider _projectionSurfaceCollider;

        #region Unity Lifecycle

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            GridSystemTestAssetConfigSO testAssetConfig = GridSystemPlayModeTestAssets.LoadConfig();
            Assert.IsNotNull(testAssetConfig.ProjectionStructureSO);

            _gridManagerPrefabHandle =
                Addressables.LoadAssetAsync<GameObject>(testAssetConfig.GridManagerPrefab);
            _cameraPrefabHandle =
                Addressables.LoadAssetAsync<GameObject>(testAssetConfig.CameraPrefab);
            _projectionSurfacePrefabHandle =
                Addressables.LoadAssetAsync<GameObject>(testAssetConfig.ProjectionStructureSO.StructureAsset);

            yield return _gridManagerPrefabHandle;
            yield return _cameraPrefabHandle;
            yield return _projectionSurfacePrefabHandle;

            Assert.AreEqual(AsyncOperationStatus.Succeeded, _gridManagerPrefabHandle.Status);
            Assert.AreEqual(AsyncOperationStatus.Succeeded, _cameraPrefabHandle.Status);
            Assert.AreEqual(AsyncOperationStatus.Succeeded, _projectionSurfacePrefabHandle.Status);

            GridManager gridManagerPrefab = _gridManagerPrefabHandle.Result.GetComponent<GridManager>();
            Camera cameraPrefab = _cameraPrefabHandle.Result.GetComponentInChildren<Camera>(true);
            GridFootprint projectionSurfaceFootprintPrefab =
                _projectionSurfacePrefabHandle.Result.GetComponent<GridFootprint>();
            Assert.IsNotNull(gridManagerPrefab);
            Assert.IsNotNull(cameraPrefab);
            Assert.IsNotNull(projectionSurfaceFootprintPrefab);

            ContainerBuilder builder = new();
            builder.RegisterComponentInNewPrefab(gridManagerPrefab, Lifetime.Scoped);
            builder.RegisterComponentInNewPrefab(cameraPrefab, Lifetime.Scoped);
            builder.RegisterComponentInNewPrefab(projectionSurfaceFootprintPrefab, Lifetime.Scoped);
            _container = builder.Build();

            _gridManager = _container.Resolve<GridManager>();
            _camera = _container.Resolve<Camera>();
            _projectionSurfaceFootprint = _container.Resolve<GridFootprint>();
            _projectionSurfaceCollider =
                _projectionSurfaceFootprint.GetComponentInChildren<Collider>(true);
            Assert.IsNotNull(_projectionSurfaceCollider);

            SetLayerRecursively(_projectionSurfaceFootprint.gameObject, PROJECTION_LAYER);
            AlignProjectionSurfaceTopToWorldZero();
            AimCameraDownAtProjectionSurface(CAMERA_HEIGHT_METERS);
            Physics.SyncTransforms();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _container?.Dispose();

            DestroyRuntimeObject(_gridManager);
            DestroyRuntimeObject(_camera);
            DestroyRuntimeObject(_projectionSurfaceFootprint);

            ReleaseHandle(_gridManagerPrefabHandle);
            ReleaseHandle(_cameraPrefabHandle);
            ReleaseHandle(_projectionSurfacePrefabHandle);

            yield return null;
        }

        #endregion

        #region Public Methods

        [Test]
        public void TryGetGridWorldPosition_ValidSurfaceHit_ReturnsSnappedPositionAndHit()
        {
            GridPointerProjector projector = new(
                _gridManager,
                1 << PROJECTION_LAYER,
                _camera);

            bool wasProjected = projector.TryGetGridWorldPosition(
                GetProjectionSurfaceScreenPoint(),
                out Vector3 gridWorldPosition,
                out RaycastHit hitInfo);

            Assert.IsTrue(wasProjected);
            Assert.AreSame(_projectionSurfaceCollider, hitInfo.collider);
            Assert.AreEqual(Vector3.up, hitInfo.normal);
            Assert.AreEqual(
                _gridManager.WorldToGridWorld(hitInfo.point - hitInfo.normal * 0.01f + hitInfo.normal),
                gridWorldPosition);
        }

        [Test]
        public void TryGetGridWorldPosition_CameraPointsAwayFromSurface_ReturnsFalseAndZero()
        {
            _camera.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            Physics.SyncTransforms();
            GridPointerProjector projector = new(
                _gridManager,
                1 << PROJECTION_LAYER,
                _camera);

            bool wasProjected = projector.TryGetGridWorldPosition(
                new Vector2(_camera.pixelWidth * 0.5f, _camera.pixelHeight * 0.5f),
                out Vector3 gridWorldPosition,
                out _);

            Assert.IsFalse(wasProjected);
            Assert.AreEqual(Vector3.zero, gridWorldPosition);
        }

        [Test]
        public void TryGetGridWorldPosition_SurfaceOutsideLayerMask_ReturnsFalseAndZero()
        {
            GridPointerProjector projector = new(_gridManager, 0, _camera);

            bool wasProjected = projector.TryGetGridWorldPosition(
                GetProjectionSurfaceScreenPoint(),
                out Vector3 gridWorldPosition,
                out _);

            Assert.IsFalse(wasProjected);
            Assert.AreEqual(Vector3.zero, gridWorldPosition);
        }

        [Test]
        public void TryGetGridWorldPosition_SurfaceBeyondRaycastLimit_ReturnsFalseAndZero()
        {
            AimCameraDownAtProjectionSurface(101f);
            Physics.SyncTransforms();
            GridPointerProjector projector = new(
                _gridManager,
                1 << PROJECTION_LAYER,
                _camera);

            bool wasProjected = projector.TryGetGridWorldPosition(
                GetProjectionSurfaceScreenPoint(),
                out Vector3 gridWorldPosition,
                out _);

            Assert.IsFalse(wasProjected);
            Assert.AreEqual(Vector3.zero, gridWorldPosition);
        }

        #endregion

        #region Private Methods

        private void AlignProjectionSurfaceTopToWorldZero()
        {
            _projectionSurfaceFootprint.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            Physics.SyncTransforms();
            float verticalOffsetMeters = -_projectionSurfaceCollider.bounds.max.y;
            _projectionSurfaceFootprint.transform.position += Vector3.up * verticalOffsetMeters;
        }

        private void AimCameraDownAtProjectionSurface(float cameraHeightMeters)
        {
            Vector3 surfaceCenter = _projectionSurfaceCollider.bounds.center;
            _camera.transform.SetPositionAndRotation(
                new Vector3(surfaceCenter.x, cameraHeightMeters, surfaceCenter.z),
                Quaternion.Euler(90f, 0f, 0f));
        }

        private Vector2 GetProjectionSurfaceScreenPoint()
        {
            Vector3 surfaceTopCenter = new(
                _projectionSurfaceCollider.bounds.center.x,
                _projectionSurfaceCollider.bounds.max.y,
                _projectionSurfaceCollider.bounds.center.z);
            return _camera.WorldToScreenPoint(surfaceTopCenter);
        }

        private static void SetLayerRecursively(GameObject gameObject, int layer)
        {
            gameObject.layer = layer;
            foreach (Transform child in gameObject.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private static void DestroyRuntimeObject(Component component)
        {
            if (component)
            {
                Object.Destroy(component.gameObject);
            }
        }

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
