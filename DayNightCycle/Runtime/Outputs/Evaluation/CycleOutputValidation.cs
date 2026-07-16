using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Centralizes validation shared by concrete output-definition types.
    /// </summary>
    internal static class CycleOutputValidation
    {
        #region Public Methods

        public static bool TryValidateContinuousPoints<TPoint, TValue>(
            IReadOnlyList<TPoint> points,
            double cycleDurationSeconds,
            AnimationCurve interpolationCurve,
            out string errorMessage)
            where TPoint : IContinuousCyclePoint<TValue>
        {
            if (!CycleCurveValidation.TryValidate(interpolationCurve, out errorMessage))
            {
                return false;
            }

            if (points.Count == 0)
            {
                errorMessage = "At least one timeline point is required.";
                return false;
            }

            return TryValidateUniqueTimes<TPoint, TValue>(points, cycleDurationSeconds, out errorMessage);
        }

        public static bool TryValidateDiscretePoints<TPoint, TPeriodValue, TValue>(
            IReadOnlyList<TPoint> timelinePoints,
            IReadOnlyList<TPeriodValue> periodValues,
            double cycleDurationSeconds,
            ISet<CyclePeriodId> periodIds,
            out string errorMessage)
            where TPoint : IDiscreteCyclePoint<TValue>
            where TPeriodValue : IPeriodCycleValue<TValue>
        {
            if (timelinePoints.Count == 0 && periodValues.Count == 0)
            {
                errorMessage = "At least one timeline point or period value is required.";
                return false;
            }

            if (!TryValidateUniqueDiscreteTimes<TPoint, TValue>(
                    timelinePoints,
                    cycleDurationSeconds,
                    out errorMessage))
            {
                return false;
            }

            HashSet<CyclePeriodId> configuredPeriodIds = new();
            for (int valueIndex = 0; valueIndex < periodValues.Count; valueIndex++)
            {
                TPeriodValue periodValue = periodValues[valueIndex];
                if (periodValue == null || !periodIds.Contains(periodValue.PeriodId))
                {
                    errorMessage = $"Period value at index {valueIndex} is null or references an unknown period.";
                    return false;
                }

                if (!configuredPeriodIds.Add(periodValue.PeriodId))
                {
                    errorMessage = $"Period '{periodValue.PeriodId}' has more than one value.";
                    return false;
                }
            }

            errorMessage = null;
            return true;
        }

        #endregion

        #region Private Methods

        private static bool TryValidateUniqueTimes<TPoint, TValue>(
            IReadOnlyList<TPoint> points,
            double cycleDurationSeconds,
            out string errorMessage)
            where TPoint : IContinuousCyclePoint<TValue>
        {
            HashSet<double> timesSeconds = new();
            for (int pointIndex = 0; pointIndex < points.Count; pointIndex++)
            {
                TPoint point = points[pointIndex];
                if (point == null || !TryAddTime(point.TimeSeconds, cycleDurationSeconds, timesSeconds))
                {
                    errorMessage = $"Timeline point at index {pointIndex} is null, outside the cycle, or duplicated.";
                    return false;
                }
            }

            errorMessage = null;
            return true;
        }

        private static bool TryValidateUniqueDiscreteTimes<TPoint, TValue>(
            IReadOnlyList<TPoint> points,
            double cycleDurationSeconds,
            out string errorMessage)
            where TPoint : IDiscreteCyclePoint<TValue>
        {
            HashSet<double> timesSeconds = new();
            for (int pointIndex = 0; pointIndex < points.Count; pointIndex++)
            {
                TPoint point = points[pointIndex];
                if (point == null || !TryAddTime(point.TimeSeconds, cycleDurationSeconds, timesSeconds))
                {
                    errorMessage = $"Timeline point at index {pointIndex} is null, outside the cycle, or duplicated.";
                    return false;
                }
            }

            errorMessage = null;
            return true;
        }

        private static bool TryAddTime(
            double timeSeconds,
            double cycleDurationSeconds,
            ISet<double> timesSeconds)
        {
            return CycleNumericValidation.IsFinite(timeSeconds)
                   && timeSeconds >= 0d
                   && timeSeconds < cycleDurationSeconds
                   && timesSeconds.Add(timeSeconds);
        }

        #endregion
    }
}
