using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace FakeMG.SceneLoading
{
    public sealed class SceneLoadTrigger : MonoBehaviour
    {
        [Required, SerializeField] private AssetReferenceScene _sceneToLoad;
        [Required, SerializeField] private SceneLoader _sceneLoader;
        [SerializeField] private bool _shouldLoadOnStart;
        [SerializeField] private bool _shouldSetActiveAfterLoad = true;

        private StartupSceneLoadRequest _startupSceneLoadRequest;

        #region Unity Lifecycle

        private void Start()
        {
            if (_shouldLoadOnStart)
            {
                LoadAndActivateTargetSceneSafelyAsync(destroyCancellationToken).Forget();
            }
        }

        #endregion

        #region Public Methods

        [Inject]
        public void Construct(IStartupReadiness readiness)
        {
            _startupSceneLoadRequest = new StartupSceneLoadRequest(readiness, _sceneLoader);
        }

        public async UniTask<SceneLoadResult> LoadTargetSceneAsync(CancellationToken cancellationToken = default)
        {
            return await _startupSceneLoadRequest.LoadAsync(_sceneToLoad, cancellationToken);
        }

        #endregion

        #region Private Methods

        private async UniTaskVoid LoadAndActivateTargetSceneSafelyAsync(CancellationToken cancellationToken)
        {
            try
            {
                SceneLoadResult result = await LoadTargetSceneAsync(cancellationToken);
                if (cancellationToken.IsCancellationRequested || result.Status == SceneLoadStatus.Cancelled)
                {
                    Echo.Log("Scene load trigger was cancelled during teardown.");
                    return;
                }

                if (!result.Succeeded)
                {
                    if (result.Status != SceneLoadStatus.StartupFailed)
                    {
                        Echo.Error($"Scene load trigger failed: {result.FailureReason}", context: this);
                    }

                    return;
                }

                if (_shouldSetActiveAfterLoad && !_sceneLoader.SetActiveScene(_sceneToLoad))
                {
                    Echo.Error("Scene load trigger could not activate the loaded scene.", context: this);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Echo.Log("Scene load trigger was cancelled during teardown.");
            }
            catch (Exception exception)
            {
                Echo.Error($"Scene load trigger failed: {exception}", context: this);
            }
        }

        #endregion
    }
}
