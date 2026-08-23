using System;
using FakeMG.SaveLoad;

namespace FakeMG.SaveLoad.Editor
{
    internal sealed class SaveFileViewerMutationService
    {
        private IAtomicFileTransaction _atomicFileTransaction;
        private ISaveEnvironment _saveEnvironment;

        public SaveFileViewerMutationService()
        {
        }

        internal SaveFileViewerMutationService(ISaveEnvironment saveEnvironment)
        {
            _saveEnvironment = saveEnvironment;
            _atomicFileTransaction = new AtomicFileTransaction(saveEnvironment);
        }

        #region Public Methods

        public void SaveKey(
            string saveFilePath,
            string key,
            object value,
            SaveFileProtectionSettings protectionSettings)
        {
            EnsureInitialized();
            SaveEntryKeyPolicy.Validate(key);
            MutateExistingFile(
                saveFilePath,
                protectionSettings,
                temporaryFilePath => ES3.Save(
                    key,
                    value,
                    Es3FileSettingsFactory.Create(temporaryFilePath, protectionSettings)));
        }

        public void DeleteKey(
            string saveFilePath,
            string key,
            SaveFileProtectionSettings protectionSettings)
        {
            EnsureInitialized();
            SaveEntryKeyPolicy.Validate(key);
            MutateExistingFile(
                saveFilePath,
                protectionSettings,
                temporaryFilePath => ES3.DeleteKey(
                    key,
                    Es3FileSettingsFactory.Create(temporaryFilePath, protectionSettings)));
        }

        public void ReplaceRawJson(
            string saveFilePath,
            string serializedJson,
            SaveFileProtectionSettings protectionSettings)
        {
            EnsureInitialized();
            ISaveDataStore saveDataStore = CreateStore(protectionSettings);
            SaveMetadata originalMetadata = saveDataStore.LoadMetadata(saveFilePath);
            WorldManifest originalManifest = TryLoadManifest(saveDataStore, saveFilePath, originalMetadata);
            _atomicFileTransaction.Commit(
                saveFilePath,
                temporaryFilePath =>
                {
                    ES3.SaveRaw(
                        serializedJson,
                        Es3FileSettingsFactory.Create(temporaryFilePath, protectionSettings));
                    ValidateProtectedIdentity(
                        saveDataStore,
                        temporaryFilePath,
                        originalMetadata,
                        originalManifest);
                });
        }

        public void DeleteFileAndCompanions(ValidatedSaveFileInfo saveFileInfo)
        {
            EnsureInitialized();
            if (saveFileInfo.SaveKind == SaveFileKind.WorldManifest)
            {
                throw new InvalidOperationException(
                    "World manifests cannot be deleted independently. Use DeleteWorld instead.");
            }

            ISaveDataStore saveDataStore = CreateStore(
                (SaveFileProtectionSettings)saveFileInfo.StorageProfile);
            DeleteIfPresent(saveDataStore, saveFileInfo.SaveFilePath);
            DeleteIfPresent(saveDataStore, AtomicFileTransactionPaths.GetBackupPath(saveFileInfo.SaveFilePath));
            DeleteIfPresent(saveDataStore, AtomicFileTransactionPaths.GetTemporaryPath(saveFileInfo.SaveFilePath));
            DeleteIfPresent(saveDataStore, AtomicFileTransactionPaths.GetRecoveryTemporaryPath(saveFileInfo.SaveFilePath));
        }

