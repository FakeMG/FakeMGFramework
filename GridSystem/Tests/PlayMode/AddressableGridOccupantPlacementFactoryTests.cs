using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
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

        #region Public Methods

        [SetUp]
        public void SetUp()
        {
            GridSystemTestAssetConfigSO testAssetConfig = GridSystemPlayModeTestAssets.LoadConfig();
            Assert.IsNotNull(testAssetConfig.FactoryStructureSO);

            _structureSO = testAssetConfig.FactoryStructureSO;
            ContainerBuilder builder = new();
            _container = builder.Build();
            _factory = new AddressableGridOccupantPlacementFactory(false, null, _container);
        }

        [TearDown]
        public void TearDown()
        {
            if (_createdPlacement != null)
            {
                _factory.DestroyStructure(_createdPlacement);
                _createdPlacement = null;
            }

            _container?.Dispose();
        }

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
