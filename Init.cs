using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace FakeMG.Framework
{
    public class Init : MonoBehaviour
    {
        [SerializeField] private AssetReference sceneAssetReferences;

        private void Start()
        {
            Addressables.LoadSceneAsync(sceneAssetReferences).Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Failed)
                {
                    Debug.LogError("Failed to load scene: " + handle.OperationException);
                }
            };
        }
    }
}