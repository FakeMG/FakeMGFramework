using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FakeMG.GridSystem.Tests.PlayMode
{
    /// <summary>
    /// Provides Addressable production prefabs required by GridSystem PlayMode tests.
    /// </summary>
    [CreateAssetMenu(
        fileName = "GridSystemTestAssetConfig",
        menuName = "FakeMG/Testing/Grid System Test Asset Config")]
    public sealed class GridSystemTestAssetConfigSO : ScriptableObject
    {
        [SerializeField] private AssetReferenceGameObject _gridManagerPrefab;
        [SerializeField] private AssetReferenceGameObject _structureFootprintPrefab;

        public AssetReferenceGameObject GridManagerPrefab => _gridManagerPrefab;
        public AssetReferenceGameObject GridFootprintPrefab => _structureFootprintPrefab;
    }
}
