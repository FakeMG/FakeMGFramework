using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;

namespace FakeMG.SaveLoad
{
    public sealed class WorldLifecycleAutoSave
    {
        private readonly IWorldAutoSaveRequester _requester;
        private readonly WorldSaveConfiguration _configuration;
        private readonly AutoSaveGate _gate;

        /// <summary>Whether focus loss or quit would save: only with autosave on, a world open, and autosave not suspended.</summary>
        public bool CanFlush => _configuration.IsAutoSaveEnabled && _requester.HasActiveWorld && !_gate.IsSuspended;

        #region Public Methods

        public WorldLifecycleAutoSave(IWorldAutoSaveRequester requester, WorldSaveConfiguration configuration, AutoSaveGate gate)
        {
            _requester = requester;
            _configuration = configuration;
            _gate = gate;
        }

        public UniTask FocusChangedAsync(bool isFocused, CancellationToken cancellationToken)
        {
            return isFocused || !CanFlush ? UniTask.CompletedTask : FlushAsync("application focus loss", cancellationToken);
        }

        public async UniTask FlushAsync(string lifecycleReason, CancellationToken cancellationToken)
        {
            using var flushLifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            try
            {
                UniTask<WorldSaveResult> saveTask = _requester.TriggerAutoSaveAsync(flushLifetime.Token).Preserve();
                UniTask timeoutTask = UniTask.Delay(TimeSpan.FromSeconds(_configuration.FlushTimeoutSeconds),
                    DelayType.UnscaledDeltaTime, PlayerLoopTiming.Update, flushLifetime.Token);
                (bool didSaveComplete, WorldSaveResult result) = await UniTask.WhenAny(saveTask, timeoutTask);
                if (!didSaveComplete)
                {
                    Echo.Error($"World autosave exceeded {_configuration.FlushTimeoutSeconds} seconds during {lifecycleReason}.");
                }
                else if (!result.Succeeded)
                {
                    Echo.Error($"World autosave failed during {lifecycleReason}: {result.FailureReason}");
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Echo.Log($"World autosave during {lifecycleReason} was cancelled during teardown.");
            }
            catch (Exception exception)
            {
                Echo.Error($"World autosave failed during {lifecycleReason}: {exception}");
            }
            finally
            {
                flushLifetime.Cancel();
            }
        }

        #endregion
    }
}
