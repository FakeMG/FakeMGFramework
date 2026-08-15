using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.SaveLoad
{
    /// <summary>
    /// Executes one ordered save transaction. External participants prepare first, captured local
    /// state commits atomically second, and participant completion always observes the commit result.
    /// </summary>
    public sealed class SaveOperationExecutor
    {
        private readonly ISaveDataStore _saveDataStore;
        private readonly IAtomicFileTransaction _atomicFileTransaction;
        private readonly SaveFileCatalog _saveFileCatalog;
        private readonly SaveStateRegistry _stateRegistry;
        private readonly AsyncSaveParticipantBatch _participantBatch;

        public SaveOperationExecutor(
            ISaveDataStore saveDataStore,
            IAtomicFileTransaction atomicFileTransaction,
            SaveFileCatalog saveFileCatalog,
            SaveStateRegistry stateRegistry)
        {
            _saveDataStore = saveDataStore;
            _atomicFileTransaction = atomicFileTransaction;
            _saveFileCatalog = saveFileCatalog;
            _stateRegistry = stateRegistry;
            _participantBatch = new AsyncSaveParticipantBatch();
        }

        #region Public Methods

        public async UniTask<bool> ExecuteAsync(
            SaveFileKind saveKind,
            string saveDirectoryPath,
            string fixedSaveFilePath,
            bool usesFixedSaveFile,
            int maximumAutoSaveCount,
            CancellationToken cancellationToken)
        {
            DateTime timestampUtc = DateTime.UtcNow;
            var requestPolicy = new DefaultSaveRequestPolicy(saveKind, usesFixedSaveFile, _saveFileCatalog, _saveDataStore);
            string saveFilePath = requestPolicy.CreateSaveFilePath(saveDirectoryPath, fixedSaveFilePath, timestampUtc);
            var context = new SaveOperationContext(saveDirectoryPath, saveFilePath, saveKind, timestampUtc);
            bool didMetadataCommit = false;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _participantBatch.PrepareAsync(_stateRegistry.AsyncParticipants, context, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                SaveMetadata metadata = CreateMetadata(timestampUtc, saveKind);
                var capturedStates = _stateRegistry.CaptureStates();
                _atomicFileTransaction.Commit(
                    saveFilePath,
                    temporaryFilePath => _saveDataStore.WriteSaveFile(
                        temporaryFilePath, metadata, capturedStates));
                didMetadataCommit = true;
                requestPolicy.ApplyRetention(saveDirectoryPath, maximumAutoSaveCount);
                Echo.Log($"{requestPolicy.DisplayName} saved to {saveFilePath}.");
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Echo.Error($"Failed to save {saveFilePath}: {exception.Message}");
                return false;
            }
            finally
            {
                await _participantBatch.CompleteAsync(context, didMetadataCommit, CancellationToken.None, ReportParticipantCompletionFailure);
            }
        }

        #endregion

        #region Private Methods

        private static SaveMetadata CreateMetadata(DateTime timestampUtc, SaveFileKind saveKind)
        {
            return new SaveMetadata
            {
                TimestampUtc = timestampUtc,
                GameVersion = Application.version,
                SaveKind = saveKind,
            };
        }

        private void ReportParticipantCompletionFailure(IAsyncSaveParticipant participant, Exception exception)
        {
            Echo.Error($"Save participant {participant.GetType().Name} failed completion: {exception.Message}");
        }

        #endregion
    }
}
