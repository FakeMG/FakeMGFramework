using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;

namespace FakeMG.SaveLoad
{
    public sealed class WorldSaveRepository
    {
        private static readonly IAsyncSaveParticipant[] _noParticipants =
            Array.Empty<IAsyncSaveParticipant>();

        private readonly SaveFileService _saveFileService;
        private readonly WorldSaveCatalog _worldSaveCatalog;
        private readonly ISaveDataStore _saveDataStore;
        private readonly ISaveDataStoreProfile _worldStorageProfile;

        public WorldSaveRepository(
            SaveFileService saveFileService,
            WorldSaveCatalog worldSaveCatalog,
            ISaveDataStoreFactory saveDataStoreFactory,
            ISaveDataStoreProfile worldStorageProfile)
        {
            _saveFileService = saveFileService;
            _worldSaveCatalog = worldSaveCatalog;
            _worldStorageProfile = worldStorageProfile;
            _saveDataStore = saveDataStoreFactory.Create(worldStorageProfile);
        }

        #region Public Methods

        public IReadOnlyList<WorldSummary> GetWorlds()
        {
            return _worldSaveCatalog.GetWorlds();
        }

        public IReadOnlyList<WorldSnapshotSummary> GetSnapshots(string worldId)
        {
            return _worldSaveCatalog.GetSnapshots(worldId);
        }

        public bool WorldDirectoryExists(string worldId)
        {
            return _saveDataStore.DirectoryExists(SaveFileCatalog.CreateWorldDirectoryPath(worldId));
        }

        public async UniTask<(WorldManifest Manifest, SaveFileLoadResult Result)> LoadManifestAsync(
            string worldId,
            CancellationToken cancellationToken)
        {
            var manifestSaveable = new WorldManifestSaveable(worldId);
            IReadOnlyDictionary<string, ISaveable> saveables =
                SaveableRegistration.Create(new ISaveable[] { manifestSaveable });
            var descriptor = new SaveFileDescriptor(
                SaveFileCatalog.CreateWorldManifestFilePath(worldId),
                worldId,
                SaveFileKind.WorldManifest,
                _worldStorageProfile);
            SaveFileLoadResult result = await _saveFileService.LoadAsync(
                descriptor,
                saveables,
                _noParticipants,
                false,
                false,
                cancellationToken);
            if (!result.Succeeded || manifestSaveable.Manifest == null)
            {
                return (null, result);
            }

            if (!WorldManifestValidator.TryValidate(
                    manifestSaveable.Manifest,
                    worldId,
                    out string failureReason))
            {
                return (
                    null,
                    new SaveFileLoadResult(SaveFileLoadStatus.Rejected, failureReason));
            }

            return (manifestSaveable.Manifest, result);
        }

        public UniTask<SaveFileLoadResult> LoadSnapshotAsync(
            string worldId,
            WorldSnapshotSummary snapshot,
            IReadOnlyDictionary<string, ISaveable> saveables,
            IReadOnlyList<IAsyncSaveParticipant> participants,
            CancellationToken cancellationToken)
        {
            if (!string.Equals(snapshot.WorldId, worldId, StringComparison.Ordinal))
            {
                return UniTask.FromResult(new SaveFileLoadResult(
                    SaveFileLoadStatus.Rejected,
                    $"Snapshot owner '{snapshot.WorldId}' does not match '{worldId}'."));
            }

            string filePath = _worldSaveCatalog.GetSnapshotFilePath(worldId, snapshot.FileName);
            var descriptor = new SaveFileDescriptor(
                filePath,
                worldId,
                snapshot.SaveKind,
                _worldStorageProfile);
            return _saveFileService.LoadAsync(
                descriptor,
                saveables,
                participants,
                false,
                false,
                cancellationToken);
        }

        public UniTask<SaveFileWriteResult> SaveManifestAsync(
            WorldManifest manifest,
            DateTime timestampUtc,
            CancellationToken cancellationToken)
        {
            if (!WorldManifestValidator.TryValidate(
                    manifest,
                    manifest?.WorldId,
                    out string failureReason))
            {
                return UniTask.FromResult(new SaveFileWriteResult(
                    SaveFileWriteStatus.Failed,
                    string.Empty,
                    failureReason));
            }

            string filePath = SaveFileCatalog.CreateWorldManifestFilePath(manifest.WorldId);
            var descriptor = new SaveFileDescriptor(
                filePath,
                manifest.WorldId,
                SaveFileKind.WorldManifest,
                _worldStorageProfile);
            var states = new Dictionary<string, object>
            {
                [SaveFileCatalog.WORLD_MANIFEST_KEY] = manifest,
            };
            return _saveFileService.SaveAsync(
                descriptor,
                states,
                _noParticipants,
                timestampUtc,
                cancellationToken);
        }

