using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;

namespace FakeMG.SaveLoad
{
    /// <summary>
    /// Requests an autosave each time the configured interval passes while a world is open. A suspension freezes the
    /// countdown; with no world open it restarts, so every play session waits a full interval before its first autosave.
    /// </summary>
    public sealed class IntervalAutoSaveService
    {
        private readonly IWorldAutoSaveRequester _requester;
        private readonly WorldSaveConfiguration _configuration;
        private readonly AutoSaveGate _gate;
        private readonly AutoSaveSchedule _schedule;

        #region Public Methods

        public IntervalAutoSaveService(IWorldAutoSaveRequester requester, WorldSaveConfiguration configuration, AutoSaveGate gate)
        {
            _requester = requester;
            _configuration = configuration;
            _gate = gate;
            _schedule = new AutoSaveSchedule(configuration.AutoSaveIntervalSeconds);
        }

        public async UniTask AdvanceAsync(float elapsedSeconds, CancellationToken cancellationToken)
        {
            if (!_requester.HasActiveWorld)
            {
                _schedule.Reset(_configuration.AutoSaveIntervalSeconds);
                return;
            }

            if (_gate.IsSuspended || !_configuration.IsAutoSaveEnabled || !_schedule.Advance(elapsedSeconds))
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
