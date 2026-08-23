using System;
using System.Collections.Generic;
using System.Linq;
using FakeMG.Framework;

namespace FakeMG.SaveLoad
{
    /// <summary>
    /// Discovers self-contained world folders and validates manifest/snapshot ownership before
    /// exposing them to world selection code.
    /// </summary>
    public sealed class WorldSaveCatalog
    {
        private readonly ISaveDataStore _saveDataStore;
        private readonly SaveFileCatalog _saveFileCatalog;
        private readonly ISaveDataStoreProfile _storageProfile;
        private readonly ISaveEnvironment _saveEnvironment;
        private readonly List<SaveCatalogDiagnostic> _diagnostics = new();

        public WorldSaveCatalog(
            ISaveDataStoreFactory saveDataStoreFactory,
            ISaveDataStoreProfile storageProfile,
            ISaveEnvironment saveEnvironment = null)
        {
            _storageProfile = storageProfile;
            _saveEnvironment = saveEnvironment;
            _saveDataStore = saveDataStoreFactory.Create(storageProfile);
            _saveFileCatalog = new SaveFileCatalog(_saveDataStore, storageProfile, saveEnvironment: saveEnvironment);
        }

        #region Public Methods

        public IReadOnlyList<WorldSummary> GetWorlds()
        {
            return DiscoverWorlds().Worlds;
        }

        public WorldCatalogDiscoveryResult DiscoverWorlds()
        {
            _diagnostics.Clear();
            List<WorldSummary> worlds = new();
            if (!_saveDataStore.DirectoryExists(SaveFileCatalog.WORLD_ROOT_DIRECTORY_PATH))
            {
                return new WorldCatalogDiscoveryResult(worlds, Array.Empty<SaveCatalogDiagnostic>());
            }

            foreach (string directoryName in _saveDataStore.GetDirectories(SaveFileCatalog.WORLD_ROOT_DIRECTORY_PATH))
            {
                string normalizedDirectoryName = directoryName.Replace("\\", "/").Trim('/');
                int finalSeparatorIndex = normalizedDirectoryName.LastIndexOf('/');
                string worldId = finalSeparatorIndex < 0
                    ? normalizedDirectoryName
                    : normalizedDirectoryName[(finalSeparatorIndex + 1)..];
                if (TryLoadManifest(worldId, out WorldManifest manifest))
                {
                    worlds.Add(new WorldSummary(manifest));
                }
            }

            WorldSummary[] orderedWorlds = worlds
                .OrderByDescending(world => world.LastPlayedTimestampUtc)
                .ToArray();
            return new WorldCatalogDiscoveryResult(orderedWorlds, _diagnostics.ToArray());
        }

        public IReadOnlyList<WorldSnapshotSummary> GetSnapshots(string worldId)
        {
            return _saveFileCatalog
                .GetWorldSnapshotFiles(worldId)
                .OrderByDescending(file => file.TimestampUtc)
                .Select(file => new WorldSnapshotSummary(file))
                .ToArray();
        }

        public bool TryLoadManifest(string worldId, out WorldManifest manifest)
        {
            manifest = null;
            try
            {
                SaveFileCatalog.ValidateWorldId(worldId);
                string manifestPath = SaveFileCatalog.CreateWorldManifestFilePath(worldId);
                if (TryLoadManifestFile(manifestPath, worldId, out manifest))
                {
                    return true;
                }

                string backupManifestPath = AtomicFileTransactionPaths.GetBackupPath(manifestPath);
                if (TryLoadManifestFile(backupManifestPath, worldId, out manifest))
                {
                    Echo.Warning($"World '{worldId}' will recover its manifest from backup when opened.");
                    return true;
                }

                Echo.Warning($"Skipped world '{worldId}' because its manifest is missing or invalid.");
                AddDiagnostic(
                    SaveFileCatalog.CreateWorldManifestFilePath(worldId),
                    SaveCatalogRejectionReason.CorruptManifest,
                    "World manifest is missing or invalid.");
                return false;
            }
            catch (Exception exception)
            {
                string filePath = string.IsNullOrWhiteSpace(worldId)
                    ? string.Empty
                    : $"{SaveFileCatalog.WORLD_ROOT_DIRECTORY_PATH}/{worldId}";
                AddDiagnostic(filePath, SaveCatalogRejectionReason.InvalidPath, exception.Message);
                Echo.Warning($"Skipped world '{worldId}': {exception}");
                return false;
            }
        }

