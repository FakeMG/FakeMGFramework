using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FakeMG.SaveLoad.Tests.PlayMode
{
    [CreateAssetMenu(
        fileName = "SaveLoadTestAssetConfig",
        menuName = "FakeMG/Testing/Save Load Test Asset Config")]
    public sealed class SaveLoadTestAssetConfigSO : ScriptableObject
    {
        [SerializeField] private AssetReferenceGameObject _coreManagersPrefab;

        public AssetReferenceGameObject CoreManagersPrefab => _coreManagersPrefab;
    }
}
