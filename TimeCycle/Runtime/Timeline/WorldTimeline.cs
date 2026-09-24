using System;

namespace FakeMG.TimeCycle
{
    internal sealed class WorldTimeline
    {
        private readonly double _cycleDurationSeconds;
        private double _completedCycleSeconds;

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

        public void RecordCompletedCycle() => _completedCycleSeconds += _cycleDurationSeconds;

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