        public UniTask<SaveFileWriteResult> SaveSnapshotAsync(
            WorldManifest manifest,
            WorldSnapshotKind snapshotKind,
            IReadOnlyDictionary<string, object> states,
            IReadOnlyList<IAsyncSaveParticipant> participants,
            DateTime timestampUtc,
            CancellationToken cancellationToken)
        {
            SaveFileKind saveKind = snapshotKind == WorldSnapshotKind.Auto
                ? SaveFileKind.Auto
                : SaveFileKind.Manual;
            string filePath = CreateUniqueSnapshotPath(manifest.WorldId, saveKind, timestampUtc);
            var descriptor = new SaveFileDescriptor(
                filePath,
                manifest.WorldId,
                saveKind,
                _worldStorageProfile);
            return _saveFileService.SaveAsync(
                descriptor,
                states,
                participants,
                timestampUtc,
                cancellationToken);
        }

        public bool TryGetSnapshot(
            string worldId,
            string snapshotFileName,
            out WorldSnapshotSummary snapshot,
            out string failureReason)
        {
            snapshot = GetSnapshots(worldId).FirstOrDefault(
                candidate => string.Equals(
                    candidate.FileName,
                    snapshotFileName,
                    StringComparison.Ordinal));
            if (snapshot != null)
            {
                failureReason = string.Empty;
                return true;
            }

            failureReason =
                $"Snapshot '{snapshotFileName}' is not a validated snapshot owned by '{worldId}'.";
            return false;
        }

        public void DeleteSnapshots(IReadOnlyList<WorldSnapshotSummary> snapshots)
        {
            foreach (WorldSnapshotSummary snapshot in snapshots)
            {
                string filePath = _worldSaveCatalog.GetSnapshotFilePath(
                    snapshot.WorldId,
                    snapshot.FileName);
                _worldSaveCatalog.DeleteFileAndCompanions(filePath);
            }
        }

        public void DeleteWorld(string worldId)
        {
            _worldSaveCatalog.DeleteWorld(worldId);
        }

        #endregion

        #region Private Methods

        private string CreateUniqueSnapshotPath(
            string worldId,
            SaveFileKind saveKind,
            DateTime timestampUtc)
        {
            string filePath = SaveFileCatalog.CreateWorldSnapshotFilePath(
                worldId,
                saveKind,
                timestampUtc);
            while (_saveDataStore.FileExists(filePath))
            {
                timestampUtc = timestampUtc.AddTicks(1);
                filePath = SaveFileCatalog.CreateWorldSnapshotFilePath(
                    worldId,
                    saveKind,
                    timestampUtc);
            }

            return filePath;
        }

        private sealed class WorldManifestSaveable : ISaveable
        {
            private static readonly EmptyManifestState _emptyManifestState = new();
            private readonly string _expectedWorldId;

            public string SaveId => SaveFileCatalog.WORLD_MANIFEST_KEY;
            public WorldManifest Manifest { get; private set; }

            public WorldManifestSaveable(string expectedWorldId)
            {
                _expectedWorldId = expectedWorldId;
            }

            public object CaptureState()
            {
                return Manifest != null ? Manifest : _emptyManifestState;
            }

            public bool TryValidateState(object state, out string failureReason)
            {
                if (state is EmptyManifestState)
                {
                    failureReason = string.Empty;
                    return true;
                }

                return state is WorldManifest manifest
                    ? WorldManifestValidator.TryValidate(manifest, _expectedWorldId, out failureReason)
                    : FailWrongStateType(out failureReason);
            }

            public void RestoreState(object state)
            {
                Manifest = state is EmptyManifestState ? null : (WorldManifest)state;
            }

            public void RestoreDefaultState()
            {
                Manifest = null;
            }

            private static bool FailWrongStateType(out string failureReason)
            {
                failureReason = "World manifest state has the wrong type.";
                return false;
            }

            private sealed class EmptyManifestState
            {
            }
        }

        #endregion
    }

    public static class WorldManifestValidator
    {
        public static bool TryValidate(
            WorldManifest manifest,
            string expectedWorldId,
            out string failureReason)
        {
            if (manifest == null)
            {
                failureReason = "World manifest is missing.";
                return false;
            }

            if (!string.Equals(manifest.WorldId, expectedWorldId, StringComparison.Ordinal))
            {
                failureReason =
                    $"Manifest world ID '{manifest.WorldId}' does not match '{expectedWorldId}'.";
                return false;
            }

            if (!WorldId.TryParse(manifest.WorldId, out _))
            {
                failureReason = $"Manifest world ID '{manifest.WorldId}' is invalid.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(manifest.DisplayName))
            {
                failureReason = "World display name is required.";
                return false;
            }

            if (manifest.CreatedTimestampUtc == default ||
                manifest.LastPlayedTimestampUtc == default ||
                manifest.LastPlayedTimestampUtc < manifest.CreatedTimestampUtc)
            {
                failureReason = "World manifest timestamps are invalid.";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }
    }
}
