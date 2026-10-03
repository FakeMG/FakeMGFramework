using System;

namespace FakeMG.TimeCycle
{
    internal sealed class WorldTimeline
    {
        private readonly double _cycleDurationSeconds;
        private double _completedCycleSeconds;
        public long CurrentDay => (long)Math.Round(_completedCycleSeconds / _cycleDurationSeconds) + 1;

        #region Public Methods

        public WorldTimeline(double cycleDurationSeconds)
        {
            if (double.IsNaN(cycleDurationSeconds) || double.IsInfinity(cycleDurationSeconds) || cycleDurationSeconds <= 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cycleDurationSeconds),
                    cycleDurationSeconds,
                    "Cycle duration must be finite and positive.");
            }

            _cycleDurationSeconds = cycleDurationSeconds;
        }

        public double GetAuthoritativeTimeSeconds(double cycleTimeSeconds) => _completedCycleSeconds + cycleTimeSeconds;

        public double GetNormalizedAuthoritativeTimeSeconds(double progress01) => _completedCycleSeconds + progress01 * _cycleDurationSeconds;

        public double GetWorldTimeSeconds(long dayNumber, double progress01) => ((dayNumber - 1d) + progress01) * _cycleDurationSeconds;

        public void SetCurrentDay(long dayNumber) => _completedCycleSeconds = (dayNumber - 1d) * _cycleDurationSeconds;

        public void RecordCompletedCycle() => _completedCycleSeconds += _cycleDurationSeconds;

        public double RestoreProgress01(double authoritativeTimeSeconds)
        {
            return RestoreAuthoritativeTime(authoritativeTimeSeconds) / _cycleDurationSeconds;
        }

        public double RestoreAuthoritativeTime(double authoritativeTimeSeconds)
        {
            if (double.IsNaN(authoritativeTimeSeconds) || double.IsInfinity(authoritativeTimeSeconds) || authoritativeTimeSeconds < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(authoritativeTimeSeconds),
                    authoritativeTimeSeconds,
                    "Authoritative time must be finite and nonnegative.");
            }

            _completedCycleSeconds = Math.Floor(authoritativeTimeSeconds / _cycleDurationSeconds) * _cycleDurationSeconds;
            return authoritativeTimeSeconds - _completedCycleSeconds;
        }

        public void ResetCompletedCycles() => _completedCycleSeconds = 0d;

        #endregion
    }
}
