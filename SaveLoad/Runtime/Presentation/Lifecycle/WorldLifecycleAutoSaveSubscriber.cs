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
        private WorldLifecycleAutoSave _autoSave;
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
            _autoSave.FocusChangedAsync(isFocused, _lifetimeCancellationSource.Token).Forget();
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
            _autoSave = new WorldLifecycleAutoSave(autoSaveRequester, configuration);
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
            await _autoSave.FlushAsync("application quit", _lifetimeCancellationSource.Token);
            _canQuit = true;
            Application.Quit();
        }

        #endregion
    }
}
