using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using FakeMG.Framework;

namespace FakeMG.SaveLoad
{
    /// <summary>
    /// Owns canonical global/world path conventions and discovers only files created by the current
    /// persistence layout. All returned paths remain relative to Application.persistentDataPath.
    /// </summary>
    public sealed class SaveFileCatalog
    {
        public const string WORLD_ROOT_DIRECTORY_PATH = "Saves";
        public const string WORLD_MANIFEST_FILE_NAME = "world.json";
        public const string WORLD_MANIFEST_KEY = "WorldManifest";
        public const string MANUAL_SAVE_PATH_PREFIX = "manual_";
        public const string AUTO_SAVE_PATH_PREFIX = "autosave_";
        public const string WORLD_ID_PREFIX = "world_";
        public const string METADATA_KEY = "Metadata";
        public const string GLOBAL_FILE_EXTENSION = ".json";
        public const string WORLD_SNAPSHOT_FILE_EXTENSION = ".sav";

        private readonly ISaveDataStore _saveDataStore;
        private readonly ISaveDataStoreProfile _storageProfile;
        private readonly bool _shouldLogInvalidFiles;
        private readonly ISaveEnvironment _saveEnvironment;
        private readonly List<SaveCatalogDiagnostic> _diagnostics = new();

        public SaveFileCatalog(
            ISaveDataStore saveDataStore,
            ISaveDataStoreProfile storageProfile = null,
            bool shouldLogInvalidFiles = true,
            ISaveEnvironment saveEnvironment = null)
        {
            _saveDataStore = saveDataStore;
            _storageProfile = storageProfile ?? SaveFileProtectionSettings.Plain;
            _shouldLogInvalidFiles = shouldLogInvalidFiles;
            _saveEnvironment = saveEnvironment;
        }

        #region Public Methods

        public List<ValidatedSaveFileInfo> GetManagedSaveFiles()
        {
            return new List<ValidatedSaveFileInfo>(DiscoverManagedSaveFiles().Files);
        }

        public SaveCatalogDiscoveryResult DiscoverManagedSaveFiles()
        {
            _diagnostics.Clear();
            List<ValidatedSaveFileInfo> saveFiles = new();
            CollectGlobalDocuments(saveFiles);
            CollectWorldFiles(saveFiles);
            return new SaveCatalogDiscoveryResult(saveFiles, _diagnostics.ToArray());
        }

        public List<ValidatedSaveFileInfo> GetWorldSnapshotFiles(string worldId)
        {
            ValidateWorldId(worldId);
            string worldDirectoryPath = CreateWorldDirectoryPath(worldId);
            List<ValidatedSaveFileInfo> saveFiles = new();
            if (!_saveDataStore.DirectoryExists(worldDirectoryPath))
            {
                return saveFiles;
            }

            foreach (string fileName in _saveDataStore.GetFiles(worldDirectoryPath + "/"))
            {
                string saveFilePath = NormalizeSaveFilePath(fileName, worldDirectoryPath);
                if (!IsWorldSnapshotPath(saveFilePath) ||
                    !TryLoadManagedMetadataWithBackup(saveFilePath, out SaveMetadata metadata))
                {
                    continue;
                }

                if (metadata.OwnerId == worldId && SaveKindPolicy.IsWorldSnapshot(metadata.SaveKind))
                {
                    saveFiles.Add(new ValidatedSaveFileInfo(saveFilePath, metadata, _storageProfile));
                }
                else
                {
                    AddDiagnostic(
                        saveFilePath,
                        SaveCatalogRejectionReason.InvalidOwnership,
                        $"Snapshot metadata does not match world '{worldId}'.");
                }
            }

            return saveFiles;
        }

