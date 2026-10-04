using System;
using System.Collections.Generic;

namespace FakeMG.SaveLoad
{
    public static class SaveKindPolicy
    {
        public static void ValidatePersistedKind(SaveFileKind saveKind)
        {
            if (saveKind is SaveFileKind.GlobalDocument or SaveFileKind.WorldManifest or
                SaveFileKind.Manual or SaveFileKind.Auto)
            {
                return;
            }

            throw new ArgumentOutOfRangeException(nameof(saveKind), saveKind, "Unsupported save-file kind.");
        }

        public static bool IsWorldSnapshot(SaveFileKind saveKind)
        {
            return saveKind is SaveFileKind.Manual or SaveFileKind.Auto;
        }
    }

    public static class SavePathOwnershipPolicy
    {
        public static void Validate(SaveFileDescriptor descriptor)
        {
            string filePath = descriptor.FilePath;
            switch (descriptor.SaveKind)
            {
                case SaveFileKind.GlobalDocument:
                    if (SaveFileCatalog.GetSaveDirectoryPath(filePath).Length != 0 ||
                        !filePath.EndsWith(SaveFileCatalog.GLOBAL_FILE_EXTENSION, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ArgumentException("Global documents must be root-level .json files.");
                    }

                    return;
                case SaveFileKind.WorldManifest:
                    if (!string.Equals(
                            filePath,
                            SaveFileCatalog.CreateWorldManifestFilePath(descriptor.OwnerId),
                            StringComparison.Ordinal))
                    {
                        throw new ArgumentException("World manifest path does not match its owner.");
                    }

                    return;
                case SaveFileKind.Manual:
                case SaveFileKind.Auto:
                    SaveFileCatalog.ValidateWorldId(descriptor.OwnerId);
                    if (!filePath.StartsWith(
                            SaveFileCatalog.CreateWorldDirectoryPath(descriptor.OwnerId) + "/",
                            StringComparison.Ordinal) ||
                        !SaveFileCatalog.IsWorldSnapshotPath(filePath))
                    {
                        throw new ArgumentException("World snapshot path does not match its owner.");
                    }

                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(descriptor.SaveKind));
            }
        }
    }

    public sealed class SaveMetadataValidator
    {
        public void Validate(SaveMetadata metadata, SaveFileDescriptor descriptor)
        {
            if (metadata == null)
            {
                throw new SaveLoadRejectedException("Save metadata is missing.");
            }

            if (!string.Equals(metadata.OwnerId, descriptor.OwnerId, StringComparison.Ordinal))
            {
                throw new SaveLoadRejectedException(
                    $"Save owner '{metadata.OwnerId}' does not match '{descriptor.OwnerId}'.");
            }

            if (metadata.SaveKind != descriptor.SaveKind)
            {
                throw new SaveLoadRejectedException(
                    $"Save kind '{metadata.SaveKind}' does not match '{descriptor.SaveKind}'.");
            }

            SaveKindPolicy.ValidatePersistedKind(metadata.SaveKind);
            if (string.IsNullOrWhiteSpace(metadata.ApplicationVersion))
            {
                throw new SaveLoadRejectedException("Save application version is missing.");
            }
        }
    }

    public static class SaveVersionPolicy
    {
        public static bool TryValidateReadableVersion(
            string savedVersion,
            string runtimeVersion,
            out string failureReason)
        {
            if (!Version.TryParse(savedVersion, out Version parsedSavedVersion))
            {
                failureReason = $"Saved version '{savedVersion}' is malformed.";
                return false;
            }

            if (!Version.TryParse(runtimeVersion, out Version parsedRuntimeVersion))
            {
                failureReason = $"Runtime version '{runtimeVersion}' is malformed.";
                return false;
            }

            if (parsedSavedVersion > parsedRuntimeVersion)
            {
                failureReason = $"Saved version '{savedVersion}' is newer than runtime version '{runtimeVersion}'.";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }
    }

    public static class SaveEntryKeyPolicy
    {
        public static void Validate(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Save entry key is required.", nameof(key));
            }

            if (string.Equals(key, SaveFileCatalog.METADATA_KEY, StringComparison.Ordinal))
            {
                throw new ArgumentException("Save metadata is reserved and cannot be edited as state.", nameof(key));
            }
        }
    }

    public static class SaveableRegistration
    {
        public static IReadOnlyDictionary<string, ISaveable> Create(IEnumerable<ISaveable> saveables)
        {
            Dictionary<string, ISaveable> saveablesById = new(StringComparer.Ordinal);
            foreach (ISaveable saveable in saveables)
            {
                if (saveable == null || string.IsNullOrWhiteSpace(saveable.SaveId))
                {
                    throw new InvalidOperationException("Every saveable must declare a non-empty stable SaveId.");
                }

                if (!saveablesById.TryAdd(saveable.SaveId, saveable))
                {
                    throw new InvalidOperationException($"Duplicate saveable ID '{saveable.SaveId}'.");
                }
            }

            return saveablesById;
        }
    }
}
