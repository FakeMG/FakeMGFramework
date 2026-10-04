using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using UnityEngine;
using VContainer;

namespace FakeMG.Settings
{
    /// <summary>
    /// Converts the settings save event into an asynchronous save request. Subscription ownership is
    /// paired with this component's enabled lifetime so repeated scene activation cannot leak handlers.
    /// </summary>
    public interface ISettingsPersistenceRequester
    {
        UniTask<SettingsPersistenceResult> SaveSettingsAsync(CancellationToken cancellationToken);
    }

    public readonly struct SettingsPersistenceResult
    {
        public bool Succeeded { get; }
        public string FailureReason { get; }

        public SettingsPersistenceResult(bool succeeded, string failureReason)
        {
            Succeeded = succeeded;
            FailureReason = failureReason ?? string.Empty;
        }
    }

    public sealed class SettingsSaveSubscriber : MonoBehaviour
    {
        private SettingsStateRepository _settingsStateRepository;
        private ISettingsPersistenceRequester _settingsPersistenceRequester;
        private CancellationTokenSource _lifetimeCancellationSource;

        #region Unity Lifecycle

        private void OnEnable()
        {
            _lifetimeCancellationSource = new CancellationTokenSource();
            _settingsStateRepository.OnSettingsChanged += SaveSettingsAfterChange;
        }

        private void OnDisable()
        {
            _settingsStateRepository.OnSettingsChanged -= SaveSettingsAfterChange;
            _lifetimeCancellationSource.Cancel();
            _lifetimeCancellationSource.Dispose();
            _lifetimeCancellationSource = null;
        }

        #endregion

        #region Public Methods

        [Inject]
        public void Construct(
            SettingsStateRepository settingsStateRepository,
            ISettingsPersistenceRequester settingsPersistenceRequester)
        {
            _settingsStateRepository = settingsStateRepository;
            _settingsPersistenceRequester = settingsPersistenceRequester;
        }

        #endregion

        #region Private Methods

        private void SaveSettingsAfterChange()
        {
            SaveSettingsAsync(_lifetimeCancellationSource.Token).Forget();
        }

        private async UniTaskVoid SaveSettingsAsync(CancellationToken cancellationToken)
        {
            try
            {
                SettingsPersistenceResult result = await _settingsPersistenceRequester.SaveSettingsAsync(cancellationToken);
                if (!result.Succeeded)
                {
                    Echo.Error( $"Settings save request failed: {result.FailureReason}",context: this);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Echo.Log("Settings save request was cancelled because its subscriber was disabled.");
            }
            catch (Exception exception)
            {
                Echo.Error($"Settings save request failed: {exception}", context: this);
            }
        }

        #endregion
    }
}
