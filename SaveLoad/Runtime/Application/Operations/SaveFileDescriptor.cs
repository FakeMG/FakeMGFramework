using System;

namespace FakeMG.SaveLoad
{
    public sealed class SaveFileDescriptor
    {
        public string FilePath { get; }
        public string OwnerId { get; }
        public SaveFileKind SaveKind { get; }
        public ISaveDataStoreProfile Profile { get; }

        public SaveFileDescriptor(
            string filePath,
            string ownerId,
            SaveFileKind saveKind,
            ISaveDataStoreProfile profile)
        {
            FilePath = SaveFileCatalog.NormalizeSaveFilePath(filePath);
            OwnerId = string.IsNullOrWhiteSpace(ownerId)
                ? throw new ArgumentException("Save owner ID is required.", nameof(ownerId))
                : ownerId;
            SaveKindPolicy.ValidatePersistedKind(saveKind);
            SaveKind = saveKind;
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            SavePathOwnershipPolicy.Validate(this);
        }
    }
}
