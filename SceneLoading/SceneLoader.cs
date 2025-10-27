using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FakeMG.Framework.SaveLoad.Advanced;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FakeMG.Framework.SceneLoading
{
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField] private DataApplicationManager _dataApplicationManager;

        private readonly Dictionary<string, SceneController> _sceneControllers = new();

        public event Action<AssetReferenceScene> OnSceneLoaded;
        public event Action<AssetReferenceScene> OnSceneUnloaded;
        public event Action<AssetReferenceScene, string> OnSceneLoadFailed;
        public event Action<AssetReferenceScene, string> OnSceneUnloadFailed;

        public async UniTask LoadSceneAsync(AssetReferenceScene sceneRef, LoadSceneMode mode = LoadSceneMode.Additive)
        {
            var loader = GetOrCreateLoader(sceneRef);
            await loader.LoadSceneAsync(mode);
            await _dataApplicationManager.ApplyDataForSceneAsync(loader.GetLoadedSceneName());
        }

        public async UniTask UnloadSceneAsync(AssetReferenceScene sceneRef)
        {
            var key = sceneRef.AssetGUID;
            if (_sceneControllers.TryGetValue(key, out var sceneController))
            {
                await sceneController.UnloadSceneAsync();
            }
            else
            {
                Debug.LogWarning($"No SceneLoader found for {sceneRef}");
            }
        }

        public async UniTask ReloadSceneAsync(AssetReferenceScene sceneRef)
        {
            var key = sceneRef.AssetGUID;
            if (_sceneControllers.TryGetValue(key, out var sceneController))
            {
                await sceneController.ReloadSceneAsync();
                await _dataApplicationManager.ApplyDataForSceneAsync(sceneController.GetLoadedSceneName());
            }
            else
            {
                Debug.LogWarning($"No SceneLoader found for {sceneRef}");
            }
        }

        public bool IsSceneLoaded(AssetReferenceScene sceneRef)
        {
            var key = sceneRef.AssetGUID;
            return _sceneControllers.TryGetValue(key, out var sceneController) && sceneController.IsSceneLoaded;
        }

        public void SetActiveScene(AssetReferenceScene sceneRef)
        {
            var key = sceneRef.AssetGUID;
            if (_sceneControllers.TryGetValue(key, out var sceneController))
            {
                sceneController.SetActiveScene();
            }
            else
            {
                Debug.LogWarning($"Cannot set active scene. No SceneLoader for {sceneRef}");
            }
        }

        public SceneController GetOrCreateLoader(AssetReferenceScene sceneRef)
        {
            var key = sceneRef.AssetGUID;
            if (_sceneControllers.TryGetValue(key, out var sceneController)) return sceneController;

            sceneController = new SceneController(sceneRef);

            sceneController.OnSceneLoaded += () => OnSceneLoaded?.Invoke(sceneRef);
            sceneController.OnSceneUnloaded += () => OnSceneUnloaded?.Invoke(sceneRef);
            sceneController.OnSceneLoadFailed += msg => OnSceneLoadFailed?.Invoke(sceneRef, msg);
            sceneController.OnSceneUnloadFailed += msg => OnSceneUnloadFailed?.Invoke(sceneRef, msg);

            _sceneControllers[key] = sceneController;
            return sceneController;
        }
    }
}