        public void DeleteWorld(ValidatedSaveFileInfo manifestFileInfo)
        {
            EnsureInitialized();
            if (manifestFileInfo.SaveKind != SaveFileKind.WorldManifest)
            {
                throw new InvalidOperationException("DeleteWorld requires a validated world manifest.");
            }

            WorldId.Parse(manifestFileInfo.OwnerId);
            string expectedManifestPath = SaveFileCatalog.CreateWorldManifestFilePath(manifestFileInfo.OwnerId);
            if (!string.Equals(
                    manifestFileInfo.SaveFilePath,
                    expectedManifestPath,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The selected manifest path does not match its world owner.");
            }

            ISaveDataStore saveDataStore = CreateStore(
                (SaveFileProtectionSettings)manifestFileInfo.StorageProfile);
            saveDataStore.DeleteDirectory(SaveFileCatalog.CreateWorldDirectoryPath(manifestFileInfo.OwnerId));
        }

        #endregion

        #region Private Methods

        private void MutateExistingFile(
            string saveFilePath,
            SaveFileProtectionSettings protectionSettings,
            Action<string> mutateTemporaryFile)
        {
            ISaveDataStore saveDataStore = CreateStore(protectionSettings);
            SaveMetadata originalMetadata = saveDataStore.LoadMetadata(saveFilePath);
            WorldManifest originalManifest = TryLoadManifest(saveDataStore, saveFilePath, originalMetadata);
            _atomicFileTransaction.Commit(
                saveFilePath,
                temporaryFilePath =>
                {
                    saveDataStore.CopyFile(saveFilePath, temporaryFilePath);
                    mutateTemporaryFile(temporaryFilePath);
                    ValidateProtectedIdentity(
                        saveDataStore,
                        temporaryFilePath,
                        originalMetadata,
                        originalManifest);
                });
        }

        private void EnsureInitialized()
        {
            if (_saveEnvironment != null)
            {
                return;
            }

            _saveEnvironment = new UnitySaveEnvironment();
            _atomicFileTransaction = new AtomicFileTransaction(_saveEnvironment);
        }

        private ISaveDataStore CreateStore(SaveFileProtectionSettings protectionSettings)
        {
            return new Es3SaveDataStore(protectionSettings, _saveEnvironment.StorageRootPath);
        }

        private static WorldManifest TryLoadManifest(
            ISaveDataStore saveDataStore,
            string saveFilePath,
            SaveMetadata metadata)
        {
            return metadata.SaveKind == SaveFileKind.WorldManifest
                ? saveDataStore.LoadState(SaveFileCatalog.WORLD_MANIFEST_KEY, saveFilePath) as WorldManifest
                : null;
        }

        private static void ValidateProtectedIdentity(
            ISaveDataStore saveDataStore,
            string temporaryFilePath,
            SaveMetadata originalMetadata,
            WorldManifest originalManifest)
        {
            SaveMetadata candidateMetadata = saveDataStore.LoadMetadata(temporaryFilePath);
            if (candidateMetadata == null ||
                candidateMetadata.SaveKind != originalMetadata.SaveKind ||
                !string.Equals(candidateMetadata.OwnerId, originalMetadata.OwnerId, StringComparison.Ordinal) ||
                !string.Equals(
                    candidateMetadata.ApplicationVersion,
                    originalMetadata.ApplicationVersion,
                    StringComparison.Ordinal) ||
                candidateMetadata.TimestampUtc != originalMetadata.TimestampUtc)
            {
                throw new InvalidOperationException(
                    "Save metadata is read-only and cannot be changed by the Save File Viewer.");
            }

            if (originalMetadata.SaveKind != SaveFileKind.WorldManifest)
            {
                return;
            }

            WorldManifest candidateManifest =
                saveDataStore.LoadState(SaveFileCatalog.WORLD_MANIFEST_KEY, temporaryFilePath) as WorldManifest;
            if (candidateManifest == null || originalManifest == null ||
                !string.Equals(candidateManifest.WorldId, originalManifest.WorldId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("A world manifest's world ID is read-only.");
            }
        }

        private static void DeleteIfPresent(ISaveDataStore saveDataStore, string saveFilePath)
        {
            if (saveDataStore.FileExists(saveFilePath))
            {
                saveDataStore.DeleteFile(saveFilePath);
            }
        }

        #endregion
    }
}
