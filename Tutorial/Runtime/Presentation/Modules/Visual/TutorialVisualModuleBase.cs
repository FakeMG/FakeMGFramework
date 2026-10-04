using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Shared behavior for visual modules: load the addressable visual prefab,
    /// instantiate it under the default tutorial visual root or under a tutorial target
    /// chosen per module, configure its content, then animate it in. Expected failures
    /// (missing parent target, failed configuration) are reported by returning false so
    /// the step can skip (required) or continue (optional) without exceptions.
    /// Visual modules block completion so the step waits for the show animation to finish.
    /// </summary>
    [Serializable]
    public abstract class TutorialVisualModuleBase<TView> : ITutorialModule where TView : TutorialVisualView
    {
        [SerializeField] private bool _isRequired;
        [SerializeField] private AssetReferenceT<GameObject> _visualPrefab;
        [Tooltip("Optional. Parents the visual under this target's RectTransform instead of the default tutorial visual root.")]
        [SerializeField] private TutorialTargetKeySO _parentTargetKey;

        private GameObject _instance;
        private TView _view;
        private UniTaskCompletionSource _hideCompletionSource;
        private bool _hasStartedHiding;

        public bool BlocksCompletion => true;
        public bool IsRequired => _isRequired;

        /// <summary>
        /// The RectTransform the visual instance is parented under, and therefore the
        /// coordinate space subclasses must position against. Set during activation.
        /// </summary>
        protected RectTransform ParentRect { get; private set; }

        public async UniTask<bool> ActivateAsync(TutorialContext context, CancellationToken cancellationToken)
        {
            if (!TryResolveParent(context, out RectTransform parent))
            {
                return false;
            }

            ParentRect = parent;

            GameObject prefab = await context.Loader.LoadAsync(_visualPrefab, cancellationToken);
            _instance = UnityEngine.Object.Instantiate(prefab, parent);
            _view = _instance.GetComponent<TView>();
            _hasStartedHiding = false;
            _hideCompletionSource = new UniTaskCompletionSource();

            if (!ConfigureView(_view, context))
            {
                OnBeforeViewDestroyed();
                DestroyInstance();
                return false;
            }

            try
            {
                await _view.ShowAsync(cancellationToken);
            }
            catch
            {
                OnBeforeViewDestroyed();
                DestroyInstance();
                throw;
            }

            return true;
        }

        public async UniTask DeactivateAsync(CancellationToken cancellationToken)
        {
            OnBeforeViewDestroyed();
            await HideViewAsync(cancellationToken);

            DestroyInstance();
        }

        /// <summary>
        /// Configures the freshly instantiated view. Return false (after logging why) to
        /// fail activation; the instance is destroyed and the step reacts per IsRequired.
        /// </summary>
        protected abstract bool ConfigureView(TView view, TutorialContext context);

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
        /// Resolves a UI target by key to its RectTransform. Logs and returns false when
        /// it is not registered (e.g. its popup is not open) or has no RectTransform.
        /// </summary>
        protected static bool TryResolveTargetRect(TutorialContext context, TutorialTargetKeySO key,
            out RectTransform targetRect)
        {
            targetRect = null;

            if (!context.TargetRegistry.TryGet(key, out ITutorialTarget target))
            {
                Echo.Error($"Tutorial target '{(key == null ? "<none>" : key.name)}' is not registered. Is its UI active?");
                return false;
            }

            if (target.InteractionTransform is RectTransform rect)
            {
                targetRect = rect;
                return true;
            }

            Echo.Error($"Tutorial target '{key.name}' has no RectTransform to position against.");
            return false;
        }

        private bool TryResolveParent(TutorialContext context, out RectTransform parent)
        {
            if (_parentTargetKey == null)
            {
                parent = context.VisualRoot;
                return true;
            }

            return TryResolveTargetRect(context, _parentTargetKey, out parent);
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
