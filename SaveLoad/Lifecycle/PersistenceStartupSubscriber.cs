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
        private IGlobalSaveInitializer _globalSaveInitializer;
        private IWorldStartupContext _worldStartupContext;
        private WorldSaveConfiguration _configuration;
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
        public void Construct(
            IGlobalSaveInitializer globalSaveInitializer,
            IWorldStartupContext worldStartupContext,
            WorldSaveConfiguration configuration)
        {
            _globalSaveInitializer = globalSaveInitializer;
            _worldStartupContext = worldStartupContext;
            _configuration = configuration;
        }

        #endregion

        #region Private Methods

        private async UniTaskVoid InitializePersistenceSafelyAsync(CancellationToken cancellationToken)
        {
            try
            {
                GlobalSaveInitializationResult globalResult = await _globalSaveInitializer.InitializeAsync(cancellationToken);
                if (!globalResult.Succeeded)
                {
                    Echo.Error(string.Join(Environment.NewLine, globalResult.FailureReasons), context: this);
                }

                await _configuration.StartupPolicySO.InitializeAsync(
                    _worldStartupContext,
                    _configuration.DefaultWorldDisplayName,
                    cancellationToken);
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
