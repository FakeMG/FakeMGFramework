using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;

namespace FakeMG.SaveLoad
{
    public sealed class SaveFileService
    {
        private readonly ISaveDataStoreFactory _saveDataStoreFactory;
        private readonly IAtomicFileTransaction _atomicFileTransaction;
        private readonly ISaveMigrationPlan _migrationPlan;
        private readonly ISaveEnvironment _saveEnvironment;
        private readonly SaveMetadataValidator _metadataValidator;

        public SaveFileService(
            ISaveDataStoreFactory saveDataStoreFactory,
            IAtomicFileTransaction atomicFileTransaction,
            ISaveMigrationPlan migrationPlan,
            ISaveEnvironment saveEnvironment,
            SaveMetadataValidator metadataValidator)
        {
            _saveDataStoreFactory = saveDataStoreFactory;
            _atomicFileTransaction = atomicFileTransaction;
            _migrationPlan = migrationPlan;
            _saveEnvironment = saveEnvironment;
            _metadataValidator = metadataValidator;
        }

        #region Public Methods

        public async UniTask<SaveFileWriteResult> SaveAsync(
            SaveFileDescriptor descriptor,
            IReadOnlyDictionary<string, object> states,
            IReadOnlyList<IAsyncSaveParticipant> participants,
            DateTime timestampUtc,
            CancellationToken cancellationToken)
        {
            ISaveDataStore saveDataStore = _saveDataStoreFactory.Create(descriptor.Profile);
            var context = new SaveOperationContext(
                SaveFileCatalog.GetSaveDirectoryPath(descriptor.FilePath),
                descriptor.FilePath,
                descriptor.SaveKind,
                timestampUtc);
            var participantBatch = new AsyncSaveParticipantBatch();
            bool didCommitFile = false;
            string failureReason = string.Empty;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await participantBatch.PrepareAsync(participants, context, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                var metadata = new SaveMetadata
                {
                    TimestampUtc = timestampUtc,
                    ApplicationVersion = _saveEnvironment.ApplicationVersion,
                    SaveKind = descriptor.SaveKind,
                    OwnerId = descriptor.OwnerId,
                };
                _atomicFileTransaction.Commit(
                    descriptor.FilePath,
                    temporaryFilePath => saveDataStore.WriteSaveFile(temporaryFilePath, metadata, states));
                didCommitFile = true;
            }
            catch (OperationCanceledException)
            {
                failureReason = $"Saving '{descriptor.FilePath}' was cancelled.";
            }
            catch (Exception exception)
            {
                failureReason = $"Failed to save '{descriptor.FilePath}': {exception}";
                Echo.Error(failureReason);
            }

            using var completionCancellationSource = new CancellationTokenSource();
            IReadOnlyList<string> completionFailures = await participantBatch.CompleteAsync(
                context,
                didCommitFile,
                completionCancellationSource.Token,
                ReportParticipantCompletionFailure);
            if (completionFailures.Count > 0)
            {
                SaveFileWriteStatus status = didCommitFile
                    ? SaveFileWriteStatus.CommittedWithParticipantCompletionFailure
                    : SaveFileWriteStatus.Failed;
                return new SaveFileWriteResult(status, descriptor.FilePath, string.Join("; ", completionFailures));
            }

            if (didCommitFile)
            {
                return new SaveFileWriteResult(SaveFileWriteStatus.Success, descriptor.FilePath);
            }

            return new SaveFileWriteResult(
                cancellationToken.IsCancellationRequested
                    ? SaveFileWriteStatus.Cancelled
                    : SaveFileWriteStatus.Failed,
                descriptor.FilePath,
                failureReason);
        }

