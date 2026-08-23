using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using UnityEngine;
using VContainer;

namespace FakeMG.SaveLoad
{
    public sealed class WorldLifecycleAutoSaveSubscriber : MonoBehaviour
    {
        private IWorldAutoSaveRequester _autoSaveRequester;
        private WorldSaveConfiguration _configuration;
        private CancellationTokenSource _lifetimeCancellationSource;
        private bool _canQuit;
        private bool _isQuitFlushRunning;

        #region Unity Lifecycle

        private void Awake()
        {
            _lifetimeCancellationSource = new CancellationTokenSource();
        }

#if UNITY_STANDALONE && !UNITY_EDITOR
        private void OnEnable()
        {
            Application.wantsToQuit += DelayQuitUntilAutoSaveFinishes;
        }

        private void OnDisable()
        {
            Application.wantsToQuit -= DelayQuitUntilAutoSaveFinishes;
        }
#endif

        private void OnApplicationFocus(bool isFocused)
        {
            if (isFocused || !_autoSaveRequester.HasActiveWorld)
            {
                return;
            }

            FlushAutoSaveWithTimeoutSafelyAsync("application focus loss", _lifetimeCancellationSource.Token).Forget();
        }

        private void OnDestroy()
        {
            _lifetimeCancellationSource.Cancel();
            _lifetimeCancellationSource.Dispose();
            _lifetimeCancellationSource = null;
        }

        #endregion

        #region Public Methods

        [Inject]
        public void Construct(
            IWorldAutoSaveRequester autoSaveRequester,
            WorldSaveConfiguration configuration)
        {
            _autoSaveRequester = autoSaveRequester;
            _configuration = configuration;
        }

        #endregion

        #region Private Methods

        private bool DelayQuitUntilAutoSaveFinishes()
        {
            if (_canQuit || !_autoSaveRequester.HasActiveWorld)
            {
                return true;
            }

            if (!_isQuitFlushRunning)
            {
                _isQuitFlushRunning = true;
                FlushThenQuitSafelyAsync().Forget();
            }

            return false;
        }

        private async UniTaskVoid FlushThenQuitSafelyAsync()
        {
            await FlushAutoSaveWithTimeoutSafelyAsync("application quit", _lifetimeCancellationSource.Token);
            _canQuit = true;
            Application.Quit();
        }

        private async UniTask FlushAutoSaveWithTimeoutSafelyAsync(string lifecycleReason, CancellationToken cancellationToken)
        {
            try
            {
                UniTask<WorldSaveResult> saveTask = _autoSaveRequester.TriggerAutoSaveAsync(cancellationToken).Preserve();
                UniTask timeoutTask = UniTask.Delay(
                    TimeSpan.FromSeconds(_configuration.FlushTimeoutSeconds),
                    DelayType.UnscaledDeltaTime,
                    PlayerLoopTiming.Update,
                    cancellationToken);
                (bool didSaveComplete, WorldSaveResult result) = await UniTask.WhenAny(saveTask, timeoutTask);
                if (!didSaveComplete)
                {
                    Echo.Error(
                        $"World autosave exceeded {_configuration.FlushTimeoutSeconds} seconds " +
                        $"during {lifecycleReason}.",
                        context: this);
                    return;
                }

                if (!result.Succeeded)
                {
                    Echo.Error($"World autosave failed during {lifecycleReason}: {result.FailureReason}", context: this);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Echo.Log($"World autosave during {lifecycleReason} was cancelled during teardown.");
            }
            catch (Exception exception)
            {
                Echo.Error($"World autosave failed during {lifecycleReason}: {exception}", context: this);
            }
        }

        #endregion
    }
}
