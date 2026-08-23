using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FakeMG.SceneLoading
{
    public class SceneLoadTrigger : MonoBehaviour
    {
        [Required, SerializeField] private AssetReferenceScene _sceneToLoad;
        [Required, SerializeField] private SceneLoader _sceneLoader;
        [SerializeField] private bool _loadOnStart;
        [SerializeField] private float _delayBeforeLoadSeconds;
        [SerializeField] private bool _setActiveAfterLoad = true;

        private CancellationTokenSource _lifetimeCancellationSource;

        #region Unity Lifecycle

        private void Awake()
        {
            _lifetimeCancellationSource = new CancellationTokenSource();
        }

        private void Start()
        {
            if (_loadOnStart)
            {
                LoadTargetSceneSafelyAsync(_lifetimeCancellationSource.Token).Forget();
            }
        }

        private void OnDestroy()
        {
            _lifetimeCancellationSource.Cancel();
            _lifetimeCancellationSource.Dispose();
            _lifetimeCancellationSource = null;
        }

        #endregion

        #region Public Methods

        public async UniTask<SceneLoadResult> LoadTargetSceneAsync(CancellationToken cancellationToken = default)
        {
            if (_delayBeforeLoadSeconds > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_delayBeforeLoadSeconds), cancellationToken: cancellationToken);
            }

            SceneLoadResult result = await _sceneLoader.LoadSceneAsync(_sceneToLoad, cancellationToken: cancellationToken);

            if (result.Succeeded && _setActiveAfterLoad)
            {
                _sceneLoader.SetActiveScene(_sceneToLoad);
            }

            return result;
        }

        #endregion

        #region Private Methods

        private async UniTaskVoid LoadTargetSceneSafelyAsync(CancellationToken cancellationToken)
        {
            try
            {
                SceneLoadResult result = await LoadTargetSceneAsync(cancellationToken);
                if (!result.Succeeded)
                {
                    Echo.Error($"Scene load trigger failed: {result.FailureReason}", context: this);
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