        public async UniTask<SaveFileLoadResult> LoadAsync(
            SaveFileDescriptor descriptor,
            IReadOnlyDictionary<string, ISaveable> saveables,
            IReadOnlyList<IAsyncSaveParticipant> participants,
            bool restoreDefaultsWhenMissing,
            bool restoreDefaultsOnFailure,
            CancellationToken cancellationToken)
        {
            ISaveDataStore saveDataStore = _saveDataStoreFactory.Create(descriptor.Profile);
            string backupFilePath = AtomicFileTransactionPaths.GetBackupPath(descriptor.FilePath);
            List<string> candidatePaths = new(2);
            if (saveDataStore.FileExists(descriptor.FilePath))
            {
                candidatePaths.Add(descriptor.FilePath);
            }

            if (saveDataStore.FileExists(backupFilePath))
            {
                candidatePaths.Add(backupFilePath);
            }

            if (candidatePaths.Count == 0)
            {
                if (!restoreDefaultsWhenMissing)
                {
                    return new SaveFileLoadResult(SaveFileLoadStatus.Missing, $"Save file '{descriptor.FilePath}' does not exist.");
                }

                bool didRestoreDefaults = await TryRestoreDefaultsAsync(
                    descriptor.FilePath,
                    saveables,
                    participants,
                    cancellationToken);
                return didRestoreDefaults
                    ? new SaveFileLoadResult(SaveFileLoadStatus.DefaultsAppliedBecauseMissing)
                    : new SaveFileLoadResult(SaveFileLoadStatus.Failed, $"Defaults could not be restored for missing file '{descriptor.FilePath}'.");
            }

            List<string> candidateFailures = new();
            bool hasFailedCandidate = false;
            foreach (string candidatePath in candidatePaths)
            {
                SaveFileLoadResult candidateResult = await TryLoadCandidateAsync(
                    descriptor,
                    candidatePath,
                    saveDataStore,
                    saveables,
                    participants,
                    cancellationToken);
                if (candidateResult.Succeeded)
                {
                    if (!string.Equals(candidatePath, descriptor.FilePath, StringComparison.Ordinal))
                    {
                        _atomicFileTransaction.PromoteValidatedBackup(candidatePath, descriptor.FilePath);
                        return new SaveFileLoadResult(SaveFileLoadStatus.RecoveredBackup, loadedFilePath: candidatePath);
                    }

                    return candidateResult;
                }

                candidateFailures.Add($"{candidatePath}: {candidateResult.FailureReason}");
                hasFailedCandidate |= candidateResult.Status == SaveFileLoadStatus.Failed;
                if (candidateResult.Status == SaveFileLoadStatus.Cancelled)
                {
                    return candidateResult;
                }
            }

            if (restoreDefaultsOnFailure)
            {
                await TryRestoreDefaultsAsync(descriptor.FilePath, saveables, participants, cancellationToken);
            }

            string failureReason = string.Join(Environment.NewLine, candidateFailures);
            Echo.Error($"No valid candidate could load '{descriptor.FilePath}'. {failureReason}");
            return new SaveFileLoadResult(hasFailedCandidate ? SaveFileLoadStatus.Failed : SaveFileLoadStatus.Rejected, failureReason);
        }

        public UniTask<bool> RestoreDefaultsAsync(
            string contextFilePath,
            IReadOnlyDictionary<string, ISaveable> saveables,
            IReadOnlyList<IAsyncSaveParticipant> participants,
            CancellationToken cancellationToken)
        {
            return TryRestoreDefaultsAsync(contextFilePath, saveables, participants, cancellationToken);
        }

        #endregion

        #region Private Methods

