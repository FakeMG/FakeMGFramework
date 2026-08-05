using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.SaveLoad
{
    /// <summary>
    /// Restores one save file, validates a retained backup on failure, and applies default state when
    /// no usable file exists. It coordinates state only and never depends on a concrete serializer.
    /// </summary>
    public sealed class LoadOperationExecutor
    {
        private readonly ISaveDataStore _saveDataStore;
        private readonly IAtomicFileTransaction _atomicFileTransaction;
        private readonly SaveStateRegistry _stateRegistry;
        private readonly AsyncSaveParticipantBatch _participantBatch;
        private readonly VersionMigrator _versionMigrator;
        private readonly Action _notifyLoadingComplete;

        public LoadOperationExecutor(
            ISaveDataStore saveDataStore,
            IAtomicFileTransaction atomicFileTransaction,
            SaveStateRegistry stateRegistry,
            VersionMigrator versionMigrator,
            Action notifyLoadingComplete)
        {
            _saveDataStore = saveDataStore;
            _atomicFileTransaction = atomicFileTransaction;
            _stateRegistry = stateRegistry;
            _participantBatch = new AsyncSaveParticipantBatch();
            _versionMigrator = versionMigrator;
            _notifyLoadingComplete = notifyLoadingComplete;
        }

        #region Public Methods

        public async UniTask<bool> LoadAsync(
            string saveDirectoryPath,
            string saveFilePath,
            CancellationToken cancellationToken)
        {
            if (!_saveDataStore.FileExists(saveFilePath))
            {
                Echo.Warning($"No save file found for {saveFilePath}.");
                await LoadDefaultAsync(saveDirectoryPath, saveFilePath, cancellationToken);
                return true;
            }

            return await TryLoadFileAsync(saveDirectoryPath, saveFilePath, true, cancellationToken);
        }

        public async UniTask LoadDefaultAsync(
            string saveDirectoryPath,
            string saveFilePath,
            CancellationToken cancellationToken)
        {
            RestoreDefaultStates();
            var context = new LoadOperationContext(saveDirectoryPath, saveFilePath);
            await _participantBatch.ApplyLoadedStateAsync(_stateRegistry.AsyncParticipants, context, cancellationToken);
            _notifyLoadingComplete();
            Echo.Log("Initialized default data for all Saveables because no compatible save was available.");
        }

        #endregion

        #region Private Methods

        private async UniTask<bool> TryLoadFileAsync(
            string saveDirectoryPath,
            string saveFilePath,
            bool canRestoreBackup,
            CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                SaveMetadata metadata = _saveDataStore.LoadMetadata(saveFilePath);
                if (_versionMigrator != null && metadata.GameVersion != Application.version)
                {
                    bool didMigrationSucceed = _versionMigrator.MigrateSaveFile(
                        saveFilePath, metadata.GameVersion);
                    if (!didMigrationSucceed)
                    {
                        Echo.Error($"Migration failed for {saveFilePath}. Loading aborted.");
                        await LoadDefaultAsync(saveDirectoryPath, saveFilePath, cancellationToken);
                        return false;
                    }

                    metadata = _saveDataStore.LoadMetadata(saveFilePath);
                }

                RestoreStates(saveFilePath);
                var context = new LoadOperationContext(saveDirectoryPath, saveFilePath);
                await _participantBatch.ApplyLoadedStateAsync(_stateRegistry.AsyncParticipants, context, cancellationToken);
                Echo.Log($"Game loaded from {saveFilePath}, saved at {metadata.GetTimestampUtc()}.");
                _notifyLoadingComplete();
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                string backupSaveFilePath = AtomicFileTransactionPaths.GetBackupPath(saveFilePath);
                if (canRestoreBackup && _saveDataStore.FileExists(backupSaveFilePath))
                {
                    Echo.Warning($"Trying retained backup for {saveFilePath} after load failed: {exception.Message}");
                    bool didLoadBackup = await TryLoadFileAsync(
                        saveDirectoryPath, backupSaveFilePath, false, cancellationToken);
                    if (didLoadBackup)
                    {
                        _atomicFileTransaction.PromoteValidatedBackup(backupSaveFilePath, saveFilePath);
                        Echo.Warning($"Recovered {saveFilePath} from its validated backup.");
                        return true;
                    }
                }

                if (exception is SaveLoadRejectedException)
                {
                    Echo.Error($"Rejected incompatible save {saveFilePath}: {exception.Message}");
                    return false;
                }

                Echo.Error($"Failed to load {saveFilePath}: {exception.Message}. Loading default data instead.");
                await LoadDefaultAsync(saveDirectoryPath, saveFilePath, cancellationToken);
                return false;
            }
        }

        private void RestoreStates(string saveFilePath)
        {
            foreach (var saveable in _stateRegistry.Saveables)
            {
                if (_saveDataStore.KeyExists(saveable.Key, saveFilePath))
                {
                    saveable.Value.RestoreState(_saveDataStore.LoadState(saveable.Key, saveFilePath));
                    continue;
                }

                Echo.Warning(
                    $"No data found for {saveable.Key} in {saveFilePath}. Restored default state.");
                saveable.Value.RestoreDefaultState();
            }
        }

        private void RestoreDefaultStates()
        {
            foreach (var saveable in _stateRegistry.Saveables.Values)
            {
                saveable.RestoreDefaultState();
            }
        }

        #endregion
    }
}
