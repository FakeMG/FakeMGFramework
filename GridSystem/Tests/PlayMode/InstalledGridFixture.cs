using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace FakeMG.GridSystem.Tests.PlayMode
{
    /// <summary>
    /// The grid as <see cref="GridSystemInstaller"/> composes it, loading its Editor-only structure through real Addressables
    /// handles without entering a production catalog.
    /// </summary>
    internal sealed class InstalledGridFixture
    {
        private const int PLACEMENT_LAYER_MASK = 1 << 8;

        private AsyncOperationHandle<IResourceLocator> _initializationHandle;
        private ResourceLocationMap _locator;
        private IObjectResolver _container;

        public GridManager GridManager { get; private set; }
        public GridOccupantPlacementService Placements { get; private set; }
        public StructureSO StructureSO { get; private set; }
        public ReleaseCountingAssetProvider Provider { get; private set; }

        private InstalledGridFixture()
        {
        }

        #region Public Methods

        public static async UniTask<InstalledGridFixture> CreateAsync()
        {
            var fixture = new InstalledGridFixture();
            await fixture.RegisterStructureLocationAsync();
            fixture.BuildContainer();
            return fixture;
        }

        public async UniTask<GridOccupantPlacement> PlaceAtCellAsync(Vector3Int pivotCell)
        {
            Vector3 worldPositionMeters = GridManager.CellToWorld(pivotCell);
            bool wasPlaced = await Placements.PlaceStructureIfEmptyAsync(StructureSO, worldPositionMeters, null, CancellationToken.None);
            Assert.IsTrue(wasPlaced, $"The fixture structure could not be placed at {pivotCell}.");
            Assert.IsTrue(Placements.TryGetPlacementAtPosition(worldPositionMeters, out GridOccupantPlacement placement));
            return placement;
        }

        public void ResizeFootprint(GridOccupantPlacement placement, BoundsInt cellBounds)
        {
            placement.Footprint.OverrideCellBounds(cellBounds);
            GridManager.RebuildOccupancyIndex(Placements.GetPlacedStructures());
        }

        public async UniTask DisposeAsync()
        {
            Placements.ClearAllStructures();
            _container.Dispose();
            Object.Destroy(GridManager.gameObject);
            await UniTask.NextFrame();

            Addressables.RemoveResourceLocator(_locator);
            Addressables.ResourceManager.ResourceProviders.Remove(Provider);
            Addressables.Release(_initializationHandle);
        }

        #endregion

        #region Private Methods

        private async UniTask RegisterStructureLocationAsync()
        {
            StructureSO = GridSystemPlayModeTestAssets.LoadAsset<StructureSO>(GridSystemPlayModeTestAssets.FACTORY_STRUCTURE_SO_GUID);
            GameObject prefab = GridSystemPlayModeTestAssets.LoadAsset<GameObject>(StructureSO.StructureAsset.AssetGUID);
            _initializationHandle = Addressables.InitializeAsync(false);
            await _initializationHandle.ToUniTask();
            Assert.AreEqual(AsyncOperationStatus.Succeeded, _initializationHandle.Status);

            string providerId = $"FakeMG.GridSystem.Tests.{Guid.NewGuid():N}";
            Provider = new ReleaseCountingAssetProvider();
            Assert.IsTrue(Provider.Initialize(providerId, null));
            Addressables.ResourceManager.ResourceProviders.Add(Provider);
            _locator = new ResourceLocationMap(providerId);
            _locator.Add(StructureSO.StructureAsset.RuntimeKey, new ResourceLocationBase(
                StructureSO.StructureAsset.AssetGUID,
                AssetDatabase.GetAssetPath(prefab),
                providerId,
                typeof(GameObject)));
            Addressables.AddResourceLocator(_locator);
        }

        private void BuildContainer()
        {
            GridManager gridManagerPrefab = GridSystemPlayModeTestAssets.LoadPrefabComponent<GridManager>(
                GridSystemPlayModeTestAssets.GRID_MANAGER_PREFAB_GUID);
            ContainerBuilder builder = new();
            builder.RegisterComponentInNewPrefab(gridManagerPrefab, Lifetime.Scoped);
            builder.RegisterInstance(new PlacementState());
            GridSystemInstaller.Register(builder, PLACEMENT_LAYER_MASK);
            _container = builder.Build();
            GridManager = _container.Resolve<GridManager>();
            Placements = _container.Resolve<GridOccupantPlacementService>();
        }

        #endregion
    }

    /// <summary>Releasing the last handle reaches the provider, which is the public signal that a prefab was not leaked.</summary>
    internal sealed class ReleaseCountingAssetProvider : AssetDatabaseProvider
    {
        public int ReleaseCount { get; private set; }

        public ReleaseCountingAssetProvider() : base(0f)
        {
        }

        #region Public Methods

        public override void Release(IResourceLocation location, object asset)
        {
            ReleaseCount++;
            base.Release(location, asset);
        }

        #endregion
    }
}