        private async UniTask<SaveFileLoadResult> TryLoadCandidateAsync(
            SaveFileDescriptor descriptor,
            string candidateFilePath,
            ISaveDataStore saveDataStore,
            IReadOnlyDictionary<string, ISaveable> saveables,
            IReadOnlyList<IAsyncSaveParticipant> participants,
            CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                SaveMetadata metadata = saveDataStore.LoadMetadata(candidateFilePath);
                _metadataValidator.Validate(metadata, descriptor);
                var versionMigrator = new VersionMigrator(
                    _migrationPlan,
                    saveDataStore,
                    _atomicFileTransaction,
                    _saveEnvironment.ApplicationVersion);
                if (!string.Equals(metadata.ApplicationVersion, _saveEnvironment.ApplicationVersion, StringComparison.Ordinal))
                {
                    MigrationResult migrationResult = versionMigrator.MigrateSaveFile(candidateFilePath, metadata.ApplicationVersion);
                    if (!migrationResult.Succeeded)
                    {
                        throw new SaveLoadRejectedException(migrationResult.FailureReason);
                    }

                    metadata = saveDataStore.LoadMetadata(candidateFilePath);
                    _metadataValidator.Validate(metadata, descriptor);
                }

                IReadOnlyDictionary<string, StagedSaveState> stagedStates = StageAndValidateStates(saveDataStore, candidateFilePath, saveables);
                bool didApply = await TryApplyStatesAsync(
                    candidateFilePath,
                    stagedStates,
                    saveables,
                    participants,
                    cancellationToken);
                if (!didApply)
                {
                    return new SaveFileLoadResult(SaveFileLoadStatus.Failed, $"Applying '{candidateFilePath}' failed and live state was rolled back.");
                }

                Echo.Log($"Loaded '{candidateFilePath}', saved at {metadata.TimestampUtc}.");
                return new SaveFileLoadResult(SaveFileLoadStatus.Success, loadedFilePath: candidateFilePath);
            }
            catch (OperationCanceledException)
            {
                return new SaveFileLoadResult(SaveFileLoadStatus.Cancelled, $"Loading '{candidateFilePath}' was cancelled.");
            }
            catch (Exception exception)
            {
                bool wasRejected = exception is SaveLoadRejectedException or ArgumentException;
                Echo.Error($"Failed to load candidate '{candidateFilePath}': {exception}");
                return new SaveFileLoadResult(wasRejected ? SaveFileLoadStatus.Rejected : SaveFileLoadStatus.Failed, exception.Message);
            }
        }

        private static IReadOnlyDictionary<string, StagedSaveState> StageAndValidateStates(
            ISaveDataStore saveDataStore,
            string saveFilePath,
            IReadOnlyDictionary<string, ISaveable> saveables)
        {
            Dictionary<string, StagedSaveState> stagedStates = new(saveables.Count, StringComparer.Ordinal);
            foreach (KeyValuePair<string, ISaveable> saveableEntry in saveables)
            {
                bool hasSavedState = saveDataStore.KeyExists(saveableEntry.Key, saveFilePath);
                object state = hasSavedState ? saveDataStore.LoadState(saveableEntry.Key, saveFilePath) : null;
                if (hasSavedState && !saveableEntry.Value.TryValidateState(state, out string failureReason))
                {
                    throw new SaveLoadRejectedException($"State '{saveableEntry.Key}' is invalid: {failureReason}");
                }

                stagedStates.Add(saveableEntry.Key, new StagedSaveState(hasSavedState, state));
            }

            return stagedStates;
        }

        private async UniTask<bool> TryApplyStatesAsync(
            string saveFilePath,
            IReadOnlyDictionary<string, StagedSaveState> stagedStates,
            IReadOnlyDictionary<string, ISaveable> saveables,
            IReadOnlyList<IAsyncSaveParticipant> participants,
            CancellationToken cancellationToken)
        {
            Dictionary<string, object> rollbackStates = CaptureAndValidateRollbackStates(saveables);
            var context = new LoadOperationContext(SaveFileCatalog.GetSaveDirectoryPath(saveFilePath), saveFilePath);
            var participantBatch = new AsyncSaveParticipantBatch();
            try
            {
                foreach (KeyValuePair<string, ISaveable> saveableEntry in saveables)
                {
                    StagedSaveState stagedState = stagedStates[saveableEntry.Key];
                    if (stagedState.HasSavedState)
                    {
                        saveableEntry.Value.RestoreState(stagedState.State);
                    }
                    else
                    {
                        saveableEntry.Value.RestoreDefaultState();
                    }
                }

                await participantBatch.ApplyLoadedStateAsync(participants, context, cancellationToken);
                return true;
            }
            catch (Exception exception)
            {
                bool wasCancelled = exception is OperationCanceledException && cancellationToken.IsCancellationRequested;
                if (wasCancelled)
                {
                    Echo.Log($"State application was cancelled for '{saveFilePath}'.");
                }
                else
                {
                    Echo.Error($"State application failed for '{saveFilePath}': {exception}");
                }
                using var rollbackCancellationSource = new CancellationTokenSource();
                await participantBatch.RollBackLoadedStateAsync(
                    context,
                    rollbackCancellationSource.Token,
                    ReportParticipantLoadRollbackFailure);
                RestoreRollbackStates(saveables, rollbackStates);
                if (wasCancelled)
                {
                    throw new OperationCanceledException(cancellationToken);
                }

                return false;
            }
        }

