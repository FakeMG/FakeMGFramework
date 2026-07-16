using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Centralizes evaluator construction shared by concrete Unity-serializable output definitions.
    /// </summary>
    internal static class CycleOutputDefinitionBuilder
    {
        #region Public Methods

        public static ICycleOutputEvaluator CreateContinuous<TPoint, TValue>(
            double cycleDurationSeconds,
            IReadOnlyList<TPoint> points,
            AnimationCurve interpolationCurve,
            Func<TValue, TValue, float, TValue> interpolate)
            where TPoint : IContinuousCyclePoint<TValue>
        {
            List<ContinuousCycleValue<TValue>> values = new(points.Count);
            for (int pointIndex = 0; pointIndex < points.Count; pointIndex++)
            {
                TPoint point = points[pointIndex];
                values.Add(new ContinuousCycleValue<TValue>(point.TimeSeconds, point.Value));
            }

            return new ContinuousCycleOutputEvaluator<TValue>(
                cycleDurationSeconds,
                values,
                interpolationCurve,
                interpolate);
        }

        public static ICycleOutputEvaluator CreateDiscrete<TPoint, TPeriodValue, TValue>(
            IReadOnlyList<TPoint> timelinePoints,
            IReadOnlyList<TPeriodValue> periodValues,
            IReadOnlyList<CyclePeriodDefinition> periods)
            where TPoint : IDiscreteCyclePoint<TValue>
            where TPeriodValue : IPeriodCycleValue<TValue>
        {
            List<DiscreteCycleChange<TValue>> changes = new();
            for (int valueIndex = 0; valueIndex < periodValues.Count; valueIndex++)
            {
                TPeriodValue periodValue = periodValues[valueIndex];
                for (int periodIndex = 0; periodIndex < periods.Count; periodIndex++)
                {
                    CyclePeriodDefinition period = periods[periodIndex];
                    if (period.PeriodId.Equals(periodValue.PeriodId))
                    {
                        changes.Add(new DiscreteCycleChange<TValue>(
                            period.StartTimeSeconds,
                            0,
                            periodValue.Value));
                        break;
                    }
                }
            }

            for (int pointIndex = 0; pointIndex < timelinePoints.Count; pointIndex++)
            {
                TPoint point = timelinePoints[pointIndex];
                changes.Add(new DiscreteCycleChange<TValue>(point.TimeSeconds, 1, point.Value));
            }

            return new DiscreteCycleOutputEvaluator<TValue>(changes);
        }

        #endregion
    }
}
