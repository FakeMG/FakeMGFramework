using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;

namespace FakeMG.SaveLoad
{
    public sealed class IntervalAutoSaveService : IIntervalAutoSaveControl
    {
        private readonly IWorldAutoSaveRequester _requester;
        private readonly WorldSaveConfiguration _configuration;
        private readonly AutoSaveSchedule _schedule;
        private readonly IntervalAutoSaveGate _suspensions = new();

        #region Public Methods

        public IntervalAutoSaveService(IWorldAutoSaveRequester requester, WorldSaveConfiguration configuration)
        {
            _requester = requester;
            _configuration = configuration;
            _schedule = new AutoSaveSchedule(configuration.AutoSaveIntervalSeconds);
        }

        public IDisposable SuspendIntervalAutoSave(string reason) => _suspensions.SuspendIntervalAutoSave(reason);

        public async UniTask AdvanceAsync(float elapsedSeconds, CancellationToken cancellationToken)
        {
            if (_suspensions.IsSuspended
                || !_configuration.IsAutoSaveEnabled
                || !_requester.HasActiveWorld
                || !_schedule.Advance(elapsedSeconds))
            {
                return;
            }

            try
            {
                WorldSaveResult result = await _requester.TriggerAutoSaveAsync(cancellationToken);
                if (!result.Succeeded)
                {
                    Echo.Error($"Interval autosave failed: {result.FailureReason}");
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Echo.Log("Interval autosave wait was cancelled during teardown.");
            }
            catch (Exception exception)
            {
                Echo.Error($"Interval autosave failed: {exception}");
            }
        }

        #endregion
    }
}
