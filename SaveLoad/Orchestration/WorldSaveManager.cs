using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;

namespace FakeMG.SaveLoad
{
    public sealed class WorldSaveManager : IWorldSaveManager
    {
        private readonly SaveFileService _saveFileService;
        private readonly WorldSaveRepository _worldSaveRepository;
        private readonly WorldSnapshotRetentionPolicy _retentionPolicy;
        private readonly ISaveTimeProvider _saveTimeProvider;
        private readonly WorldSaveConfiguration _configuration;
        private readonly IReadOnlyDictionary<string, ISaveable> _worldSaveables;
        private readonly AsyncSaveParticipantDependencyOrderResolver _participantOrderResolver;
        private readonly List<IAsyncSaveParticipant> _registeredParticipants = new();
        private readonly WorldOperationQueue _operationQueue = new();

        private IReadOnlyList<IAsyncSaveParticipant> _orderedParticipants = Array.Empty<IAsyncSaveParticipant>();
        private WorldManifest _activeWorldManifest;
        private WorldSaveResult? _lastWorldSaveResult;

        public string ActiveWorldId => _activeWorldManifest?.WorldId;
        public bool HasActiveWorld => _activeWorldManifest != null;
        public bool IsSaving => _operationQueue.IsProcessing;

        public WorldSaveManager(
            SaveFileService saveFileService,
            WorldSaveRepository worldSaveRepository,
            WorldSnapshotRetentionPolicy retentionPolicy,
            ISaveTimeProvider saveTimeProvider,
            WorldSaveConfiguration configuration,
            AsyncSaveParticipantDependencyOrderResolver participantOrderResolver,
            IEnumerable<ISaveable> worldSaveables)
        {
            _saveFileService = saveFileService;
            _worldSaveRepository = worldSaveRepository;
            _retentionPolicy = retentionPolicy;
            _saveTimeProvider = saveTimeProvider;
            _configuration = configuration;
            _participantOrderResolver = participantOrderResolver;
            _worldSaveables = SaveableRegistration.Create(worldSaveables);
        }

        #region Public Methods

        public IReadOnlyList<WorldSummary> GetWorlds()
        {
            return _worldSaveRepository.GetWorlds();
        }

        public IReadOnlyList<WorldSnapshotSummary> GetSnapshots(string worldId)
        {
            WorldId.Parse(worldId);
            return _worldSaveRepository.GetSnapshots(worldId);
        }

        public UniTask<WorldCreationResult> CreateWorldAsync(string displayName, CancellationToken cancellationToken = default)
        {
            return _operationQueue.EnqueueOperationAsync(
                operationCancellationToken => CreateWorldInternalAsync(displayName, operationCancellationToken),
                cancellationToken);
        }

        public UniTask<WorldOperationResult> OpenWorldAsync(string worldId, CancellationToken cancellationToken = default)
        {
            return _operationQueue.EnqueueOperationAsync(
                operationCancellationToken => OpenWorldInternalAsync(worldId, null, true, operationCancellationToken),
                cancellationToken);
        }

        public UniTask<WorldOperationResult> LoadSnapshotAsync(
            string worldId,
            string snapshotFileName,
            CancellationToken cancellationToken = default)
        {
            return _operationQueue.EnqueueOperationAsync(
                operationCancellationToken => OpenWorldInternalAsync(worldId, snapshotFileName, false, operationCancellationToken),
                cancellationToken);
        }

        public UniTask<WorldSaveResult> SaveManualAsync(CancellationToken cancellationToken = default)
        {
            return EnqueueSaveAsync(WorldSnapshotKind.Manual, cancellationToken);
        }

        public UniTask<WorldSaveResult> TriggerAutoSaveAsync(CancellationToken cancellationToken = default)
        {
            return EnqueueSaveAsync(WorldSnapshotKind.Auto, cancellationToken);
        }

        public UniTask<WorldOperationResult> DeleteWorldAsync(string worldId, CancellationToken cancellationToken = default)
        {
            return _operationQueue.EnqueueOperationAsync(
                operationCancellationToken => DeleteWorldInternalAsync(worldId, operationCancellationToken),
                cancellationToken);
        }

