using System;
using VContainer.Unity;

namespace FakeMG.TimeCycle
{
    public sealed class WorldTimelineCycleSubscriber : IInitializable, IDisposable
    {
        private readonly ITimeOfCycle _time;
        private readonly IWorldTimelineCycleRecorder _timeline;

        #region Public Methods

        public WorldTimelineCycleSubscriber(ITimeOfCycle time, IWorldTimelineCycleRecorder timeline)
        {
            _time = time;
            _timeline = timeline;
        }

        public void Initialize() => _time.OnCycleCompleted += RecordCompletedCycle;
        public void Dispose() => _time.OnCycleCompleted -= RecordCompletedCycle;

        #endregion

        #region Private Methods

        private void RecordCompletedCycle() => _timeline.RecordCompletedCycle();

        #endregion
    }
}
