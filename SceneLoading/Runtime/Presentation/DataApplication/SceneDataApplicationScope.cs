using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using UnityEngine;
using VContainer;
using VContainer.Internal;

namespace FakeMG.SceneLoading
{
    public sealed class SceneDataApplicationScope : MonoBehaviour
    {
        private readonly List<IDisposable> _registrations = new();
        private ISceneDataApplicationCoordinator _coordinator;
        private LoadedSceneContext _sceneContext;

        #region Unity Lifecycle

        private void Start()
        {
            ApplyInitialSceneDataSafelyAsync(destroyCancellationToken).Forget();
        }

        private void OnDestroy()
        {
            foreach (IDisposable registration in _registrations)
            {
                registration.Dispose();
            }

            _registrations.Clear();
        }

        #endregion

        #region Private Methods

        [Inject]
        private void Construct(ISceneDataApplicationCoordinator coordinator, ContainerLocal<IReadOnlyList<ILoadedSceneDataApplier>> localDataAppliers)
        {
            _coordinator = coordinator;
            _sceneContext = UnitySceneContextFactory.Create(gameObject.scene);
            foreach (ILoadedSceneDataApplier dataApplier in localDataAppliers.Value)
            {
                _registrations.Add(coordinator.Register(_sceneContext, dataApplier));
            }
        }

        private async UniTaskVoid ApplyInitialSceneDataSafelyAsync(CancellationToken cancellationToken)
        {
            try
            {
                SceneDataApplicationResult result = await _coordinator.ApplyLoadedDataAsync(_sceneContext, cancellationToken);
                if (!result.Succeeded)
                {
                    Echo.Error($"Initial data application for scene '{_sceneContext.Name}' failed: {result.FailureReason}", context: this);
                }
            }
            catch (OperationCanceledException)
            {
                string cancellationReason = cancellationToken.IsCancellationRequested
                    ? "scene teardown"
                    : "the data application coordinator";
                Echo.Warning(
                    $"Initial data application for scene '{_sceneContext.Name}' was cancelled by {cancellationReason}.",
                    context: this);
            }
            catch (Exception exception)
            {
                Echo.Error($"Initial data application for scene '{_sceneContext.Name}' failed unexpectedly: {exception}", context: this);
            }
        }

        #endregion
    }
}
