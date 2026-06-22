using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using VContainer;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Spawns a camera controller from an addressable prefab for the duration of a step the
    /// same way visual modules spawn their views: it loads and instantiates the prefab,
    /// injects the controller's scene dependencies, moves the camera, then restores and
    /// despawns it on deactivation. A game that needs no tutorial camera simply leaves this
    /// module off its steps. Blocks completion so the step waits for the move to finish.
    /// </summary>
    [Serializable]
    public sealed class CameraBehaviorModule : ITutorialModule
    {
        [SerializeField] private bool _isRequired;
        [SerializeField] private AssetReferenceT<GameObject> _cameraControllerPrefab;
        [SerializeField] private TutorialCameraSetting _cameraSetting;

        private IObjectResolver _objectResolver;
        private GameObject _instance;
        private ITutorialCameraController _camera;

        public bool BlocksCompletion => true;
        public bool IsRequired => _isRequired;

        [Inject]
        public void Construct(IObjectResolver objectResolver)
        {
            _objectResolver = objectResolver;
        }

        public async UniTask ActivateAsync(TutorialContext context, CancellationToken cancellationToken)
        {
            GameObject prefab = await context.Loader.LoadAsync(_cameraControllerPrefab, cancellationToken);
            _instance = UnityEngine.Object.Instantiate(prefab);
            _camera = _instance.GetComponent<ITutorialCameraController>();

            try
            {
                _objectResolver.Inject(_camera);
                await _camera.MoveToAsync(_cameraSetting, cancellationToken);
            }
            catch
            {
                DestroyInstance();
                throw;
            }
        }

        public async UniTask DeactivateAsync(CancellationToken cancellationToken)
        {
            if (_camera != null)
            {
                await _camera.RestoreAsync(cancellationToken);
            }

            DestroyInstance();
        }

        private void DestroyInstance()
        {
            if (_instance != null)
            {
                UnityEngine.Object.Destroy(_instance);
                _instance = null;
                _camera = null;
            }
        }
    }
}