        public bool RegisterAsyncSaveParticipant(IAsyncSaveParticipant participant)
        {
            if (participant == null)
            {
                Echo.Error("Cannot register a missing save participant.");
                return false;
            }

            if (_registeredParticipants.Contains(participant))
            {
                Echo.Warning($"Save participant '{participant.ParticipantId}' is already registered.");
                return false;
            }

            List<IAsyncSaveParticipant> proposedParticipants = new(_registeredParticipants)
            {
                participant,
            };
            try
            {
                _orderedParticipants = _participantOrderResolver.Resolve(proposedParticipants);
                _registeredParticipants.Add(participant);
                return true;
            }
            catch (Exception exception)
            {
                Echo.Error($"Save participant registration was rejected: {exception}");
                return false;
            }
        }

        public void UnregisterAsyncSaveParticipant(IAsyncSaveParticipant participant)
        {
            if (!_registeredParticipants.Remove(participant))
            {
                Echo.Warning("Save participant unregistration was ignored because it was not registered.");
                return;
            }

            _orderedParticipants = _participantOrderResolver.Resolve(_registeredParticipants);
        }

        public void Dispose()
        {
            _operationQueue.Dispose();
        }

        #endregion

        #region Private Methods

        private UniTask<WorldSaveResult> EnqueueSaveAsync(WorldSnapshotKind snapshotKind, CancellationToken cancellationToken)
        {
            return _operationQueue.EnqueueSaveAsync(
                snapshotKind,
                operationCancellationToken => SaveWorldInternalAsync(snapshotKind, operationCancellationToken),
                cancellationToken);
        }

        private async UniTask<WorldCreationResult> CreateWorldInternalAsync(string displayName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return WorldCreationResult.Failure("World display name is required.");
            }

            if (!CanStartWorldTransition(out string transitionFailureReason))
            {
                return WorldCreationResult.Failure(transitionFailureReason);
            }

            WorldManifest previousManifest = CloneManifest(_activeWorldManifest);
            IReadOnlyDictionary<string, object> rollbackStates;
            try
            {
                rollbackStates = SaveableCollection.Capture(_worldSaveables);
            }
            catch (Exception exception)
            {
                return WorldCreationResult.Failure($"Could not stage current world state before creation: {exception.Message}");
            }

            string worldId = WorldId.CreateNew().Value;
            if (_worldSaveRepository.WorldDirectoryExists(worldId))
            {
                return WorldCreationResult.Failure($"Generated world directory '{worldId}' already exists.");
            }

            DateTime timestampUtc = _saveTimeProvider.GetUtcNow();
            var manifest = new WorldManifest
            {
                WorldId = worldId,
                DisplayName = displayName.Trim(),
                CreatedTimestampUtc = timestampUtc,
                LastPlayedTimestampUtc = timestampUtc,
            };

            try
            {
                bool didRestoreDefaults = await _saveFileService.RestoreDefaultsAsync(
                    SaveFileCatalog.CreateWorldManifestFilePath(worldId),
                    _worldSaveables,
                    _orderedParticipants,
                    cancellationToken);
                if (!didRestoreDefaults)
                {
                    return await RollBackFailedCreationAsync(
                        worldId,
                        previousManifest,
                        rollbackStates,
                        "Default world state could not be applied.");
                }

                SaveFileWriteResult manifestResult = await _worldSaveRepository.SaveManifestAsync(manifest, timestampUtc, cancellationToken);
                if (!manifestResult.Succeeded)
                {
                    return await RollBackFailedCreationAsync(
                        worldId,
                        previousManifest,
                        rollbackStates,
                        manifestResult.FailureReason);
                }

                SaveFileWriteResult snapshotResult = await _worldSaveRepository.SaveSnapshotAsync(
                    manifest,
                    WorldSnapshotKind.Auto,
                    SaveableCollection.Capture(_worldSaveables),
                    _orderedParticipants,
                    timestampUtc,
                    cancellationToken);
                if (!snapshotResult.Succeeded)
                {
                    return await RollBackFailedCreationAsync(
                        worldId,
                        previousManifest,
                        rollbackStates,
                        snapshotResult.FailureReason);
                }

                _activeWorldManifest = manifest;
                _lastWorldSaveResult = null;
                Echo.Log($"Created world '{manifest.DisplayName}' ({manifest.WorldId}).");
                return WorldCreationResult.Success(new WorldSummary(manifest));
            }
            catch (OperationCanceledException)
            {
                await RollBackFailedCreationAsync(
                    worldId,
                    previousManifest,
                    rollbackStates,
                    "World creation was cancelled.");
                return WorldCreationResult.Failure("World creation was cancelled.");
            }
            catch (Exception exception)
            {
                return await RollBackFailedCreationAsync(
                    worldId,
                    previousManifest,
                    rollbackStates,
                    $"World creation failed: {exception}");
            }
        }

