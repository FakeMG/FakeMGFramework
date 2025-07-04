﻿using Cysharp.Threading.Tasks;
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

        public bool IsLoading { get; private set; }

        public bool IsUnloading { get; private set; }

        public bool IsBusy => IsLoading || IsUnloading;
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

        public void ReloadScene()
        {
            if (IsBusy)
            {
                Debug.LogWarning("SceneLoader is busy. Cannot reload scene.");
                return;
            }

            ReloadSceneAsync().Forget();
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
                IsLoading = true;
                await LoadSceneInternalAsync(loadMode);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Unexpected error during scene loading: {ex.Message}");
                onSceneLoadFailed?.Invoke($"Unexpected error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async UniTask<bool> LoadSceneInternalAsync(LoadSceneMode loadMode)
        {
            if (sceneReference == null)
            {
                Debug.LogError("Scene reference is null.");
                onSceneLoadFailed?.Invoke("Scene reference is null");
                return false;
            }

            if (IsSceneLoaded)
            {
                Debug.LogWarning($"Scene {sceneReference.editorAsset?.name} is already loaded.");
                return true;
            }

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
                return true;
            }

            string errorMsg = $"Failed to load scene: {sceneReference.editorAsset?.name}";
            Debug.LogError(errorMsg);
            onSceneLoadFailed?.Invoke(errorMsg);
            return false;
        }

        private async UniTaskVoid UnloadSceneAsync()
        {
            try
            {
                IsUnloading = true;
                await UnloadSceneInternalAsync();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Unexpected error during scene unloading: {ex.Message}");
                onSceneUnloadFailed?.Invoke($"Unexpected error: {ex.Message}");
            }
            finally
            {
                IsUnloading = false;
            }
        }

        private async UniTask<bool> UnloadSceneInternalAsync()
        {
            if (!IsSceneLoaded || !_loadedScene.HasValue)
            {
                Debug.LogWarning($"Scene {sceneReference.editorAsset?.name} is not loaded or already unloaded.");
                return true;
            }

            AsyncOperationHandle<SceneInstance> handle = _loadedScene.Value;
            string sceneName = handle.Result.Scene.name;

            AsyncOperationHandle unloadHandle = Addressables.UnloadSceneAsync(handle);
            await unloadHandle;

            if (unloadHandle.Status == AsyncOperationStatus.Succeeded)
            {
                _loadedScene = null;
                Debug.Log($"Successfully unloaded scene: {sceneName}");
                onSceneUnloaded?.Invoke();
                return true;
            }

            string errorMsg = $"Failed to unload scene: {sceneName}";
            Debug.LogError(errorMsg);
            onSceneUnloadFailed?.Invoke(errorMsg);
            return false;
        }

        private async UniTaskVoid ReloadSceneAsync()
        {
            try
            {
                IsLoading = true;
                IsUnloading = true;
                await ReloadSceneInternalAsync();
            }
            catch (System.Exception ex)
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
            if (sceneReference == null)
            {
                Debug.LogError("Scene reference is null.");
                onSceneLoadFailed?.Invoke("Scene reference is null");
                return false;
            }

            if (!IsSceneLoaded)
            {
                Debug.LogWarning($"Scene {sceneReference.editorAsset?.name} is not loaded. Loading it instead.");
                return await LoadSceneInternalAsync(LoadSceneMode.Additive);
            }

            // First unload the scene
            bool unloadSuccess = await UnloadSceneInternalAsync();
            if (!unloadSuccess)
            {
                return false;
            }

            // Then load it again
            bool loadSuccess = await LoadSceneInternalAsync(LoadSceneMode.Additive);

            string sceneName = sceneReference.editorAsset?.name ?? "Unknown";
            if (loadSuccess)
            {
                Debug.Log($"Successfully reloaded scene: {sceneName}");
            }

            return loadSuccess;
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