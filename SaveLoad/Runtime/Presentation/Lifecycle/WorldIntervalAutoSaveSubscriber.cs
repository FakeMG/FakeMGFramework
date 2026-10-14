using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace FakeMG.SaveLoad
{
    public sealed class WorldIntervalAutoSaveSubscriber : MonoBehaviour, IIntervalAutoSaveControl
    {
        private IntervalAutoSaveService _autoSave;
        private CancellationTokenSource _lifetimeCancellationSource;

        #region Unity Lifecycle

        private void Awake()
        {
            _lifetimeCancellationSource = new CancellationTokenSource();
        }

        private void Update()
        {
            _autoSave.AdvanceAsync(Time.unscaledDeltaTime, _lifetimeCancellationSource.Token).Forget();
        }

        private void OnDestroy()
        {
            _lifetimeCancellationSource.Cancel();
            _lifetimeCancellationSource.Dispose();
            _lifetimeCancellationSource = null;
        }

        #endregion

        #region Public Methods

        public IDisposable SuspendIntervalAutoSave(string reason) => _autoSave.SuspendIntervalAutoSave(reason);

        [Inject]
        public void Construct(IWorldAutoSaveRequester autoSaveRequester, WorldSaveConfiguration configuration)
        {
            _autoSave = new IntervalAutoSaveService(autoSaveRequester, configuration);
        }

        #endregion


    }
}
