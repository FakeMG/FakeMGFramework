using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FakeMG.GridSystem.Tests.PlayMode
{
    /// <summary>
    /// Provides framework-owned Addressable fixtures required by GridSystem PlayMode tests.
    /// </summary>
    [CreateAssetMenu(
        fileName = GridSystemPlayModeTestAssets.CONFIG_RESOURCE_NAME,
        menuName = "FakeMG/Testing/Grid System Test Asset Config")]
    public sealed class GridSystemTestAssetConfigSO : ScriptableObject
    {
        [Header("Framework Test Prefabs")]
        [SerializeField] private AssetReferenceGameObject _gridManagerPrefab;
        [SerializeField] private AssetReferenceGameObject _structureFootprintPrefab;
        [SerializeField] private AssetReferenceGameObject _cameraPrefab;

        [Header("Framework Test Structures")]
        [SerializeField] private StructureSO _projectionStructureSO;
        [SerializeField] private StructureSO _factoryStructureSO;

        public AssetReferenceGameObject GridManagerPrefab => _gridManagerPrefab;
        public AssetReferenceGameObject GridFootprintPrefab => _structureFootprintPrefab;
        public AssetReferenceGameObject CameraPrefab => _cameraPrefab;
        public StructureSO ProjectionStructureSO => _projectionStructureSO;
        public StructureSO FactoryStructureSO => _factoryStructureSO;
    }
}