        private async UniTask<bool> TryRestoreDefaultsAsync(
            string contextFilePath,
            IReadOnlyDictionary<string, ISaveable> saveables,
            IReadOnlyList<IAsyncSaveParticipant> participants,
            CancellationToken cancellationToken)
        {
            Dictionary<string, object> rollbackStates;
            try
            {
                rollbackStates = CaptureAndValidateRollbackStates(saveables);
            }
            catch (Exception exception)
            {
                Echo.Error($"Could not stage rollback state before restoring defaults: {exception}");
                return false;
            }

            string saveDirectoryPath = string.IsNullOrWhiteSpace(contextFilePath)
                ? string.Empty
                : SaveFileCatalog.GetSaveDirectoryPath(contextFilePath);
            var context = new LoadOperationContext(saveDirectoryPath, contextFilePath ?? string.Empty);
            var participantBatch = new AsyncSaveParticipantBatch();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (ISaveable saveable in saveables.Values)
                {
                    saveable.RestoreDefaultState();
                }

                await participantBatch.ApplyLoadedStateAsync(participants, context, cancellationToken);
                return true;
            }
            catch (Exception exception)
            {
                Echo.Error($"Restoring defaults failed: {exception}");
                using var rollbackCancellationSource = new CancellationTokenSource();
                await participantBatch.RollBackLoadedStateAsync(
                    context,
                    rollbackCancellationSource.Token,
                    ReportParticipantLoadRollbackFailure);
                RestoreRollbackStates(saveables, rollbackStates);
                return false;
            }
        }

        private static Dictionary<string, object> CaptureAndValidateRollbackStates(IReadOnlyDictionary<string, ISaveable> saveables)
        {
            Dictionary<string, object> rollbackStates = new(saveables.Count, StringComparer.Ordinal);
            foreach (KeyValuePair<string, ISaveable> saveableEntry in saveables)
            {
                object rollbackState = saveableEntry.Value.CaptureState();
                if (!saveableEntry.Value.TryValidateState(rollbackState, out string failureReason))
                {
                    throw new InvalidOperationException($"Saveable '{saveableEntry.Key}' produced invalid rollback state: {failureReason}");
                }

                rollbackStates.Add(saveableEntry.Key, rollbackState);
            }

            return rollbackStates;
        }

        private static void RestoreRollbackStates(
            IReadOnlyDictionary<string, ISaveable> saveables,
            IReadOnlyDictionary<string, object> rollbackStates)
        {
            List<Exception> rollbackFailures = new();
            foreach (KeyValuePair<string, ISaveable> saveableEntry in saveables.Reverse())
            {
                try
                {
                    saveableEntry.Value.RestoreState(rollbackStates[saveableEntry.Key]);
                }
                catch (Exception exception)
                {
                    rollbackFailures.Add(exception);
                    Echo.Error($"Rollback failed for saveable '{saveableEntry.Key}': {exception}");
                }
            }

            if (rollbackFailures.Count > 0)
            {
                throw new AggregateException("One or more saveables could not be rolled back.", rollbackFailures);
            }
        }

        private static void ReportParticipantCompletionFailure(IAsyncSaveParticipant participant, Exception exception)
        {
            Echo.Error($"Save participant '{participant.ParticipantId}' completion failed: {exception}");
        }

        private static void ReportParticipantLoadRollbackFailure(IAsyncSaveParticipant participant, Exception exception)
        {
            Echo.Error($"Save participant '{participant.ParticipantId}' load rollback failed: {exception}");
        }

        private readonly struct StagedSaveState
        {
            public bool HasSavedState { get; }
            public object State { get; }

            public StagedSaveState(bool hasSavedState, object state)
            {
                HasSavedState = hasSavedState;
                State = state;
            }
        }

        #endregion
    }
}
