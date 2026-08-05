using System;
using System.Linq;

namespace FakeMG.SaveLoad
{
    /// <summary>
    /// Encapsulates manual, automatic, and fixed-file naming and retention rules. Save execution asks
    /// this object for file decisions while request precedence remains owned by its coordinator.
    /// </summary>
    public sealed class DefaultSaveRequestPolicy : ISaveRequestPolicy
    {
        private readonly bool _usesFixedSaveFile;
        private readonly SaveFileCatalog _saveFileCatalog;
        private readonly ISaveDataStore _saveDataStore;

        public SaveFileKind SaveKind { get; }
        public string DisplayName { get; }

        public DefaultSaveRequestPolicy(
            SaveFileKind saveKind,
            bool usesFixedSaveFile,
            SaveFileCatalog saveFileCatalog,
            ISaveDataStore saveDataStore)
        {
            SaveKind = saveKind;
            _usesFixedSaveFile = usesFixedSaveFile;
            _saveFileCatalog = saveFileCatalog;
            _saveDataStore = saveDataStore;
            DisplayName = saveKind == SaveFileKind.Auto ? "Auto-save" : "Game";
        }

        #region Public Methods

        public string CreateSaveFilePath(string saveDirectoryPath, string fixedSaveFilePath, DateTime timestampUtc)
        {
            if (_usesFixedSaveFile)
            {
                return fixedSaveFilePath;
            }

            return SaveKind == SaveFileKind.Auto
                ? SaveFileCatalog.CreateAutoSaveFilePath(saveDirectoryPath, timestampUtc)
                : SaveFileCatalog.CreateManualSaveFilePath(saveDirectoryPath, timestampUtc);
        }

        public void ApplyRetention(string saveDirectoryPath, int maximumAutoSaveCount)
        {
            if (_usesFixedSaveFile || SaveKind != SaveFileKind.Auto)
            {
                return;
            }

            ManagedSaveFileInfo[] autoSaveFiles = _saveFileCatalog
                .GetManagedSaveFiles(saveDirectoryPath)
                .Where(file => file.SaveKind == SaveFileKind.Auto)
                .OrderBy(file => file.Metadata.GetTimestampUtc())
                .ToArray();
            int removalCount = autoSaveFiles.Length - maximumAutoSaveCount;
            for (int saveIndex = 0; saveIndex < removalCount; saveIndex++)
            {
                _saveDataStore.DeleteFile(autoSaveFiles[saveIndex].SaveFilePath);
            }
        }

        #endregion
    }
}
