using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using UnityEngine;
using VContainer;

namespace FakeMG.SaveLoad
{
    public sealed class WorldIntervalAutoSaveSubscriber : MonoBehaviour
    {
        private IWorldAutoSaveRequester _autoSaveRequester;
        private WorldSaveConfiguration _configuration;
        private AutoSaveSchedule _autoSaveSchedule;
        private CancellationTokenSource _lifetimeCancellationSource;

        #region Unity Lifecycle

        private void Awake()
        {
            _lifetimeCancellationSource = new CancellationTokenSource();
        }

        private void Start()
        {
            _autoSaveSchedule = new AutoSaveSchedule(_configuration.AutoSaveIntervalSeconds);
        }

        private void Update()
        {
            if (!_configuration.IsAutoSaveEnabled ||
                !_autoSaveRequester.HasActiveWorld ||
                !_autoSaveSchedule.Advance(Time.deltaTime))
            {
                return;
            }

            RequestIntervalAutoSaveSafelyAsync(_lifetimeCancellationSource.Token).Forget();
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
            IWorldAutoSaveRequester autoSaveRequester,
            WorldSaveConfiguration configuration)
        {
            _autoSaveRequester = autoSaveRequester;
            _configuration = configuration;
        }

        #endregion

        #region Private Methods

        private async UniTaskVoid RequestIntervalAutoSaveSafelyAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                WorldSaveResult result = await _autoSaveRequester.TriggerAutoSaveAsync(cancellationToken);
                if (!result.Succeeded)
                {
                    Echo.Error($"Interval autosave failed: {result.FailureReason}", context: this);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Echo.Log("Interval autosave wait was cancelled during teardown.");
            }
            catch (Exception exception)
            {
                Echo.Error($"Interval autosave failed: {exception}", context: this);
            }
        }

        #endregion
    }
}
