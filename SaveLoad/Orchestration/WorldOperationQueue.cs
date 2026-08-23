using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;

namespace FakeMG.SaveLoad
{
    public enum WorldSnapshotKind
    {
        Manual = 0,
        Auto = 1,
    }

    public sealed class WorldOperationQueue : IDisposable
    {
        private readonly object _queueLock = new();
        private readonly LinkedList<IQueuedWorldOperation> _pendingOperations = new();
        private readonly CancellationTokenSource _lifetimeCancellationSource = new();
        private QueuedWorldOperation<WorldSaveResult> _pendingAutoSave;
        private bool _isProcessing;
        private bool _isDisposed;

        public bool IsProcessing
        {
            get
            {
                lock (_queueLock)
                {
                    return _isProcessing || _pendingOperations.Count > 0;
                }
            }
        }

        #region Public Methods

        public UniTask<WorldSaveResult> EnqueueSaveAsync(
            WorldSnapshotKind snapshotKind,
            Func<CancellationToken, UniTask<WorldSaveResult>> operationAsync,
            CancellationToken callerCancellationToken)
        {
            lock (_queueLock)
            {
                ThrowIfDisposed();
                if (snapshotKind == WorldSnapshotKind.Auto && _pendingAutoSave != null)
                {
                    return _pendingAutoSave.Task.AttachExternalCancellation(callerCancellationToken);
                }

                var operation = new QueuedWorldOperation<WorldSaveResult>(
                    operationAsync,
                    snapshotKind == WorldSnapshotKind.Auto);
                if (snapshotKind == WorldSnapshotKind.Manual)
                {
                    InsertManualBeforePendingAutoSaves(operation);
                }
                else
                {
                    _pendingOperations.AddLast(operation);
                    _pendingAutoSave = operation;
                }

                StartProcessingIfNeeded();
                return operation.Task.AttachExternalCancellation(callerCancellationToken);
            }
        }

        public UniTask<TResult> EnqueueOperationAsync<TResult>(
            Func<CancellationToken, UniTask<TResult>> operationAsync,
            CancellationToken callerCancellationToken)
        {
            lock (_queueLock)
            {
                ThrowIfDisposed();
                var operation = new QueuedWorldOperation<TResult>(operationAsync, false);
                _pendingOperations.AddLast(operation);
                StartProcessingIfNeeded();
                return operation.Task.AttachExternalCancellation(callerCancellationToken);
            }
        }

        public void Dispose()
        {
            lock (_queueLock)
            {
                if (_isDisposed)
                {
                    return;
                }

                _isDisposed = true;
                _lifetimeCancellationSource.Cancel();
                foreach (IQueuedWorldOperation operation in _pendingOperations)
                {
                    operation.Cancel(_lifetimeCancellationSource.Token);
                }

                _pendingOperations.Clear();
                _pendingAutoSave = null;
            }

            _lifetimeCancellationSource.Dispose();
        }

        #endregion

        #region Private Methods

        private void InsertManualBeforePendingAutoSaves(IQueuedWorldOperation manualOperation)
        {
            LinkedListNode<IQueuedWorldOperation> node = _pendingOperations.First;
            while (node != null && !node.Value.IsAutoSave)
            {
                node = node.Next;
            }

            if (node == null)
            {
                _pendingOperations.AddLast(manualOperation);
            }
            else
            {
                _pendingOperations.AddBefore(node, manualOperation);
            }
        }

        private void StartProcessingIfNeeded()
        {
            if (_isProcessing)
            {
                return;
            }

            _isProcessing = true;
            ProcessOperationsAndReportFailuresAsync().Forget();
        }

        private async UniTaskVoid ProcessOperationsAndReportFailuresAsync()
        {
            try
            {
                while (true)
                {
                    IQueuedWorldOperation operation;
                    lock (_queueLock)
                    {
                        if (_pendingOperations.Count == 0 || _isDisposed)
                        {
                            _isProcessing = false;
                            return;
                        }

                        operation = _pendingOperations.First.Value;
                        _pendingOperations.RemoveFirst();
                        if (ReferenceEquals(operation, _pendingAutoSave))
                        {
                            _pendingAutoSave = null;
                        }
                    }

                    await operation.ExecuteAsync(_lifetimeCancellationSource.Token);
                }
            }
            catch (OperationCanceledException) when (_lifetimeCancellationSource.IsCancellationRequested)
            {
                // Disposal cancellation is expected and every queued caller has already been completed.
            }
            catch (Exception exception)
            {
                Echo.Error($"World operation queue stopped unexpectedly: {exception}");
                lock (_queueLock)
                {
                    _isProcessing = false;
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(WorldOperationQueue));
            }
        }

        private interface IQueuedWorldOperation
        {
            bool IsAutoSave { get; }
            UniTask ExecuteAsync(CancellationToken cancellationToken);
            void Cancel(CancellationToken cancellationToken);
        }

        private sealed class QueuedWorldOperation<TResult> : IQueuedWorldOperation
        {
            private readonly Func<CancellationToken, UniTask<TResult>> _operationAsync;
            private readonly UniTaskCompletionSource<TResult> _completionSource = new();

            public bool IsAutoSave { get; }
            public UniTask<TResult> Task => _completionSource.Task;

            public QueuedWorldOperation(
                Func<CancellationToken, UniTask<TResult>> operationAsync,
                bool isAutoSave)
            {
                _operationAsync = operationAsync ?? throw new ArgumentNullException(nameof(operationAsync));
                IsAutoSave = isAutoSave;
            }

            public async UniTask ExecuteAsync(CancellationToken cancellationToken)
            {
                try
                {
                    TResult result = await _operationAsync(cancellationToken);
                    _completionSource.TrySetResult(result);
                }
                catch (OperationCanceledException)
                {
                    _completionSource.TrySetCanceled(cancellationToken);
                }
                catch (Exception exception)
                {
                    _completionSource.TrySetException(exception);
                }
            }

            public void Cancel(CancellationToken cancellationToken)
            {
                _completionSource.TrySetCanceled(cancellationToken);
            }
        }

        #endregion
    }
}
