using System;
using System.Collections.Generic;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Evaluates the most recent discrete state change in a cyclic stream.
    /// </summary>
    internal sealed class DiscreteCycleOutputEvaluator<T> : ICycleOutputEvaluator
    {
        private readonly List<DiscreteCycleChange<T>> _changes;

        public Type ValueType => typeof(T);

        public DiscreteCycleOutputEvaluator(List<DiscreteCycleChange<T>> changes)
        {
            _changes = changes;
            _changes.Sort(CompareChanges);
        }

        #region Public Methods

        public object Evaluate(double presentationTimeSeconds)
        {
            DiscreteCycleChange<T> selectedChange = _changes[_changes.Count - 1];
            for (int changeIndex = 0; changeIndex < _changes.Count; changeIndex++)
            {
                DiscreteCycleChange<T> candidate = _changes[changeIndex];
                if (candidate.TimeSeconds > presentationTimeSeconds)
                {
                    break;
                }

                selectedChange = candidate;
            }

            return selectedChange.Value;
        }

        #endregion

        #region Private Methods

        private static int CompareChanges(
            DiscreteCycleChange<T> left,
            DiscreteCycleChange<T> right)
        {
            int timeComparison = left.TimeSeconds.CompareTo(right.TimeSeconds);
            return timeComparison != 0 ? timeComparison : left.Priority.CompareTo(right.Priority);
        }

        #endregion
    }
}
