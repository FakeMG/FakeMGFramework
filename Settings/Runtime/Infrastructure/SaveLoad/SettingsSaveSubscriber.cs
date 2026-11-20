using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using FakeMG.SaveLoad;
using UnityEngine;
using VContainer;

namespace FakeMG.Settings.SaveLoad
{
    /// <summary>Saves the settings document whenever the player edits a setting.</summary>
    public sealed class SettingsSaveSubscriber : MonoBehaviour
    {
        private SettingsStateRepository _settingsStateRepository;
        private IGlobalDocumentSaveRequester _globalDocumentSaveRequester;
        private CancellationTokenSource _lifetimeCancellationSource;

        #region Unity Lifecycle

        private void OnEnable()
        {
            _lifetimeCancellationSource = new CancellationTokenSource();
            _settingsStateRepository.OnSettingsEdited += SaveSettingsAfterEdit;
        }

        private void OnDisable()
        {
            _settingsStateRepository.OnSettingsEdited -= SaveSettingsAfterEdit;
            _lifetimeCancellationSource.Cancel();
            _lifetimeCancellationSource.Dispose();
            _lifetimeCancellationSource = null;
        }

        #endregion

        #region Public Methods

        [Inject]
        public void Construct(SettingsStateRepository settingsStateRepository, IGlobalDocumentSaveRequester globalDocumentSaveRequester)
        {
            _settingsStateRepository = settingsStateRepository;
            _globalDocumentSaveRequester = globalDocumentSaveRequester;
        }

        #endregion

        #region Private Methods

        private void SaveSettingsAfterEdit()
        {
            SaveSettingsAsync(_lifetimeCancellationSource.Token).Forget();
        }

        private async UniTaskVoid SaveSettingsAsync(CancellationToken cancellationToken)
        {
            try
            {
                GlobalDocumentSaveResult result = await _globalDocumentSaveRequester.SaveAsync(SettingsGlobalSaveDocument.DOCUMENT_ID, cancellationToken);
                if (!result.Succeeded)
                {
                    Echo.Error($"Settings save request failed: {result.FailureReason}", context: this);
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
