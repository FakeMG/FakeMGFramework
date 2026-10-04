using System.Collections.Generic;

namespace FakeMG.TimeCycle
{
    public readonly struct CyclePeriodRange
    {
        public CyclePeriodId PeriodId { get; }
        public double StartProgress01 { get; }
        public double DurationProgress01 { get; }

        #region Public Methods

        public CyclePeriodRange(CyclePeriodId periodId, double startProgress01, double durationProgress01)
        {
            PeriodId = periodId;
            StartProgress01 = startProgress01;
            DurationProgress01 = durationProgress01;
        }

        #endregion
    }

    /// <summary>Immutable active cycle geometry. Zero-duration periods remain available for inspection.</summary>
    public sealed class TimeOfCycleLayout
    {
        public double CycleDurationSeconds { get; }
        public IReadOnlyList<CyclePeriodRange> Periods { get; }
        public int MaximumCycleCrossingsPerUpdate { get; }

        #region Public Methods

        public static bool TryCreate(
            double cycleDurationSeconds, IEnumerable<CyclePeriodRange> periods, out TimeOfCycleLayout layout, out string errorMessage)
        {
            layout = null;
            if (!CycleNumericValidation.IsFinite(cycleDurationSeconds) || cycleDurationSeconds <= 0d)
            {
                errorMessage = "Cycle duration must be finite and positive.";
                return false;
            }
            if (periods == null)
            {
                errorMessage = "A cycle layout requires period ranges.";
                return false;
            }
            var ranges = new List<CyclePeriodRange>(periods);
            if (ranges.Count == 0)
            {
                errorMessage = "A cycle layout requires at least one period.";
                return false;
            }

            int activePeriodCount = 0;
            var periodIds = new HashSet<CyclePeriodId>();
            foreach (CyclePeriodRange range in ranges)
            {
                if (!range.PeriodId.IsValid || !CycleProgressConversion.IsValid(range.StartProgress01)
                    || !CycleNumericValidation.IsFiniteNonNegative(range.DurationProgress01) || range.DurationProgress01 > 1d)
                {
                    errorMessage = "A period range requires a valid ID, start in [0, 1), and duration in [0, 1].";
                    return false;
                }
                if (!periodIds.Add(range.PeriodId))
                {
                    errorMessage = $"Period '{range.PeriodId.Value}' is duplicated in the cycle layout.";
                    return false;
                }
                if (range.DurationProgress01 > 0d) activePeriodCount++;
            }
            if (activePeriodCount == 0)
            {
                errorMessage = "A cycle layout requires a positive-duration period.";
                return false;
            }
            layout = new TimeOfCycleLayout(cycleDurationSeconds, ranges, activePeriodCount);
            errorMessage = string.Empty;
            return true;
        }

        #endregion

        #region Private Methods

        private TimeOfCycleLayout(double cycleDurationSeconds, List<CyclePeriodRange> ranges, int activePeriodCount)
        {
            CycleDurationSeconds = cycleDurationSeconds;
            Periods = ranges.AsReadOnly();
            MaximumCycleCrossingsPerUpdate = CycleClock.GetMaximumCycleCrossings(activePeriodCount);
        }

        #endregion
    }
}
