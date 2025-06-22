using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace FakeMG.FakeMGFramework.SceneLoading
{
    public class SceneLoader : MonoBehaviour
    {
        [Header("Scene Settings")]
        [SerializeField] private AssetReference sceneReference;

        [Header("Events")]
        public UnityEvent onSceneLoaded;
        public UnityEvent onSceneUnloaded;
        public UnityEvent<string> onSceneLoadFailed;
        public UnityEvent<string> onSceneUnloadFailed;

        private AsyncOperationHandle<SceneInstance>? _loadedScene;
        private bool _isLoading;
        private bool _isUnloading;

        public bool IsLoading => _isLoading;
        public bool IsUnloading => _isUnloading;
        public bool IsBusy => _isLoading || _isUnloading;
        public bool IsSceneLoaded => _loadedScene.HasValue && _loadedScene.Value.IsValid();
        public AssetReference SceneReference => sceneReference;

        public void LoadSceneAdditive()
        {
            if (IsBusy)
            {
                Debug.LogWarning("SceneLoader is busy. Cannot load scene.");
                return;
            }

            LoadSceneAsync(LoadSceneMode.Additive).Forget();
        }

        public void LoadSceneSingle()
        {
            if (IsBusy)
            {
                Debug.LogWarning("SceneLoader is busy. Cannot load scene.");
                return;
            }

            LoadSceneAsync(LoadSceneMode.Single).Forget();
        }

        public void UnloadScene()
        {
            if (IsBusy)
            {
                Debug.LogWarning("SceneLoader is busy. Cannot unload scene.");
                return;
            }

            UnloadSceneAsync().Forget();
        }

        public void SetActiveScene()
        {
            if (IsSceneLoaded && _loadedScene.HasValue)
            {
                Scene scene = _loadedScene.Value.Result.Scene;
                SceneManager.SetActiveScene(scene);
                Debug.Log($"Set active scene: {scene.name}");
            }
            else
            {
                Debug.LogWarning("Cannot set active scene. Scene is not loaded.");
            }
        }

        private async UniTaskVoid LoadSceneAsync(LoadSceneMode loadMode)
        {
            try
            {
                if (sceneReference == null)
                {
                    Debug.LogError("Scene reference is null.");
                    onSceneLoadFailed?.Invoke("Scene reference is null");
                    return;
                }

                if (IsSceneLoaded)
                {
                    Debug.LogWarning($"Scene {sceneReference.editorAsset?.name} is already loaded.");
                    return;
                }

                _isLoading = true;

                AsyncOperationHandle<SceneInstance> handle = Addressables.LoadSceneAsync(
                    sceneReference,
                    loadMode
                );

                await handle;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    _loadedScene = handle;
                    string sceneName = handle.Result.Scene.name;

                    Debug.Log($"Successfully loaded scene: {sceneName}");
                    onSceneLoaded?.Invoke();
                }
                else
                {
                    string errorMsg = $"Failed to load scene: {sceneReference.editorAsset?.name}";
                    Debug.LogError(errorMsg);
                    onSceneLoadFailed?.Invoke(errorMsg);
                }

                _isLoading = false;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Unexpected error during scene loading: {ex.Message}");
                onSceneLoadFailed?.Invoke($"Unexpected error: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async UniTaskVoid UnloadSceneAsync()
        {
            try
            {
                if (!IsSceneLoaded || !_loadedScene.HasValue)
                {
                    Debug.LogWarning($"Scene {sceneReference.editorAsset?.name} is not loaded or already unloaded.");
                    return;
                }

                _isUnloading = true;

                AsyncOperationHandle<SceneInstance> handle = _loadedScene.Value;
                string sceneName = handle.Result.Scene.name;

                AsyncOperationHandle unloadHandle = Addressables.UnloadSceneAsync(handle);
                await unloadHandle;

                if (unloadHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    _loadedScene = null;
                    Debug.Log($"Successfully unloaded scene: {sceneName}");
                    onSceneUnloaded?.Invoke();
                }
                else
                {
                    string errorMsg = $"Failed to unload scene: {sceneName}";
                    Debug.LogError(errorMsg);
                    onSceneUnloadFailed?.Invoke(errorMsg);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Unexpected error during scene unloading: {ex.Message}");
                onSceneUnloadFailed?.Invoke($"Unexpected error: {ex.Message}");
            }
            finally
            {
                _isUnloading = false;
            }
        }
    
        private void OnDestroy()
        {
            // Clean up any remaining loaded scene
            if (_loadedScene.HasValue && _loadedScene.Value.IsValid())
            {
                Addressables.UnloadSceneAsync(_loadedScene.Value);
            }
            _loadedScene = null;
        }
    }
}