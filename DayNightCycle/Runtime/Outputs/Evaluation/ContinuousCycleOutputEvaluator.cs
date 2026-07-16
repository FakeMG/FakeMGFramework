using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Evaluates sorted continuous points including the cyclic final-to-first segment.
    /// </summary>
    internal sealed class ContinuousCycleOutputEvaluator<T> : ICycleOutputEvaluator
    {
        private readonly double _cycleDurationSeconds;
        private readonly List<ContinuousCycleValue<T>> _values;
        private readonly AnimationCurve _interpolationCurve;
        private readonly Func<T, T, float, T> _interpolate;

        public Type ValueType => typeof(T);

        public ContinuousCycleOutputEvaluator(
            double cycleDurationSeconds,
            List<ContinuousCycleValue<T>> values,
            AnimationCurve interpolationCurve,
            Func<T, T, float, T> interpolate)
        {
            _cycleDurationSeconds = cycleDurationSeconds;
            _values = values;
            _values.Sort(CompareValuesByTime);
            _interpolationCurve = interpolationCurve;
            _interpolate = interpolate;
        }

        #region Public Methods

        public object Evaluate(double presentationTimeSeconds)
        {
            if (_values.Count == 1)
            {
                return _values[0].Value;
            }

            int destinationIndex = FindDestinationIndex(presentationTimeSeconds);
            int previousIndex = destinationIndex == 0 ? _values.Count - 1 : destinationIndex - 1;
            ContinuousCycleValue<T> previous = _values[previousIndex];
            ContinuousCycleValue<T> destination = _values[destinationIndex];
            double previousTimeSeconds = previous.TimeSeconds;
            double destinationTimeSeconds = destination.TimeSeconds;
            double evaluatedTimeSeconds = presentationTimeSeconds;
            if (destinationIndex == 0)
            {
                destinationTimeSeconds += _cycleDurationSeconds;
                if (evaluatedTimeSeconds < previousTimeSeconds)
                {
                    evaluatedTimeSeconds += _cycleDurationSeconds;
                }
            }

            double segmentDurationSeconds = destinationTimeSeconds - previousTimeSeconds;
            float segmentProgress01 = segmentDurationSeconds <= 0d
                ? 1f
                : (float)((evaluatedTimeSeconds - previousTimeSeconds) / segmentDurationSeconds);
            float easedProgress01 = _interpolationCurve.Evaluate(segmentProgress01);
            return _interpolate(previous.Value, destination.Value, easedProgress01);
        }

        #endregion

        #region Private Methods

        private int FindDestinationIndex(double presentationTimeSeconds)
        {
            for (int valueIndex = 0; valueIndex < _values.Count; valueIndex++)
            {
                if (_values[valueIndex].TimeSeconds > presentationTimeSeconds)
                {
                    return valueIndex;
                }
            }

            return 0;
        }

        private static int CompareValuesByTime(
            ContinuousCycleValue<T> left,
            ContinuousCycleValue<T> right)
        {
            return left.TimeSeconds.CompareTo(right.TimeSeconds);
        }

        #endregion
    }
}
