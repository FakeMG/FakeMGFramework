using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Shared behavior for visual modules: load the addressable visual prefab,
    /// instantiate it under the tutorial visual root, configure its content, then animate
    /// it in. If configuration or the show animation fails the instance is destroyed and
    /// the failure propagates so the step can skip (required) or continue (optional).
    /// Visual modules block completion so the step waits for the show animation to finish.
    /// </summary>
    [Serializable]
    public abstract class TutorialVisualModuleBase<TView> : ITutorialModule where TView : TutorialVisualView
    {
        [SerializeField] private bool _isRequired;
        [SerializeField] private AssetReferenceT<GameObject> _visualPrefab;

        private GameObject _instance;
        private TView _view;
        private UniTaskCompletionSource _hideCompletionSource;
        private bool _hasStartedHiding;

        public bool BlocksCompletion => true;
        public bool IsRequired => _isRequired;

        public async UniTask ActivateAsync(TutorialContext context, CancellationToken cancellationToken)
        {
            GameObject prefab = await context.Loader.LoadAsync(_visualPrefab, cancellationToken);
            _instance = UnityEngine.Object.Instantiate(prefab, context.VisualRoot);
            _view = _instance.GetComponent<TView>();
            _hasStartedHiding = false;
            _hideCompletionSource = new UniTaskCompletionSource();

            try
            {
                ConfigureView(_view, context);
                await _view.ShowAsync(cancellationToken);
            }
            catch
            {
                OnBeforeViewDestroyed();
                DestroyInstance();
                throw;
            }
        }

        public async UniTask DeactivateAsync(CancellationToken cancellationToken)
        {
            OnBeforeViewDestroyed();
            await HideViewAsync(cancellationToken);

            DestroyInstance();
        }

        protected abstract void ConfigureView(TView view, TutorialContext context);

        protected virtual void OnBeforeViewDestroyed()
        {
        }

        protected UniTask HideViewAsync(CancellationToken cancellationToken)
        {
            if (_view == null)
            {
                return UniTask.CompletedTask;
            }

            if (_hasStartedHiding)
            {
                return _hideCompletionSource.Task;
            }

            _hasStartedHiding = true;
            HideViewAndCompleteAsync(cancellationToken).Forget();
            return _hideCompletionSource.Task;
        }

        /// <summary>
        /// Resolves a UI target by key to its RectTransform, throwing if it is not
        /// registered (e.g. its popup is not open) or has no RectTransform.
        /// </summary>
        protected static RectTransform ResolveTargetRect(TutorialContext context, TutorialTargetKeySO key)
        {
            if (!context.TargetRegistry.TryGet(key, out ITutorialTarget target))
            {
                // TODO: we should not throw.
                throw new InvalidOperationException(
                    $"Tutorial target '{(key == null ? "<none>" : key.name)}' is not registered. Is its UI active?");
            }

            if (target.InteractionTransform is RectTransform rect)
            {
                return rect;
            }

            // TODO: we should not throw.
            throw new InvalidOperationException(
                $"Tutorial target '{key.name}' has no RectTransform to position against.");
        }

        private void DestroyInstance()
        {
            if (_instance != null)
            {
                UnityEngine.Object.Destroy(_instance);
                _instance = null;
                _view = null;
            }
        }

        private async UniTaskVoid HideViewAndCompleteAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _view.HideAsync(cancellationToken);
                _hideCompletionSource.TrySetResult();
            }
            catch (OperationCanceledException exception)
            {
                _hideCompletionSource.TrySetCanceled(exception.CancellationToken);
            }
            catch (Exception exception)
            {
                _hideCompletionSource.TrySetException(exception);
            }
        }
    }
}
