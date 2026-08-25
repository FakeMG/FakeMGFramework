using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace FakeMG.SaveLoad
{
    /// <summary>
    /// Executes ordered asynchronous participant phases. Completion runs in reverse order for every
    /// participant that entered preparation, including the participant whose preparation failed.
    /// </summary>
    public sealed class AsyncSaveParticipantBatch
    {
        private readonly List<IAsyncSaveParticipant> _enteredParticipants = new();
        private readonly List<IAsyncSaveParticipant> _appliedLoadParticipants = new();

        #region Public Methods

        public async UniTask PrepareAsync(
            IReadOnlyList<IAsyncSaveParticipant> participants,
            SaveOperationContext context,
            CancellationToken cancellationToken)
        {
            _enteredParticipants.Clear();
            foreach (IAsyncSaveParticipant participant in participants)
            {
                _enteredParticipants.Add(participant);
                await participant.PrepareSaveAsync(context, cancellationToken);
            }
        }

        public async UniTask ApplyLoadedStateAsync(
            IReadOnlyList<IAsyncSaveParticipant> participants,
            LoadOperationContext context,
            CancellationToken cancellationToken)
        {
            _appliedLoadParticipants.Clear();
            foreach (IAsyncSaveParticipant participant in participants)
            {
                await participant.ApplyLoadedStateAsync(context, cancellationToken);
                _appliedLoadParticipants.Add(participant);
            }
        }

        public async UniTask RollBackLoadedStateAsync(
            LoadOperationContext context,
            CancellationToken cancellationToken,
            Action<IAsyncSaveParticipant,
            Exception> reportFailure)
        {
            for (int participantIndex = _appliedLoadParticipants.Count - 1; participantIndex >= 0; participantIndex--)
            {
                IAsyncSaveParticipant participant = _appliedLoadParticipants[participantIndex];
                try
                {
                    await participant.RollBackLoadedStateAsync(context, cancellationToken);
                }
                catch (Exception exception)
                {
                    reportFailure?.Invoke(participant, exception);
                }
            }

            _appliedLoadParticipants.Clear();
        }

        public async UniTask<IReadOnlyList<string>> CompleteAsync(
            SaveOperationContext context,
            bool didMetadataCommit,
            CancellationToken cancellationToken,
            Action<IAsyncSaveParticipant, Exception> reportFailure)
        {
            List<string> failureReasons = new();
            for (int participantIndex = _enteredParticipants.Count - 1; participantIndex >= 0; participantIndex--)
            {
                IAsyncSaveParticipant participant = _enteredParticipants[participantIndex];
                try
                {
                    await participant.CompleteSaveAsync(context, didMetadataCommit, cancellationToken);
                }
                catch (Exception exception)
                {
                    failureReasons.Add($"{participant.ParticipantId}: {exception.Message}");
                    reportFailure?.Invoke(participant, exception);
                }
            }

            _enteredParticipants.Clear();
            return failureReasons;
        }

        #endregion
    }
}
