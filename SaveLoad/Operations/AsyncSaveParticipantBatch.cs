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

        #region Public Methods

        public async UniTask PrepareAsync(
            IReadOnlyList<IAsyncSaveParticipant> participants, SaveOperationContext context,
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
            IReadOnlyList<IAsyncSaveParticipant> participants, LoadOperationContext context,
            CancellationToken cancellationToken)
        {
            foreach (IAsyncSaveParticipant participant in participants)
            {
                await participant.ApplyLoadedStateAsync(context, cancellationToken);
            }
        }

        public async UniTask CompleteAsync(
            SaveOperationContext context, bool didMetadataCommit,
            CancellationToken cancellationToken, Action<IAsyncSaveParticipant, Exception> reportFailure)
        {
            for (int participantIndex = _enteredParticipants.Count - 1; participantIndex >= 0; participantIndex--)
            {
                IAsyncSaveParticipant participant = _enteredParticipants[participantIndex];
                try
                {
                    await participant.CompleteSaveAsync(context, didMetadataCommit, cancellationToken);
                }
                catch (Exception exception)
                {
                    reportFailure?.Invoke(participant, exception);
                }
            }

            _enteredParticipants.Clear();
        }

        #endregion
    }
}
