using System;
using System.IO;
using FakeMG.Framework;

namespace FakeMG.TimeCycle
{
    [Serializable]
    public sealed class WorldTimelineSaveData
    {
        public bool HasWorld;
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

        public WorldTimelineSaveData SaveData { get; private set; } = new();

        public int Revision { get; private set; }

        public event Action OnRestoreRequested;

        #region Public Methods

        public void Attach(IWorldTimelineCaptureSource captureSource) => _captureSource = captureSource;

        public void Detach(IWorldTimelineCaptureSource captureSource)
        {
            if (ReferenceEquals(_captureSource, captureSource))
                _captureSource = null;
        }

        public WorldTimelineSaveData CaptureForStorage()
        {
            if (_captureSource == null)
                return SaveData;

            if (_captureSource.TryCapture(out WorldTimelineSaveData saveData, out string failureReason))
                return saveData;

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
            OnRestoreRequested?.Invoke();
        }

        public void Reset()
        {
            SaveData = new WorldTimelineSaveData();
            Revision++;
            OnRestoreRequested?.Invoke();
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

        private static bool IsFiniteNonnegative(double timeSeconds)
        {
            return !double.IsNaN(timeSeconds)
                && !double.IsInfinity(timeSeconds)
                && timeSeconds >= 0d;
        }

        #endregion
    }
}
