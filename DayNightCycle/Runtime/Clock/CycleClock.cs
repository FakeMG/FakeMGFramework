using System;
using System.Collections.Generic;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Owns cyclic authoritative and presentation time plus chronological boundary detection.
    /// </summary>
    internal sealed class CycleClock
    {
        private const double TIME_EPSILON_SECONDS = 0.000001d;
        private const int MAX_BOUNDARY_NOTIFICATION_COUNT = 4096;

        private ResolvedTimeOfCycleConfiguration _configuration;

        public double AuthoritativeTimeSeconds { get; set; }
        public double PresentationTimeSeconds { get; set; }
        public CyclePeriodId CurrentPeriodId { get; private set; }

        #region Public Methods

        public void Configure(
            ResolvedTimeOfCycleConfiguration configuration,
            double authoritativeTimeSeconds,
            double presentationTimeSeconds)
        {
            _configuration = configuration;
            AuthoritativeTimeSeconds = authoritativeTimeSeconds;
            PresentationTimeSeconds = presentationTimeSeconds;
            CurrentPeriodId = ResolvePeriodId(authoritativeTimeSeconds);
        }

        public bool TryAdvanceAuthoritativeTime(
            double signedDeltaSeconds,
            out IReadOnlyList<CycleClockNotification> notifications,
            out string errorMessage)
        {
            List<CycleClockNotification> collectedNotifications = new();
            notifications = collectedNotifications;
            if (!CycleNumericValidation.IsFinite(signedDeltaSeconds))
            {
                errorMessage = "The signed time delta must be finite.";
                return false;
            }

            if (Math.Abs(signedDeltaSeconds) <= TIME_EPSILON_SECONDS)
            {
                errorMessage = null;
                return true;
            }

            int boundariesPerCycle = _configuration.Periods.Count + 1;
            int maximumCycleCrossings = Math.Max(
                1,
                MAX_BOUNDARY_NOTIFICATION_COUNT / boundariesPerCycle);
            double maximumTravelDistanceSeconds =
                _configuration.CycleDurationSeconds * maximumCycleCrossings;
            if (Math.Abs(signedDeltaSeconds) > maximumTravelDistanceSeconds)
            {
                errorMessage =
                    $"One update may cross at most {maximumCycleCrossings} full cycles " +
                    $"({MAX_BOUNDARY_NOTIFICATION_COUNT} boundary notifications).";
                return false;
            }

            List<CycleBoundary> boundaries = CreateBoundaries(AuthoritativeTimeSeconds, signedDeltaSeconds);
            for (int boundaryIndex = 0; boundaryIndex < boundaries.Count; boundaryIndex++)
            {
                CycleBoundary boundary = boundaries[boundaryIndex];
                if (boundary.IsCycleCompletion)
                {
                    collectedNotifications.Add(CycleClockNotification.CycleCompleted());
                    continue;
                }

                AddPeriodChangeIfDifferent(boundary.DestinationPeriodId, collectedNotifications);
            }

            AuthoritativeTimeSeconds = NormalizeTime(AuthoritativeTimeSeconds + signedDeltaSeconds);
            AddPeriodChangeIfDifferent(ResolvePeriodId(AuthoritativeTimeSeconds), collectedNotifications);
            errorMessage = null;
            return true;
        }

        public bool SetAuthoritativeDestination(
            double destinationTimeSeconds,
            out CyclePeriodChange periodChange)
        {
            AuthoritativeTimeSeconds = destinationTimeSeconds;
            CyclePeriodId destinationPeriodId = ResolvePeriodId(destinationTimeSeconds);
            if (CurrentPeriodId.Equals(destinationPeriodId))
            {
                periodChange = default;
                return false;
            }

            periodChange = new CyclePeriodChange(CurrentPeriodId, destinationPeriodId);
            CurrentPeriodId = destinationPeriodId;
            return true;
        }

        public double GetDirectedDistanceSeconds(
            double startTimeSeconds,
            double targetTimeSeconds,
            TimeMovementDirection direction)
        {
            if (Math.Abs(startTimeSeconds - targetTimeSeconds) <= TIME_EPSILON_SECONDS)
            {
                return 0d;
            }

            return direction == TimeMovementDirection.Forward
                ? NormalizeTime(targetTimeSeconds - startTimeSeconds)
                : NormalizeTime(startTimeSeconds - targetTimeSeconds);
        }

        public double NormalizeTime(double timeSeconds)
        {
            double normalizedTimeSeconds = timeSeconds % _configuration.CycleDurationSeconds;
            return normalizedTimeSeconds < 0d
                ? normalizedTimeSeconds + _configuration.CycleDurationSeconds
                : normalizedTimeSeconds;
        }

        #endregion

        #region Private Methods

        private List<CycleBoundary> CreateBoundaries(double startTimeSeconds, double signedDeltaSeconds)
        {
            bool isForward = signedDeltaSeconds > 0d;
            double travelDistanceSeconds = Math.Abs(signedDeltaSeconds);
            double cycleDurationSeconds = _configuration.CycleDurationSeconds;
            List<CycleBoundary> boundaries = new();

            double firstWrapDistanceSeconds = isForward
                ? cycleDurationSeconds - startTimeSeconds
                : startTimeSeconds;
            if (isForward && firstWrapDistanceSeconds <= TIME_EPSILON_SECONDS)
            {
                firstWrapDistanceSeconds = cycleDurationSeconds;
            }

            AddRepeatedBoundaries(
                boundaries,
                firstWrapDistanceSeconds,
                travelDistanceSeconds,
                cycleDurationSeconds,
                CycleBoundary.CycleCompletion);

            for (int periodIndex = 0; periodIndex < _configuration.Periods.Count; periodIndex++)
            {
                CyclePeriodDefinition period = _configuration.Periods[periodIndex];
                double firstBoundaryDistanceSeconds;
                CyclePeriodId destinationPeriodId;
                if (isForward)
                {
                    firstBoundaryDistanceSeconds = period.StartTimeSeconds - startTimeSeconds;
                    if (firstBoundaryDistanceSeconds <= TIME_EPSILON_SECONDS)
                    {
                        firstBoundaryDistanceSeconds += cycleDurationSeconds;
                    }

                    destinationPeriodId = period.PeriodId;
                }
                else
                {
                    firstBoundaryDistanceSeconds = startTimeSeconds - period.StartTimeSeconds;
                    if (firstBoundaryDistanceSeconds < -TIME_EPSILON_SECONDS)
                    {
                        firstBoundaryDistanceSeconds += cycleDurationSeconds;
                    }
                    else if (Math.Abs(firstBoundaryDistanceSeconds) <= TIME_EPSILON_SECONDS)
                    {
                        firstBoundaryDistanceSeconds = 0d;
                    }

                    int previousPeriodIndex = periodIndex == 0
                        ? _configuration.Periods.Count - 1
                        : periodIndex - 1;
                    destinationPeriodId = _configuration.Periods[previousPeriodIndex].PeriodId;
                }

                AddRepeatedBoundaries(
                    boundaries,
                    firstBoundaryDistanceSeconds,
                    travelDistanceSeconds,
                    cycleDurationSeconds,
                    distanceSeconds => CycleBoundary.PeriodBoundary(distanceSeconds, destinationPeriodId));
            }

            boundaries.Sort(CycleBoundary.Compare);
            return boundaries;
        }

        private static void AddRepeatedBoundaries(
            ICollection<CycleBoundary> boundaries,
            double firstBoundaryDistanceSeconds,
            double travelDistanceSeconds,
            double cycleDurationSeconds,
            Func<double, CycleBoundary> createBoundary)
        {
            for (double boundaryDistanceSeconds = firstBoundaryDistanceSeconds;
                 boundaryDistanceSeconds <= travelDistanceSeconds + TIME_EPSILON_SECONDS;
                 boundaryDistanceSeconds += cycleDurationSeconds)
            {
                if (boundaryDistanceSeconds < -TIME_EPSILON_SECONDS)
                {
                    continue;
                }

                boundaries.Add(createBoundary(Math.Max(0d, boundaryDistanceSeconds)));
            }
        }

        private void AddPeriodChangeIfDifferent(
            CyclePeriodId destinationPeriodId,
            ICollection<CycleClockNotification> notifications)
        {
            if (CurrentPeriodId.Equals(destinationPeriodId))
            {
                return;
            }

            CyclePeriodChange periodChange = new(CurrentPeriodId, destinationPeriodId);
            CurrentPeriodId = destinationPeriodId;
            notifications.Add(CycleClockNotification.PeriodChanged(periodChange));
        }

        private CyclePeriodId ResolvePeriodId(double timeSeconds)
        {
            CyclePeriodId selectedPeriodId = _configuration.Periods[_configuration.Periods.Count - 1].PeriodId;
            for (int periodIndex = 0; periodIndex < _configuration.Periods.Count; periodIndex++)
            {
                CyclePeriodDefinition period = _configuration.Periods[periodIndex];
                if (period.StartTimeSeconds > timeSeconds)
                {
                    break;
                }

                selectedPeriodId = period.PeriodId;
            }

            return selectedPeriodId;
        }

        #endregion

        /// <summary>
        /// Stores one sortable cycle or period boundary.
        /// </summary>
        private readonly struct CycleBoundary
        {
            public double DistanceSeconds { get; }
            public bool IsCycleCompletion { get; }
            public CyclePeriodId DestinationPeriodId { get; }

            private CycleBoundary(
                double distanceSeconds,
                bool isCycleCompletion,
                CyclePeriodId destinationPeriodId)
            {
                DistanceSeconds = distanceSeconds;
                IsCycleCompletion = isCycleCompletion;
                DestinationPeriodId = destinationPeriodId;
            }

            #region Public Methods

            public static CycleBoundary CycleCompletion(double distanceSeconds)
            {
                return new CycleBoundary(distanceSeconds, true, default);
            }

            public static CycleBoundary PeriodBoundary(
                double distanceSeconds,
                CyclePeriodId destinationPeriodId)
            {
                return new CycleBoundary(distanceSeconds, false, destinationPeriodId);
            }

            public static int Compare(CycleBoundary left, CycleBoundary right)
            {
                int distanceComparison = left.DistanceSeconds.CompareTo(right.DistanceSeconds);
                return distanceComparison != 0
                    ? distanceComparison
                    : right.IsCycleCompletion.CompareTo(left.IsCycleCompletion);
            }

            #endregion
        }
    }
}