        private async UniTask<WorldOperationResult> OpenWorldInternalAsync(
            string worldId,
            string selectedSnapshotFileName,
            bool canUseSnapshotFallback,
            CancellationToken cancellationToken)
        {
            try
            {
                WorldId.Parse(worldId);
            }
            catch (ArgumentException exception)
            {
                return WorldOperationResult.Rejected(exception.Message);
            }

            if (!CanStartWorldTransition(out string transitionFailureReason))
            {
                return WorldOperationResult.Failure(transitionFailureReason);
            }

            (WorldManifest manifest, SaveFileLoadResult manifestResult) = await _worldSaveRepository.LoadManifestAsync(worldId, cancellationToken);
            if (manifest == null)
            {
                return WorldOperationResult.Rejected($"Cannot open world '{worldId}': {manifestResult.FailureReason}");
            }

            IReadOnlyDictionary<string, object> rollbackStates;
            try
            {
                rollbackStates = SaveableCollection.Capture(_worldSaveables);
            }
            catch (Exception exception)
            {
                return WorldOperationResult.Failure($"Could not stage the active world before opening '{worldId}': {exception.Message}");
            }

            WorldManifest previousManifest = CloneManifest(_activeWorldManifest);
            IReadOnlyList<WorldSnapshotSummary> snapshots;
            if (canUseSnapshotFallback)
            {
                snapshots = _worldSaveRepository.GetSnapshots(worldId);
            }
            else if (_worldSaveRepository.TryGetSnapshot(
                         worldId,
                         selectedSnapshotFileName,
                         out WorldSnapshotSummary selectedSnapshot,
                         out string failureReason))
            {
                snapshots = new[] { selectedSnapshot };
            }
            else
            {
                return WorldOperationResult.Rejected(failureReason);
            }

            List<string> snapshotFailures = new();
            foreach (WorldSnapshotSummary snapshot in snapshots)
            {
                SaveFileLoadResult snapshotResult = await _worldSaveRepository.LoadSnapshotAsync(
                    worldId,
                    snapshot,
                    _worldSaveables,
                    _orderedParticipants,
                    cancellationToken);
                if (!snapshotResult.Succeeded)
                {
                    snapshotFailures.Add($"{snapshot.FileName}: {snapshotResult.FailureReason}");
                    continue;
                }

                WorldManifest activatedManifest = CloneManifest(manifest);
                activatedManifest.LastPlayedTimestampUtc = _saveTimeProvider.GetUtcNow();
                SaveFileWriteResult updateResult = await _worldSaveRepository.SaveManifestAsync(activatedManifest, activatedManifest.LastPlayedTimestampUtc, cancellationToken);
                if (!updateResult.Succeeded)
                {
                    await RollBackLoadedWorldAsync(
                        previousManifest,
                        rollbackStates,
                        $"{SaveFileCatalog.CreateWorldDirectoryPath(worldId)}/{snapshot.FileName}");
                    return WorldOperationResult.Failure($"Snapshot loaded but manifest update failed: {updateResult.FailureReason}");
                }

                _activeWorldManifest = activatedManifest;
                _lastWorldSaveResult = null;
                return WorldOperationResult.Success();
            }

            return WorldOperationResult.Rejected(
                snapshotFailures.Count == 0
                    ? $"World '{worldId}' has no valid snapshots."
                    : string.Join(Environment.NewLine, snapshotFailures));
        }

