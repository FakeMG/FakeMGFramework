using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.SaveLoad;

namespace FakeMG.Settings.SaveLoad
{
    public sealed class SettingsGlobalSaveDocument : IGlobalSaveDocument
    {
        public const string DOCUMENT_ID = "settings";
        public const string SAVE_FILE_NAME = "settings.json";
        public const string SAVE_ID = nameof(SettingsGlobalSaveDocument);

        private readonly SettingsStateRepository _settingsStateRepository;

        public string DocumentId => DOCUMENT_ID;
        public string FileName => SAVE_FILE_NAME;
        public string SaveId => SAVE_ID;
        public ISaveDataStoreProfile StorageProfile { get; }

        public SettingsGlobalSaveDocument(SettingsStateRepository settingsStateRepository, ISaveDataStoreProfile storageProfile)
        {
            _settingsStateRepository = settingsStateRepository;
            StorageProfile = storageProfile;
        }

        #region Public Methods

        public object CaptureState()
        {
            return _settingsStateRepository.CaptureSnapshot();
        }

        public bool TryValidateState(object state, out string failureReason)
        {
            if (state is not SettingDataSnapshot snapshot)
            {
                failureReason = "Settings document state has the wrong type.";
                return false;
            }

            return _settingsStateRepository.TryValidateSnapshot(snapshot, out failureReason);
        }

        public void RestoreState(object state)
        {
            _settingsStateRepository.RestoreSnapshot((SettingDataSnapshot)state);
        }

        public void RestoreDefaultState()
        {
            _settingsStateRepository.RestoreDefaults();
        }

        #endregion

    }

    public sealed class SettingsPersistenceRequester : ISettingsPersistenceRequester
    {
        private readonly IGlobalDocumentSaveRequester _globalDocumentSaveRequester;

        public SettingsPersistenceRequester(IGlobalDocumentSaveRequester globalDocumentSaveRequester)
        {
            _globalDocumentSaveRequester = globalDocumentSaveRequester;
        }

        public async UniTask<SettingsPersistenceResult> SaveSettingsAsync(CancellationToken cancellationToken)
        {
            GlobalDocumentSaveResult result = await _globalDocumentSaveRequester.SaveAsync(SettingsGlobalSaveDocument.DOCUMENT_ID, cancellationToken);
            return new SettingsPersistenceResult(result.Succeeded, result.FailureReason);
        }
    }
}