        public static string CreateGlobalSaveFilePath(string globalFileName)
        {
            if (string.IsNullOrWhiteSpace(globalFileName))
            {
                throw new ArgumentException("Global save file name is required.", nameof(globalFileName));
            }

            string normalizedFileName = NormalizePathSeparators(globalFileName).Trim();
            if (HasDirectorySegments(normalizedFileName))
            {
                throw new ArgumentException("Global save files must live in the storage root.", nameof(globalFileName));
            }

            ValidatePathSegments(normalizedFileName, nameof(globalFileName), false);
            if (!normalizedFileName.EndsWith(GLOBAL_FILE_EXTENSION, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Global save file names must use the .json extension.", nameof(globalFileName));
            }

            return normalizedFileName;
        }

        public static string CreateWorldId(Guid worldGuid)
        {
            return WORLD_ID_PREFIX + worldGuid.ToString("N");
        }

        public static string CreateWorldDirectoryPath(string worldId)
        {
            ValidateWorldId(worldId);
            return $"{WORLD_ROOT_DIRECTORY_PATH}/{worldId}";
        }

        public static string CreateWorldManifestFilePath(string worldId)
        {
            return $"{CreateWorldDirectoryPath(worldId)}/{WORLD_MANIFEST_FILE_NAME}";
        }

        public static string CreateWorldSnapshotFilePath(string worldId, SaveFileKind saveKind, DateTime timestampUtc)
        {
            if (saveKind != SaveFileKind.Manual && saveKind != SaveFileKind.Auto)
            {
                throw new ArgumentException("World snapshots must be manual or automatic.", nameof(saveKind));
            }

            string prefix = saveKind == SaveFileKind.Auto ? AUTO_SAVE_PATH_PREFIX : MANUAL_SAVE_PATH_PREFIX;
            return $"{CreateWorldDirectoryPath(worldId)}/{prefix}{timestampUtc.Ticks}{WORLD_SNAPSHOT_FILE_EXTENSION}";
        }

        public static string NormalizeSaveFilePath(string saveFilePath)
        {
            return NormalizeSaveFilePath(saveFilePath, string.Empty);
        }

        public static string NormalizeSaveFilePath(string saveFilePath, string saveDirectoryPath)
        {
            if (string.IsNullOrWhiteSpace(saveFilePath))
            {
                return saveFilePath;
            }

            string normalizedPath = NormalizePathSeparators(saveFilePath).Trim();
            if (Path.IsPathRooted(normalizedPath))
            {
                throw new ArgumentException("Save file path must be relative.", nameof(saveFilePath));
            }

            string trimmedPath = normalizedPath.Trim('/');
            ValidatePathSegments(trimmedPath, nameof(saveFilePath), false);
            string normalizedDirectoryPath = NormalizeSaveDirectoryPath(saveDirectoryPath);
            if (HasDirectorySegments(trimmedPath))
            {
                ValidateDirectoryOwnership(trimmedPath, normalizedDirectoryPath, nameof(saveFilePath));
                return trimmedPath;
            }

            return string.IsNullOrEmpty(normalizedDirectoryPath)
                ? trimmedPath
                : $"{normalizedDirectoryPath}/{trimmedPath}";
        }

        public static string NormalizeSaveDirectoryPath(string saveDirectoryPath)
        {
            if (string.IsNullOrWhiteSpace(saveDirectoryPath))
            {
                return string.Empty;
            }

            string normalizedPath = NormalizePathSeparators(saveDirectoryPath).Trim().Trim('/');
            if (Path.IsPathRooted(normalizedPath))
            {
                throw new ArgumentException("Save directory path must be relative.", nameof(saveDirectoryPath));
            }

            ValidatePathSegments(normalizedPath, nameof(saveDirectoryPath), true);
            return normalizedPath;
        }

        public static string GetSaveDirectoryPath(string saveFilePath)
        {
            string normalizedPath = NormalizeSaveFilePath(saveFilePath);
            int lastSeparatorIndex = normalizedPath.LastIndexOf('/');
            return lastSeparatorIndex < 0 ? string.Empty : normalizedPath[..lastSeparatorIndex];
        }

        public static string GetSaveFileName(string saveFilePath)
        {
            return Path.GetFileName(NormalizePathSeparators(saveFilePath));
        }

        public static void ValidateWorldId(string worldId)
        {
            WorldId.Parse(worldId);
        }

        public static bool IsWorldSnapshotPath(string saveFilePath)
        {
            string fileName = GetSaveFileName(saveFilePath);
            if (!fileName.EndsWith(WORLD_SNAPSHOT_FILE_EXTENSION, StringComparison.Ordinal))
            {
                return false;
            }

            string prefix = fileName.StartsWith(MANUAL_SAVE_PATH_PREFIX, StringComparison.Ordinal)
                ? MANUAL_SAVE_PATH_PREFIX
                : fileName.StartsWith(AUTO_SAVE_PATH_PREFIX, StringComparison.Ordinal)
                    ? AUTO_SAVE_PATH_PREFIX
                    : null;
            if (prefix == null)
            {
                return false;
            }

            int timestampLength = fileName.Length - prefix.Length - WORLD_SNAPSHOT_FILE_EXTENSION.Length;
            string timestampText = fileName.Substring(prefix.Length, timestampLength);
            return long.TryParse(timestampText, NumberStyles.None, CultureInfo.InvariantCulture, out long timestampTicks) &&
                   timestampTicks >= DateTime.MinValue.Ticks &&
                   timestampTicks <= DateTime.MaxValue.Ticks;
        }

        #endregion

        #region Private Methods

        private void CollectGlobalDocuments(ICollection<ValidatedSaveFileInfo> saveFiles)
        {
            if (!_saveDataStore.DirectoryExists(string.Empty))
            {
                return;
            }

            foreach (string fileName in _saveDataStore.GetFiles(string.Empty))
            {
                string saveFilePath = NormalizeSaveFilePath(fileName);
                if (!saveFilePath.EndsWith(GLOBAL_FILE_EXTENSION, StringComparison.OrdinalIgnoreCase) ||
                    IsTransactionCompanion(saveFilePath) ||
                    !TryLoadManagedMetadata(saveFilePath, out SaveMetadata metadata) ||
                    metadata.SaveKind != SaveFileKind.GlobalDocument)
                {
                    continue;
                }

                saveFiles.Add(new ValidatedSaveFileInfo(saveFilePath, metadata, _storageProfile));
            }
        }

        private void CollectWorldFiles(ICollection<ValidatedSaveFileInfo> saveFiles)
        {
            if (!_saveDataStore.DirectoryExists(WORLD_ROOT_DIRECTORY_PATH))
            {
                return;
            }

            foreach (string worldDirectoryName in _saveDataStore.GetDirectories(WORLD_ROOT_DIRECTORY_PATH))
            {
                string worldId = NormalizePathSeparators(worldDirectoryName).Trim('/');
                if (!WorldId.TryParse(worldId, out _))
                {
                    continue;
                }

                string manifestPath = CreateWorldManifestFilePath(worldId);
                if (TryLoadManagedMetadataWithBackup(manifestPath, out SaveMetadata manifestMetadata) &&
                    manifestMetadata.OwnerId == worldId &&
                    manifestMetadata.SaveKind == SaveFileKind.WorldManifest)
                {
                    saveFiles.Add(new ValidatedSaveFileInfo(manifestPath, manifestMetadata, _storageProfile));
                }

                foreach (ValidatedSaveFileInfo snapshot in GetWorldSnapshotFiles(worldId))
                {
                    saveFiles.Add(snapshot);
                }
            }
        }

        private bool TryLoadManagedMetadata(string saveFilePath, out SaveMetadata metadata)
        {
            metadata = null;
            try
            {
                if (IsTransactionCompanion(saveFilePath) || !_saveDataStore.FileExists(saveFilePath) ||
                    !_saveDataStore.KeyExists(METADATA_KEY, saveFilePath))
                {
                    return false;
                }

                metadata = _saveDataStore.LoadMetadata(saveFilePath);
                if (metadata == null)
                {
                    AddDiagnostic(
                        saveFilePath,
                        SaveCatalogRejectionReason.MissingMetadata,
                        "Save metadata is missing.");
                    return false;
                }

                SaveKindPolicy.ValidatePersistedKind(metadata.SaveKind);
                if (_saveEnvironment != null &&
                    !SaveVersionPolicy.TryValidateReadableVersion(
                        metadata.ApplicationVersion,
                        _saveEnvironment.ApplicationVersion,
                        out string versionFailureReason))
                {
                    AddDiagnostic(
                        saveFilePath,
                        SaveCatalogRejectionReason.UnsupportedVersion,
                        versionFailureReason);
                    metadata = null;
                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                AddDiagnostic(
                    saveFilePath,
                    SaveCatalogRejectionReason.IncompatibleProfile,
                    exception.Message);
                if (_shouldLogInvalidFiles)
                {
                    LogSkippedManagedFileIfEditor(saveFilePath, exception);
                }

                return false;
            }
        }

        private bool TryLoadManagedMetadataWithBackup(string saveFilePath, out SaveMetadata metadata)
        {
            if (TryLoadManagedMetadata(saveFilePath, out metadata))
            {
                return true;
            }

            return TryLoadManagedMetadata(AtomicFileTransactionPaths.GetBackupPath(saveFilePath), out metadata);
        }

        public static bool IsTransactionCompanion(string saveFilePath)
        {
            return saveFilePath.EndsWith(".bak", StringComparison.OrdinalIgnoreCase) ||
                   saveFilePath.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase);
        }

        [Conditional("UNITY_EDITOR")]
        private static void LogSkippedManagedFileIfEditor(string saveFilePath, Exception exception)
        {
            Echo.Warning($"[SaveFileCatalog] Skipped invalid managed file '{saveFilePath}': {exception}");
        }

        private void AddDiagnostic(
            string saveFilePath,
            SaveCatalogRejectionReason reason,
            string message)
        {
            _diagnostics.Add(new SaveCatalogDiagnostic(saveFilePath, reason, message));
        }

        private static void ValidatePathSegments(string normalizedPath, string parameterName, bool allowEmpty)
        {
            string[] segments = normalizedPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (!allowEmpty && segments.Length == 0)
            {
                throw new ArgumentException("Path must contain at least one segment.", parameterName);
            }

            foreach (string segment in segments)
            {
                if (segment == "." || segment == ".." || segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    throw new ArgumentException($"Path segment '{segment}' is invalid.", parameterName);
                }
            }
        }

        private static bool HasDirectorySegments(string normalizedPath)
        {
            return normalizedPath.Contains('/');
        }

        private static void ValidateDirectoryOwnership(string normalizedPath, string normalizedDirectoryPath, string parameterName)
        {
            if (!string.IsNullOrEmpty(normalizedDirectoryPath) &&
                !normalizedPath.StartsWith(normalizedDirectoryPath + "/", StringComparison.Ordinal))
            {
                throw new ArgumentException("Save file path escaped its configured directory.", parameterName);
            }
        }

        private static string NormalizePathSeparators(string path)
        {
            return path.Replace("\\", "/");
        }

        #endregion
    }
}
