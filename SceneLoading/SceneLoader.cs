using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FakeMG.Framework.SaveLoad.Advanced;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace FakeMG.Framework.SceneLoading
{
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField] private DataApplicationManager dataApplicationManager;

        private readonly Dictionary<AssetReference, SceneController> _sceneControllers = new();

        public event Action<AssetReference> OnSceneLoaded;
        public event Action<AssetReference> OnSceneUnloaded;
        public event Action<AssetReference, string> OnSceneLoadFailed;
        public event Action<AssetReference, string> OnSceneUnloadFailed;

        public async UniTask LoadSceneAsync(AssetReference sceneRef, LoadSceneMode mode = LoadSceneMode.Additive)
        {
            var loader = GetOrCreateLoader(sceneRef);
            await loader.LoadSceneAsync(mode);
            await dataApplicationManager.ApplyDataForSceneAsync(loader.GetLoadedSceneName());
        }

        public async UniTask UnloadSceneAsync(AssetReference sceneRef)
        {
            if (_sceneControllers.TryGetValue(sceneRef, out var sceneController))
            {
                await sceneController.UnloadSceneAsync();
            }
            else
            {
                Debug.LogWarning($"No SceneLoader found for {sceneRef}");
            }
        }

        public async UniTask ReloadSceneAsync(AssetReference sceneRef)
        {
            if (_sceneControllers.TryGetValue(sceneRef, out var sceneController))
            {
                await sceneController.ReloadSceneAsync();
                await dataApplicationManager.ApplyDataForSceneAsync(sceneController.GetLoadedSceneName());
            }
            else
            {
                Debug.LogWarning($"No SceneLoader found for {sceneRef}");
            }
        }

        public bool IsSceneLoaded(AssetReference sceneRef)
        {
            return _sceneControllers.TryGetValue(sceneRef, out var sceneController) && sceneController.IsSceneLoaded;
        }

        public void SetActiveScene(AssetReference sceneRef)
        {
            if (_sceneControllers.TryGetValue(sceneRef, out var sceneController))
            {
                sceneController.SetActiveScene();
            }
            else
            {
                Debug.LogWarning($"Cannot set active scene. No SceneLoader for {sceneRef}");
            }
        }

        public SceneController GetOrCreateLoader(AssetReference sceneRef)
        {
            if (_sceneControllers.TryGetValue(sceneRef, out var sceneController)) return sceneController;

            sceneController = new SceneController(sceneRef);

            sceneController.OnSceneLoaded += () => OnSceneLoaded?.Invoke(sceneRef);
            sceneController.OnSceneUnloaded += () => OnSceneUnloaded?.Invoke(sceneRef);
            sceneController.OnSceneLoadFailed += msg => OnSceneLoadFailed?.Invoke(sceneRef, msg);
            sceneController.OnSceneUnloadFailed += msg => OnSceneUnloadFailed?.Invoke(sceneRef, msg);

            _sceneControllers[sceneRef] = sceneController;
            return sceneController;
        }
    }
}