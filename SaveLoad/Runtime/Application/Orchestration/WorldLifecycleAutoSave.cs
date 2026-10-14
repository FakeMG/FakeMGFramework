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

        #region Public Methods

        public WorldLifecycleAutoSave(IWorldAutoSaveRequester requester, WorldSaveConfiguration configuration)
        {
            _requester = requester;
            _configuration = configuration;
        }

        public UniTask FocusChangedAsync(bool isFocused, CancellationToken cancellationToken)
        {
            return isFocused || !_requester.HasActiveWorld
                ? UniTask.CompletedTask : FlushAsync("application focus loss", cancellationToken);
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
