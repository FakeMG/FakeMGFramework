using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace FakeMG.SceneLoading
{
    public class SceneController
    {
        private readonly AssetReferenceScene _sceneReference;
        private AsyncOperationHandle<SceneInstance>? _loadedSceneHandle;

        public bool IsLoading { get; private set; }
        public bool IsUnloading { get; private set; }
        public bool IsBusy => IsLoading || IsUnloading;
        public bool IsSceneLoaded => TryGetLoadedSceneHandle(out _);
        public AssetReferenceScene SceneReference => _sceneReference;

        public event Action OnSceneLoaded;
        public event Action OnSceneUnloaded;
        public event Action<string> OnSceneLoadFailed;
        public event Action<string> OnSceneUnloadFailed;

        public SceneController(AssetReferenceScene sceneReference)
        {
            _sceneReference = sceneReference ?? throw new ArgumentNullException(nameof(sceneReference));
        }

        public string GetLoadedSceneName()
        {
            if (!TryGetLoadedSceneHandle(out AsyncOperationHandle<SceneInstance> handle))
            {
                return null;
            }

            return handle.Result.Scene.name;
        }

        public void SetActiveScene()
        {
            if (TryGetLoadedSceneHandle(out AsyncOperationHandle<SceneInstance> handle))
            {
                Scene scene = handle.Result.Scene;
                SceneManager.SetActiveScene(scene);
                Debug.Log($"Set active scene: {scene.name}");
            }
            else
            {
                Debug.LogWarning("Cannot set active scene. Scene is not loaded.");
            }
        }

        public async UniTask LoadSceneAsync(LoadSceneMode loadMode = LoadSceneMode.Additive)
        {
            if (IsBusy)
            {
                Debug.LogWarning("SceneLoader is busy. Cannot load scene.");
                return;
            }

            try
            {
                IsLoading = true;
                await LoadSceneInternalAsync(loadMode);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Unexpected error during scene loading: {ex.Message}");
                OnSceneLoadFailed?.Invoke($"Unexpected error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async UniTask<bool> LoadSceneInternalAsync(LoadSceneMode loadMode)
        {
            if (_sceneReference == null)
            {
                Debug.LogError("Scene reference is null.");
                OnSceneLoadFailed?.Invoke("Scene reference is null");
                return false;
            }

            if (IsSceneLoaded)
            {
                Debug.LogWarning($"Scene {_sceneReference} is already loaded.");
                return true;
            }

            AsyncOperationHandle<SceneInstance> handle =
                Addressables.LoadSceneAsync(_sceneReference, loadMode);

            await handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _loadedSceneHandle = handle;
                string sceneName = handle.Result.Scene.name;

                Debug.Log($"Successfully loaded scene: {sceneName}");
                OnSceneLoaded?.Invoke();
                return true;
            }

            _loadedSceneHandle = null;
            string errorMsg = $"Failed to load scene: {_sceneReference}";
            Debug.LogError(errorMsg);
            OnSceneLoadFailed?.Invoke(errorMsg);
            return false;
        }

        public async UniTask UnloadSceneAsync()
        {
            if (IsBusy)
            {
                Debug.LogWarning("SceneLoader is busy. Cannot unload scene.");
                return;
            }

            try
            {
                IsUnloading = true;
                await UnloadSceneInternalAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Unexpected error during scene unloading: {ex.Message}");
                OnSceneUnloadFailed?.Invoke($"Unexpected error: {ex.Message}");
            }
            finally
            {
                IsUnloading = false;
            }
        }

        private async UniTask<bool> UnloadSceneInternalAsync()
        {
            if (!TryGetLoadedSceneHandle(out AsyncOperationHandle<SceneInstance> handle))
            {
                Debug.LogWarning($"Scene {_sceneReference} is not loaded or already unloaded.");
                return true;
            }

            string sceneName = handle.Result.Scene.name;

            AsyncOperationHandle unloadHandle = Addressables.UnloadSceneAsync(handle, autoReleaseHandle: false);
            await unloadHandle;

            bool unloadSucceeded = unloadHandle.Status == AsyncOperationStatus.Succeeded;
            Addressables.Release(unloadHandle);

            if (unloadSucceeded)
            {
                _loadedSceneHandle = null;
                Debug.Log($"Successfully unloaded scene: {sceneName}");
                OnSceneUnloaded?.Invoke();
                return true;
            }

            string errorMsg = $"Failed to unload scene: {sceneName}";
            Debug.LogError(errorMsg);
            OnSceneUnloadFailed?.Invoke(errorMsg);
            return false;
        }

        public async UniTask ReloadSceneAsync()
        {
            if (IsBusy)
            {
                Debug.LogWarning("SceneLoader is busy. Cannot reload scene.");
                return;
            }

            try
            {
                IsLoading = true;
                IsUnloading = true;

                await ReloadSceneInternalAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Unexpected error during scene reloading: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                IsUnloading = false;
            }
        }

        private async UniTask<bool> ReloadSceneInternalAsync()
        {
            if (_sceneReference == null)
            {
                Debug.LogError("Scene reference is null.");
                OnSceneLoadFailed?.Invoke("Scene reference is null");
                return false;
            }

            if (!IsSceneLoaded)
            {
                Debug.LogWarning($"Scene {_sceneReference} is not loaded. Loading instead.");
                return await LoadSceneInternalAsync(LoadSceneMode.Additive);
            }

            // Unload first
            bool unloadSuccess = await UnloadSceneInternalAsync();
            if (!unloadSuccess) return false;

            // Then reload
            bool loadSuccess = await LoadSceneInternalAsync(LoadSceneMode.Additive);
            if (loadSuccess)
            {
                Debug.Log($"Successfully reloaded scene: {_sceneReference}");
            }

            return loadSuccess;
        }

        private bool TryGetLoadedSceneHandle(out AsyncOperationHandle<SceneInstance> handle)
        {
            handle = default;

            if (!_loadedSceneHandle.HasValue)
            {
                return false;
            }

            handle = _loadedSceneHandle.Value;

            if (!handle.IsValid())
            {
                _loadedSceneHandle = null;
                return false;
            }

            Scene loadedScene = handle.Result.Scene;
            if (!loadedScene.IsValid() || !loadedScene.isLoaded)
            {
                _loadedSceneHandle = null;
                return false;
            }

            return true;
        }
    }
}