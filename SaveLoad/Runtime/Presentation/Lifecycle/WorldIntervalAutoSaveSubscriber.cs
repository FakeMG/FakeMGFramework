using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace FakeMG.SaveLoad
{
    public sealed class WorldIntervalAutoSaveSubscriber : MonoBehaviour
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

        [Inject]
        public void Construct(IWorldAutoSaveRequester autoSaveRequester, WorldSaveConfiguration configuration, AutoSaveGate gate)
        {
            _autoSave = new IntervalAutoSaveService(autoSaveRequester, configuration, gate);
        }

        #endregion
    }
}