        public string GetSnapshotFilePath(string worldId, string snapshotFileName)
        {
            SaveFileCatalog.ValidateWorldId(worldId);
            if (string.IsNullOrWhiteSpace(snapshotFileName) || snapshotFileName.Contains("/") || snapshotFileName.Contains("\\"))
            {
                throw new ArgumentException("Snapshot selection must be a file name.", nameof(snapshotFileName));
            }

            string worldDirectoryPath = SaveFileCatalog.CreateWorldDirectoryPath(worldId);
            string saveFilePath = SaveFileCatalog.NormalizeSaveFilePath(snapshotFileName, worldDirectoryPath);
            if (!SaveFileCatalog.IsWorldSnapshotPath(saveFilePath))
            {
                throw new ArgumentException("Selected file is not a canonical world snapshot.", nameof(snapshotFileName));
            }

            return saveFilePath;
        }

        public void DeleteWorld(string worldId)
        {
            string worldDirectoryPath = SaveFileCatalog.CreateWorldDirectoryPath(worldId);
            if (!_saveDataStore.DirectoryExists(worldDirectoryPath))
            {
                Echo.Warning($"Cannot delete missing world '{worldId}'.");
                return;
            }

            _saveDataStore.DeleteDirectory(worldDirectoryPath);
        }

        public void DeleteFileAndCompanions(string saveFilePath)
        {
            DeleteIfPresent(saveFilePath);
            DeleteIfPresent(AtomicFileTransactionPaths.GetBackupPath(saveFilePath));
            DeleteIfPresent(AtomicFileTransactionPaths.GetTemporaryPath(saveFilePath));
            DeleteIfPresent(AtomicFileTransactionPaths.GetRecoveryTemporaryPath(saveFilePath));
        }

        #endregion

        #region Private Methods

        private bool TryLoadManifestFile(string manifestPath, string worldId, out WorldManifest manifest)
        {
            manifest = null;
            try
            {
                if (!_saveDataStore.FileExists(manifestPath) ||
                    !_saveDataStore.KeyExists(SaveFileCatalog.METADATA_KEY, manifestPath) ||
                    !_saveDataStore.KeyExists(SaveFileCatalog.WORLD_MANIFEST_KEY, manifestPath))
                {
                    return false;
                }

                SaveMetadata metadata = _saveDataStore.LoadMetadata(manifestPath);
                var descriptor = new SaveFileDescriptor(
                    SaveFileCatalog.CreateWorldManifestFilePath(worldId),
                    worldId,
                    SaveFileKind.WorldManifest,
                    _storageProfile);
                new SaveMetadataValidator().Validate(metadata, descriptor);
                if (_saveEnvironment != null &&
                    !SaveVersionPolicy.TryValidateReadableVersion(metadata.ApplicationVersion, _saveEnvironment.ApplicationVersion, out string versionFailureReason))
                {
                    AddDiagnostic(manifestPath, SaveCatalogRejectionReason.UnsupportedVersion, versionFailureReason);
                    return false;
                }

                manifest = _saveDataStore.LoadState(SaveFileCatalog.WORLD_MANIFEST_KEY, manifestPath) as WorldManifest;
                if (!WorldManifestValidator.TryValidate(manifest, worldId, out string failureReason))
                {
                    AddDiagnostic(manifestPath, SaveCatalogRejectionReason.CorruptManifest, failureReason);
                    manifest = null;
                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                AddDiagnostic(manifestPath, SaveCatalogRejectionReason.IncompatibleProfile, exception.Message);
                manifest = null;
                return false;
            }
        }

        private void AddDiagnostic(string saveFilePath, SaveCatalogRejectionReason reason, string message)
        {
            _diagnostics.Add(new SaveCatalogDiagnostic(saveFilePath, reason, message));
        }

        private void DeleteIfPresent(string saveFilePath)
        {
            if (_saveDataStore.FileExists(saveFilePath))
            {
                _saveDataStore.DeleteFile(saveFilePath);
            }
        }

        #endregion
    }
}