        private async UniTask<WorldSaveResult> SaveWorldInternalAsync(WorldSnapshotKind snapshotKind, CancellationToken cancellationToken)
        {
            if (!HasActiveWorld)
            {
                var noWorldResult = new WorldSaveResult(WorldSaveStatus.NoActiveWorld, string.Empty, "No world is active.", false);
                _lastWorldSaveResult = noWorldResult;
                return noWorldResult;
            }

            WorldManifest manifest = CloneManifest(_activeWorldManifest);
            DateTime timestampUtc = _saveTimeProvider.GetUtcNow();
            SaveFileWriteResult snapshotResult;
            try
            {
                snapshotResult = await _worldSaveRepository.SaveSnapshotAsync(
                    manifest,
                    snapshotKind,
                    SaveableCollection.Capture(_worldSaveables),
                    _orderedParticipants,
                    timestampUtc,
                    cancellationToken);
            }
            catch (Exception exception)
            {
                var captureFailure = new WorldSaveResult(
                    WorldSaveStatus.SnapshotFailed,
                    string.Empty,
                    exception.ToString(),
                    false);
                _lastWorldSaveResult = captureFailure;
                return captureFailure;
            }

            if (!snapshotResult.Succeeded)
            {
                WorldSaveStatus failureStatus = snapshotResult.DidCommitFile
                    ? WorldSaveStatus.ParticipantCompletionFailed
                    : snapshotResult.Status == SaveFileWriteStatus.Cancelled
                        ? WorldSaveStatus.Cancelled
                        : WorldSaveStatus.SnapshotFailed;
                var failedResult = new WorldSaveResult(
                    failureStatus,
                    snapshotResult.FilePath,
                    snapshotResult.FailureReason,
                    snapshotResult.DidCommitFile);
                _lastWorldSaveResult = failedResult;
                return failedResult;
            }

            manifest.LastPlayedTimestampUtc = timestampUtc;
            SaveFileWriteResult manifestResult = await _worldSaveRepository.SaveManifestAsync(
                manifest,
                timestampUtc,
                cancellationToken);
            if (!manifestResult.Succeeded)
            {
                var partialResult = new WorldSaveResult(
                    WorldSaveStatus.SnapshotCommittedManifestFailed,
                    snapshotResult.FilePath,
                    manifestResult.FailureReason,
                    true);
                _lastWorldSaveResult = partialResult;
                return partialResult;
            }

            _activeWorldManifest = manifest;
            if (snapshotKind == WorldSnapshotKind.Auto)
            {
                IReadOnlyList<WorldSnapshotSummary> expiredSnapshots = _retentionPolicy.SelectExpiredAutoSaves(
                        _worldSaveRepository.GetSnapshots(manifest.WorldId),
                        _configuration.MaximumAutoSaveCount);
                _worldSaveRepository.DeleteSnapshots(expiredSnapshots);
            }

            var successResult = new WorldSaveResult(
                WorldSaveStatus.Success,
                snapshotResult.FilePath,
                string.Empty,
                true);
            _lastWorldSaveResult = successResult;
            return successResult;
        }

        private async UniTask<WorldOperationResult> DeleteWorldInternalAsync(string worldId, CancellationToken cancellationToken)
        {
            try
            {
                WorldId.Parse(worldId);
            }
            catch (ArgumentException exception)
            {
                return WorldOperationResult.Rejected(exception.Message);
            }

            if (!CanStartWorldTransition(out string transitionFailureReason))
            {
                return WorldOperationResult.Failure(transitionFailureReason);
            }

            if (!_worldSaveRepository.WorldDirectoryExists(worldId))
            {
                return WorldOperationResult.Rejected($"World '{worldId}' does not exist.");
            }

            if (!string.Equals(ActiveWorldId, worldId, StringComparison.Ordinal))
            {
                _worldSaveRepository.DeleteWorld(worldId);
                return WorldOperationResult.Success();
            }

            IReadOnlyDictionary<string, object> rollbackStates = SaveableCollection.Capture(_worldSaveables);
            WorldManifest previousManifest = CloneManifest(_activeWorldManifest);
            bool didRestoreDefaults = await _saveFileService.RestoreDefaultsAsync(
                SaveFileCatalog.CreateWorldManifestFilePath(worldId),
                _worldSaveables,
                _orderedParticipants,
                cancellationToken);
            if (!didRestoreDefaults)
            {
                return WorldOperationResult.Failure($"World '{worldId}' was not deleted because defaults could not be applied.");
            }

            try
            {
                _worldSaveRepository.DeleteWorld(worldId);
                _activeWorldManifest = null;
                _lastWorldSaveResult = null;
                return WorldOperationResult.Success();
            }
            catch (Exception exception)
            {
                await RollBackLoadedWorldAsync(
                    previousManifest,
                    rollbackStates,
                    SaveFileCatalog.CreateWorldManifestFilePath(worldId));
                return WorldOperationResult.Failure($"Deleting world '{worldId}' failed: {exception}");
            }
        }

