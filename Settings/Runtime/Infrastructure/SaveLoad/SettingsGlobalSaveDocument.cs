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
            failureReason = state is SettingDataSnapshot { Values: not null } ? string.Empty : "Settings document state is not a settings snapshot.";
            return failureReason.Length == 0;
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
}
