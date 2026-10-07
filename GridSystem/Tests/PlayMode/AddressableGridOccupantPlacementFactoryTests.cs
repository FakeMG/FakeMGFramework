using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.TestTools;
using VContainer;

namespace FakeMG.GridSystem.Tests.PlayMode
{
    public sealed class AddressableGridOccupantPlacementFactoryTests
    {
        private const string INSTANCE_ID = "factory-structure";
        private const int ROTATION_DEGREES = 90;

        private IObjectResolver _container;
        private AddressableGridOccupantPlacementFactory _factory;
        private GridOccupantPlacement _createdPlacement;
        private StructureSO _structureSO;
        private AsyncOperationHandle<IResourceLocator> _addressablesInitializationHandle;
        private ResourceLocationMap _fixtureLocator;
        private AssetDatabaseProvider _fixtureProvider;

        #region Unity Lifecycle

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _structureSO = GridSystemPlayModeTestAssets.LoadAsset<StructureSO>(GridSystemPlayModeTestAssets.FACTORY_STRUCTURE_SO_GUID);
            GameObject prefab = GridSystemPlayModeTestAssets.LoadAsset<GameObject>(_structureSO.StructureAsset.AssetGUID);
            _addressablesInitializationHandle = Addressables.InitializeAsync(false);
            yield return _addressablesInitializationHandle;
            Assert.AreEqual(AsyncOperationStatus.Succeeded, _addressablesInitializationHandle.Status);

            // Exercise real Addressables handles while the fixture remains Editor-only and outside production catalogs.
            string providerId = $"FakeMG.GridSystem.Tests.{Guid.NewGuid():N}";
            _fixtureProvider = new AssetDatabaseProvider(0f);
            Assert.IsTrue(_fixtureProvider.Initialize(providerId, null));
            Addressables.ResourceManager.ResourceProviders.Add(_fixtureProvider);
            _fixtureLocator = new ResourceLocationMap(providerId);
            _fixtureLocator.Add(_structureSO.StructureAsset.RuntimeKey, new ResourceLocationBase(
                _structureSO.StructureAsset.AssetGUID,
                AssetDatabase.GetAssetPath(prefab),
                providerId,
                typeof(GameObject)));
            Addressables.AddResourceLocator(_fixtureLocator);

            ContainerBuilder builder = new();
            _container = builder.Build();
            _factory = new AddressableGridOccupantPlacementFactory(false, null, _container);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_createdPlacement != null)
            {
                _factory.DestroyStructure(_createdPlacement);
                _createdPlacement = null;
            }

            _container?.Dispose();
            yield return null;

            if (_fixtureLocator != null)
            {
                Addressables.RemoveResourceLocator(_fixtureLocator);
            }

            if (_fixtureProvider != null)
            {
                Addressables.ResourceManager.ResourceProviders.Remove(_fixtureProvider);
            }

            if (_addressablesInitializationHandle.IsValid())
            {
                Addressables.Release(_addressablesInitializationHandle);
            }
        }

        #endregion

        #region Public Methods

        [Test]
        public async Task CreateStructureAsync_FrameworkTestPrefab_CreatesValidInitializedPlacement()
        {
            Vector3 gridWorldPosition = new(3.5f, 0f, -2.5f);
            IGridOccupantPlacementProcessor placementProcessor =
                Substitute.For<IGridOccupantPlacementProcessor>();

            _createdPlacement = await _factory.CreateStructureAsync(
                INSTANCE_ID,
                _structureSO,
                gridWorldPosition,
                ROTATION_DEGREES,
                CancellationToken.None,
                "Factory integration test failed to load its framework fixture.",
                placementProcessor);

            Assert.IsNotNull(_createdPlacement);
            Assert.AreEqual(INSTANCE_ID, _createdPlacement.InstanceId);
            Assert.AreSame(_structureSO, _createdPlacement.StructureSO);
            Assert.AreEqual(gridWorldPosition, _createdPlacement.WorldPosition);
            Assert.AreEqual(ROTATION_DEGREES, _createdPlacement.RotationDegrees);
            Assert.IsNotNull(_createdPlacement.Footprint);
            Assert.IsTrue(_createdPlacement.Footprint.TryValidate());
            Assert.AreEqual(gridWorldPosition, _createdPlacement.RuntimeInstance.transform.position);
            Assert.Less(
                Quaternion.Angle(
                    Quaternion.Euler(0f, ROTATION_DEGREES, 0f),
                    _createdPlacement.RuntimeInstance.transform.rotation),
                0.01f);
            GridOccupantIdentity identity =
                _createdPlacement.RuntimeInstance.GetComponent<GridOccupantIdentity>();
            Assert.IsNotNull(identity);
            Assert.AreEqual(INSTANCE_ID, identity.InstanceId);
            placementProcessor.Received(1).Process(_createdPlacement.RuntimeInstance);
        }

        [Test]
        public async Task DestroyStructure_CreatedPlacement_DestroysInstanceAndReleasesHandle()
        {
            _createdPlacement = await _factory.CreateStructureAsync(
                INSTANCE_ID,
                _structureSO,
                Vector3.zero,
                0,
                CancellationToken.None,
                "Factory cleanup test failed to load its framework fixture.");
            Assert.IsNotNull(_createdPlacement);
            GameObject runtimeInstance = _createdPlacement.RuntimeInstance;
            AsyncOperationHandle<GameObject> structurePrefabHandle =
                _createdPlacement.StructurePrefabHandle;

            _factory.DestroyStructure(_createdPlacement);
            _createdPlacement = null;
            await UniTask.NextFrame();
            // Addressables retains a reference until its deferred completion callbacks finish.
            await UniTask.WaitUntil(() => !structurePrefabHandle.IsValid(), PlayerLoopTiming.LastPostLateUpdate)
                .Timeout(TimeSpan.FromSeconds(1));

            Assert.IsFalse(runtimeInstance);
            Assert.IsFalse(structurePrefabHandle.IsValid());
        }

        [Test]
        public void CreateStructureAsync_PreCanceledToken_ThrowsCancellationWithoutRuntimeInstance()
        {
            using CancellationTokenSource cancellationTokenSource = new();
            cancellationTokenSource.Cancel();
            int runtimeIdentityCountBefore =
                UnityEngine.Object.FindObjectsByType<GridOccupantIdentity>(FindObjectsSortMode.None).Length;

            Assert.CatchAsync<OperationCanceledException>(async () =>
                await _factory.CreateStructureAsync(
                    INSTANCE_ID,
                    _structureSO,
                    Vector3.zero,
                    0,
                    cancellationTokenSource.Token,
                    "Factory cancellation test failed to load its framework fixture."));

            int runtimeIdentityCountAfter =
                UnityEngine.Object.FindObjectsByType<GridOccupantIdentity>(FindObjectsSortMode.None).Length;
            Assert.AreEqual(runtimeIdentityCountBefore, runtimeIdentityCountAfter);
        }

        #endregion
    }
}
