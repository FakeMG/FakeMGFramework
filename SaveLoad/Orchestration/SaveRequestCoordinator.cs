using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace FakeMG.SaveLoad
{
    public delegate UniTask<bool> SaveOperationAsync(SaveFileKind saveKind, CancellationToken cancellationToken);

    /// <summary>
    /// Serializes and coalesces save requests under an operation-owned cancellation token. Caller
    /// tokens cancel only their waits, so a short-lived UI request cannot abort a lifecycle flush.
    /// </summary>
    public sealed class SaveRequestCoordinator
    {
        private readonly SaveOperationAsync _saveOperationAsync;
        private readonly CancellationToken _operationCancellationToken;
        private UniTaskCompletionSource<bool> _activeCompletionSource;
        private SaveFileKind _selectedSaveKind;
        private bool _hasStartedOperation;

        public bool IsSaving => _activeCompletionSource != null;

        public SaveRequestCoordinator(SaveOperationAsync saveOperationAsync, CancellationToken operationCancellationToken)
        {
            _saveOperationAsync = saveOperationAsync;
            _operationCancellationToken = operationCancellationToken;
        }

        #region Public Methods

        public UniTask<bool> RequestAsync(SaveFileKind requestedSaveKind, CancellationToken callerCancellationToken)
        {
            if (_activeCompletionSource != null)
            {
                if (!_hasStartedOperation)
                {
                    _selectedSaveKind = SelectHigherPriority(_selectedSaveKind, requestedSaveKind);
                }

                return _activeCompletionSource.Task.AttachExternalCancellation(callerCancellationToken);
            }

            _activeCompletionSource = new UniTaskCompletionSource<bool>();
            _selectedSaveKind = requestedSaveKind;
            _hasStartedOperation = false;
            RunAsync().Forget();
            return _activeCompletionSource.Task.AttachExternalCancellation(callerCancellationToken);
        }

        public UniTask<bool> WaitForActiveSaveAsync(CancellationToken callerCancellationToken)
        {
            return _activeCompletionSource == null
                ? UniTask.FromResult(true)
                : _activeCompletionSource.Task.AttachExternalCancellation(callerCancellationToken);
        }

        #endregion

        #region Private Methods

        private async UniTaskVoid RunAsync()
        {
            UniTaskCompletionSource<bool> operationCompletionSource = _activeCompletionSource;
            try
            {
                await UniTask.Yield(PlayerLoopTiming.Update, _operationCancellationToken);
                _hasStartedOperation = true;
                bool didSaveSucceed = await _saveOperationAsync(_selectedSaveKind, _operationCancellationToken);
                operationCompletionSource.TrySetResult(didSaveSucceed);
            }
            catch (OperationCanceledException)
            {
                operationCompletionSource.TrySetCanceled(_operationCancellationToken);
            }
            catch (Exception exception)
            {
                operationCompletionSource.TrySetException(exception);
            }
            finally
            {
                if (ReferenceEquals(_activeCompletionSource, operationCompletionSource))
                {
                    _activeCompletionSource = null;
                    _selectedSaveKind = SaveFileKind.Unknown;
                    _hasStartedOperation = false;
                }
            }
        }

        private SaveFileKind SelectHigherPriority(SaveFileKind currentSaveKind, SaveFileKind requestedSaveKind)
        {
            if (currentSaveKind == SaveFileKind.Unknown)
            {
                return requestedSaveKind;
            }

            return GetPriority(requestedSaveKind) >
                   GetPriority(currentSaveKind)
                ? requestedSaveKind
                : currentSaveKind;
        }

        private static int GetPriority(SaveFileKind saveKind)
        {
            return saveKind == SaveFileKind.Auto ? 0 : 100;
        }

        #endregion
    }
}
