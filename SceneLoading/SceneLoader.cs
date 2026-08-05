using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.SaveLoad;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace FakeMG.SceneLoading
{
    /// <summary>
    /// Owns Addressable scene controllers for the manager scope and forwards their lifecycle events.
    /// It awaits registered application prerequisites before loading, then gives every created
    /// controller this manager as the explicit VContainer parent so additive gameplay scenes resolve
    /// initialized shared services without relying on scene discovery or load timing.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField] private DataApplicationManager _dataApplicationManager;

        private readonly Dictionary<string, SceneController> _sceneControllers = new();
        private ISceneLoadContext _sceneLoadContext;
        private IEnumerable<ISceneLoadPrerequisite> _sceneLoadPrerequisites;

        public event Action<AssetReferenceScene> OnSceneLoaded;
        public event Action<AssetReferenceScene> OnSceneUnloaded;
        public event Action<AssetReferenceScene, string> OnSceneLoadFailed;
        public event Action<AssetReferenceScene, string> OnSceneUnloadFailed;

        #region Unity Lifecycle

        private void OnDestroy()
        {
            foreach (SceneController sceneController in _sceneControllers.Values)
            {
                UnsubscribeFromSceneController(sceneController);
            }
        }

        #endregion

        #region Public Methods

        [Inject]
        public void Construct(
            ISceneLoadContext sceneLoadContext, IEnumerable<ISceneLoadPrerequisite> sceneLoadPrerequisites)
        {
            _sceneLoadContext = sceneLoadContext;
            _sceneLoadPrerequisites = sceneLoadPrerequisites;
        }

        public async UniTask LoadSceneAsync(AssetReferenceScene sceneRef, LoadSceneMode mode = LoadSceneMode.Additive)
        {
            await PrepareForSceneLoadAsync(this.GetCancellationTokenOnDestroy());
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

            sceneController = new SceneController(sceneRef, _sceneLoadContext);

            sceneController.OnSceneLoaded += PublishSceneLoaded;
            sceneController.OnSceneUnloaded += PublishSceneUnloaded;
            sceneController.OnSceneLoadFailed += PublishSceneLoadFailure;
            sceneController.OnSceneUnloadFailed += PublishSceneUnloadFailure;

            _sceneControllers[key] = sceneController;
            return sceneController;
        }

        #endregion

        #region Private Methods

        private async UniTask PrepareForSceneLoadAsync(CancellationToken cancellationToken)
        {
            foreach (ISceneLoadPrerequisite prerequisite in _sceneLoadPrerequisites)
            {
                await prerequisite.PrepareForSceneLoadAsync(cancellationToken);
            }
        }

        private void UnsubscribeFromSceneController(SceneController sceneController)
        {
            sceneController.OnSceneLoaded -= PublishSceneLoaded;
            sceneController.OnSceneUnloaded -= PublishSceneUnloaded;
            sceneController.OnSceneLoadFailed -= PublishSceneLoadFailure;
            sceneController.OnSceneUnloadFailed -= PublishSceneUnloadFailure;
        }

        private void PublishSceneLoaded(SceneController sceneController)
        {
            OnSceneLoaded?.Invoke(sceneController.SceneReference);
        }

        private void PublishSceneUnloaded(SceneController sceneController)
        {
            OnSceneUnloaded?.Invoke(sceneController.SceneReference);
        }

        private void PublishSceneLoadFailure(SceneController sceneController, string failureReason)
        {
            OnSceneLoadFailed?.Invoke(sceneController.SceneReference, failureReason);
        }

        private void PublishSceneUnloadFailure(SceneController sceneController, string failureReason)
        {
            OnSceneUnloadFailed?.Invoke(sceneController.SceneReference, failureReason);
        }

        #endregion
    }
}
