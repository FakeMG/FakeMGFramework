using System;

namespace FakeMG.SaveLoad
{
    public sealed class ValidatedSaveFileInfo
    {
        public ValidatedSaveFileInfo(
            string saveFilePath,
            SaveMetadata metadata,
            ISaveDataStoreProfile storageProfile)
        {
            SaveFilePath = SaveFileCatalog.NormalizeSaveFilePath(saveFilePath);
            SaveFileName = SaveFileCatalog.GetSaveFileName(SaveFilePath);
            SaveDirectoryPath = SaveFileCatalog.GetSaveDirectoryPath(SaveFilePath);
            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            SaveKindPolicy.ValidatePersistedKind(metadata.SaveKind);
            TimestampUtc = metadata.TimestampUtc;
            ApplicationVersion = metadata.ApplicationVersion;
            SaveKind = metadata.SaveKind;
            OwnerId = metadata.OwnerId;
            StorageProfile = storageProfile;
            SavePathOwnershipPolicy.Validate(new SaveFileDescriptor(
                SaveFilePath,
                OwnerId,
                SaveKind,
                StorageProfile));
        }

        public string SaveFilePath { get; }

        public string SaveFileName { get; }

        public string SaveDirectoryPath { get; }

        public DateTime TimestampUtc { get; }

        public string ApplicationVersion { get; }

        public SaveFileKind SaveKind { get; }

        public ISaveDataStoreProfile StorageProfile { get; }

        public string OwnerId { get; }

        public SaveMetadata CreateMetadataCopy()
        {
            return new SaveMetadata
            {
                TimestampUtc = TimestampUtc,
                ApplicationVersion = ApplicationVersion,
                SaveKind = SaveKind,
                OwnerId = OwnerId,
            };
        }
    }
}
