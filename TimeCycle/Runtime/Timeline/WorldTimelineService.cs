using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using FakeMG.SceneLoading;
using VContainer.Unity;

namespace FakeMG.TimeCycle
{
    public sealed class WorldTimelineService :
        IWorldTimeline,
        IWorldTimelineCycleRecorder,
        IWorldTimeCommands,
        IWorldTimelineCaptureSource,
        ILoadedSceneDataApplier,
        IInitializable,
        IDisposable
    {
        public const string DATA_APPLIER_ID = "WorldTimeline";

        private readonly ITimeOfCycle _time;
        private readonly WorldTimelinePersistence _persistence;
        private readonly WorldTimeline _timeline;
        private bool _isApplying;
        private int _appliedRevision = -1;

        public double AuthoritativeTimeSeconds => _timeline.GetNormalizedAuthoritativeTimeSeconds(_time.CurrentState.NormalizedCycleProgress01);
        public long CurrentDay => _timeline.CurrentDay;

        public string DataApplierId => DATA_APPLIER_ID;

        #region Public Methods

        public WorldTimelineService(
            ITimeOfCycle time,
            TimeOfCycleProfileSO profileSO,
            WorldTimelinePersistence persistence)
        {
            _time = time;
            _persistence = persistence;
            _timeline = new WorldTimeline(profileSO.CycleDurationSeconds);
        }

        public void Initialize()
        {
            _persistence.Attach(this);
        }

        public void Dispose()
        {
            _persistence.Detach(this);
        }

        public void RecordCompletedCycle() => _timeline.RecordCompletedCycle();

        public UniTask<TimeCommandResult> JumpToDayAsync(
            long dayNumber, double cycleProgress01, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                Echo.Log("World time jump cancelled before execution.");
                return UniTask.FromResult(new TimeCommandResult(0, TimeCommandStatus.Cancelled));
            }

            if (!_time.IsInitialized || _isApplying || dayNumber < 1 || !CycleProgressConversion.IsValid(cycleProgress01))
            {
                return RejectJump("Clock must be initialized and idle, day must be positive, and progress must be in [0, 1).");
            }

            double destinationSeconds = _timeline.GetWorldTimeSeconds(dayNumber, cycleProgress01);
            double distanceCycles = dayNumber - (double)CurrentDay
                + cycleProgress01 - _time.CurrentState.NormalizedCycleProgress01;
            if (!CycleNumericValidation.IsFinite(destinationSeconds) || distanceCycles < 0d)
            {
                return RejectJump("Destination must be finite and cannot precede current world time.");
            }

            if (distanceCycles > _time.ActiveLayout.MaximumCycleCrossingsPerUpdate)
            {
                return RejectJump("Destination exceeds the clock boundary-processing budget.");
            }

            if (distanceCycles == 0d) return UniTask.FromResult(new TimeCommandResult(0, TimeCommandStatus.Completed));

            _isApplying = true;
            try
            {
                TimeCommandResult result = _time.ExecuteImmediateTimeCommand(
                    cycleProgress01 * _time.ActiveLayout.CycleDurationSeconds, CommitDestinationDay, cancellationToken);
                if (result.Status != TimeCommandStatus.Completed)
                {
                    Echo.Warning($"World time jump did not apply: {result.Status}.");
                }

                return UniTask.FromResult(result);
            }
            finally
            {
                _isApplying = false;
            }

            void CommitDestinationDay() => _timeline.SetCurrentDay(dayNumber);
        }

        public bool TryCapture(out WorldTimelineSaveData state, out string failureReason)
        {
            if (_isApplying)
            {
                state = null;
                failureReason = "Cannot capture the world timeline while restoration is in progress.";
                Echo.Warning(failureReason);
                return false;
            }

            state = new WorldTimelineSaveData
            {
                AuthoritativeTimeSeconds = AuthoritativeTimeSeconds
            };
            failureReason = string.Empty;
            return true;
        }

        public async UniTask ApplyLoadedDataAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_isApplying)
            {
                await UniTask.WaitUntil(IsFinishedApplying, cancellationToken: cancellationToken);
            }

            if (_appliedRevision == _persistence.Revision)
            {
                return;
            }

            int revision = _persistence.Revision;
            _isApplying = true;
            using IDisposable pausedClock = _time.RegisterControl(TimeControlRequest.Pause(int.MaxValue, true));
            try
            {
                WorldTimelineSaveData saveData = _persistence.SaveData;
                double progress01 = _timeline.RestoreProgress01(saveData.AuthoritativeTimeSeconds);
                double targetCycleTimeSeconds = progress01 * _time.ActiveLayout.CycleDurationSeconds;

                TimeCommandResult result = await _time.ExecuteTimeCommandAsync(TimeCommand.Immediate(targetCycleTimeSeconds), cancellationToken);
                if (result.Status != TimeCommandStatus.Completed)
                {
                    throw new IOException($"World timeline restoration ended with time command status '{result.Status}'.");
                }

                _appliedRevision = revision;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Echo.Log("World timeline restoration cancelled during scene teardown.");
                throw;
            }
            catch (Exception exception)
            {
                Echo.Error(exception.ToString());
                throw;
            }
            finally
            {
                _isApplying = false;
            }
        }

        #endregion

        #region Private Methods

        private static UniTask<TimeCommandResult> RejectJump(string reason)
        {
            Echo.Warning($"World time jump rejected. {reason}");
            return UniTask.FromResult(new TimeCommandResult(0, TimeCommandStatus.Rejected));
        }

        private bool IsFinishedApplying() => !_isApplying;

        #endregion
    }
}
