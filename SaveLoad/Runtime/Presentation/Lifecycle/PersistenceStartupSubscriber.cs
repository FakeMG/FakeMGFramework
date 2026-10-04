using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using UnityEngine;
using VContainer;

namespace FakeMG.SaveLoad
{
    public sealed class PersistenceStartupSubscriber : MonoBehaviour
    {
        private PersistenceStartupCoordinator _startupCoordinator;
        private CancellationTokenSource _lifetimeCancellationSource;

        #region Unity Lifecycle

        private void Awake()
        {
            _lifetimeCancellationSource = new CancellationTokenSource();
        }

        private void Start()
        {
            InitializePersistenceSafelyAsync(_lifetimeCancellationSource.Token).Forget();
        }

        private void OnDestroy()
        {
            _lifetimeCancellationSource.Cancel();
            _lifetimeCancellationSource.Dispose();
            _lifetimeCancellationSource = null;
        }

        #endregion

        #region Public Methods

        [Inject]
        public void Construct(PersistenceStartupCoordinator startupCoordinator)
        {
            _startupCoordinator = startupCoordinator;
        }

        #endregion

        #region Private Methods

        private async UniTaskVoid InitializePersistenceSafelyAsync(CancellationToken cancellationToken)
        {
            try
            {
                StartupReadinessResult result = await _startupCoordinator.InitializeAsync(cancellationToken);
                if (result.Status == StartupReadinessStatus.Cancelled)
                {
                    Echo.Log(result.FailureReason);
                }
                else if (!result.Succeeded)
                {
                    Echo.Error(result.FailureReason, context: this);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Echo.Log("Persistence startup was cancelled during teardown.");
            }
            catch (Exception exception)
            {
                Echo.Error($"Persistence startup failed: {exception}", context: this);
            }
        }

        #endregion
    }
}