        private bool CanStartWorldTransition(out string failureReason)
        {
            if (_lastWorldSaveResult.HasValue && !_lastWorldSaveResult.Value.Succeeded)
            {
                failureReason = $"World transition rejected because the previous save failed: " + _lastWorldSaveResult.Value.FailureReason;
                return false;
            }

            failureReason = string.Empty;
            return true;
        }

        private async UniTask<WorldCreationResult> RollBackFailedCreationAsync(
            string worldId,
            WorldManifest previousManifest,
            IReadOnlyDictionary<string, object> rollbackStates,
            string failureReason)
        {
            if (!string.IsNullOrWhiteSpace(worldId) && _worldSaveRepository.WorldDirectoryExists(worldId))
            {
                _worldSaveRepository.DeleteWorld(worldId);
            }

            using var rollbackCancellationSource = new CancellationTokenSource();
            var rollbackContext = new LoadOperationContext(
                SaveFileCatalog.CreateWorldDirectoryPath(worldId),
                SaveFileCatalog.CreateWorldManifestFilePath(worldId));
            for (int participantIndex = _orderedParticipants.Count - 1; participantIndex >= 0; participantIndex--)
            {
                IAsyncSaveParticipant participant = _orderedParticipants[participantIndex];
                try
                {
                    await participant.RollBackLoadedStateAsync(rollbackContext, rollbackCancellationSource.Token);
                }
                catch (Exception exception)
                {
                    Echo.Error($"Save participant '{participant.ParticipantId}' failed while rolling back world creation: {exception}");
                }
            }

            RestoreStates(rollbackStates);
            _activeWorldManifest = previousManifest;
            Echo.Error(failureReason);
            return WorldCreationResult.Failure(failureReason);
        }

        private async UniTask RollBackLoadedWorldAsync(
            WorldManifest previousManifest,
            IReadOnlyDictionary<string, object> rollbackStates,
            string contextFilePath)
        {
            var context = new LoadOperationContext(SaveFileCatalog.GetSaveDirectoryPath(contextFilePath), contextFilePath);
            using var rollbackCancellationSource = new CancellationTokenSource();
            for (int participantIndex = _orderedParticipants.Count - 1;
                 participantIndex >= 0;
                 participantIndex--)
            {
                try
                {
                    await _orderedParticipants[participantIndex].RollBackLoadedStateAsync(context, rollbackCancellationSource.Token);
                }
                catch (Exception exception)
                {
                    Echo.Error($"Participant '{_orderedParticipants[participantIndex].ParticipantId}' failed while rolling back a world transition: {exception}");
                }
            }

            RestoreStates(rollbackStates);
            _activeWorldManifest = previousManifest;
        }

        private void RestoreStates(IReadOnlyDictionary<string, object> states)
        {
            foreach (KeyValuePair<string, ISaveable> saveableEntry in _worldSaveables.Reverse())
            {
                saveableEntry.Value.RestoreState(states[saveableEntry.Key]);
            }
        }

        private static WorldManifest CloneManifest(WorldManifest manifest)
        {
            return manifest == null
                ? null
                : new WorldManifest
                {
                    WorldId = manifest.WorldId,
                    DisplayName = manifest.DisplayName,
                    CreatedTimestampUtc = manifest.CreatedTimestampUtc,
                    LastPlayedTimestampUtc = manifest.LastPlayedTimestampUtc,
                };
        }

        #endregion
    }
}
