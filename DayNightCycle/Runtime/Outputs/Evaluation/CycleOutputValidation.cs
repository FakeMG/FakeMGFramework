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

            return TryValidateUniqueProgress<TPoint, TValue>(points, out errorMessage);
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

            if (!TryValidateUniqueDiscreteProgress<TPoint, TValue>(timelinePoints, out errorMessage))
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

        private static bool TryValidateUniqueProgress<TPoint, TValue>(IReadOnlyList<TPoint> points, out string errorMessage)
            where TPoint : IContinuousCyclePoint<TValue>
        {
            HashSet<double> progressPositions01 = new();
            for (int pointIndex = 0; pointIndex < points.Count; pointIndex++)
            {
                TPoint point = points[pointIndex];
                if (point == null || !TryAddProgress(point.PointProgress01, progressPositions01))
                {
                    errorMessage = $"Timeline point at index {pointIndex} is null, outside [0, 1), or duplicated.";
                    return false;
                }
            }

            errorMessage = null;
            return true;
        }

        private static bool TryValidateUniqueDiscreteProgress<TPoint, TValue>(IReadOnlyList<TPoint> points, out string errorMessage)
            where TPoint : IDiscreteCyclePoint<TValue>
        {
            HashSet<double> progressPositions01 = new();
            for (int pointIndex = 0; pointIndex < points.Count; pointIndex++)
            {
                TPoint point = points[pointIndex];
                if (point == null || !TryAddProgress(point.PointProgress01, progressPositions01))
                {
                    errorMessage = $"Timeline point at index {pointIndex} is null, outside [0, 1), or duplicated.";
                    return false;
                }
            }

            errorMessage = null;
            return true;
        }

        private static bool TryAddProgress(double progress01, ISet<double> progressPositions01)
        {
            return CycleProgressConversion.IsValid(progress01) && progressPositions01.Add(progress01);
        }

        #endregion
    }
}
