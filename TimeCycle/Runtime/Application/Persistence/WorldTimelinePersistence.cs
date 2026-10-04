using System;
using System.IO;
using FakeMG.Framework;

namespace FakeMG.TimeCycle
{
    [Serializable]
    public sealed class WorldTimelineSaveData
    {
        public double AuthoritativeTimeSeconds;
    }

    public interface IWorldTimelineCaptureSource
    {
        #region Public Methods

        bool TryCapture(out WorldTimelineSaveData saveData, out string failureReason);

        #endregion
    }

    public sealed class WorldTimelinePersistence
    {
        private IWorldTimelineCaptureSource _captureSource;

        private readonly double _startingTimeSeconds;

        public WorldTimelineSaveData SaveData { get; private set; }

        public int Revision { get; private set; }

        #region Public Methods

        public WorldTimelinePersistence(TimeOfCycleProfileSO profileSO)
        {
            _startingTimeSeconds = profileSO.CycleDurationSeconds * profileSO.DefaultStartingProgress01;
            SaveData = CreateStartingState();
        }

        public void Attach(IWorldTimelineCaptureSource captureSource) => _captureSource = captureSource;

        public void Detach(IWorldTimelineCaptureSource captureSource)
        {
            if (ReferenceEquals(_captureSource, captureSource))
            {
                _captureSource = null;
            }
        }

        public WorldTimelineSaveData CaptureForStorage()
        {
            if (_captureSource == null)
            {
                return SaveData;
            }

            if (_captureSource.TryCapture(out WorldTimelineSaveData saveData, out string failureReason))
            {
                return saveData;
            }

            throw new IOException(failureReason);
        }

        public void Restore(WorldTimelineSaveData saveData)
        {
            if (!TryValidate(saveData, out string failureReason))
            {
                Echo.Error(failureReason);
                return;
            }

            SaveData = saveData;
            Revision++;
        }

        public void Reset()
        {
            SaveData = CreateStartingState();
            Revision++;
        }

        public static bool TryValidate(object state, out string failureReason)
        {
            if (state is WorldTimelineSaveData saved && IsFiniteNonnegative(saved.AuthoritativeTimeSeconds))
            {
                failureReason = string.Empty;
                return true;
            }

            failureReason = "World timeline save state is invalid.";
            return false;
        }

        #endregion

        #region Private Methods

        private WorldTimelineSaveData CreateStartingState()
        {
            return new WorldTimelineSaveData { AuthoritativeTimeSeconds = _startingTimeSeconds };
        }

        private static bool IsFiniteNonnegative(double timeSeconds)
        {
            return !double.IsNaN(timeSeconds)
                && !double.IsInfinity(timeSeconds)
                && timeSeconds >= 0d;
        }

        #endregion
    }
}
