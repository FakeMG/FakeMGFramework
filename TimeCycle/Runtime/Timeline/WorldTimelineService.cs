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

        public double AuthoritativeTimeSeconds => _timeline.GetAuthoritativeTimeSeconds(_time.CurrentState.CycleTimeSeconds);

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
            _time.OnCycleCompleted += RecordCompletedCycle;
            _persistence.Attach(this);
        }

        public void Dispose()
        {
            _time.OnCycleCompleted -= RecordCompletedCycle;
            _persistence.Detach(this);
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
                double targetCycleTimeSeconds = _timeline.RestoreAuthoritativeTime(saveData.AuthoritativeTimeSeconds);

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

        private void RecordCompletedCycle() => _timeline.RecordCompletedCycle();

        private bool IsFinishedApplying() => !_isApplying;

        #endregion
    }
}
