using System;
using System.Collections.Generic;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Exposes evaluated output values to scene-specific applicators without exposing runtime storage.
    /// </summary>
    public interface IReadOnlyCycleOutputState
    {
        bool TryGetValue<T>(CycleOutputId outputId, out T value);
    }

    /// <summary>
    /// Applies evaluated outputs to one concrete scene or engine consumer.
    /// </summary>
    public interface ITimeOfCycleOutputApplicator
    {
        void Apply(IReadOnlyCycleOutputState outputState);
    }

    /// <summary>
    /// Stores the current evaluated value for every active output identifier.
    /// </summary>
    internal sealed class CycleOutputState : IReadOnlyCycleOutputState
    {
        private readonly Dictionary<CycleOutputId, object> _valueByOutputId = new();

        #region Public Methods

        public bool TryGetValue<T>(CycleOutputId outputId, out T value)
        {
            if (_valueByOutputId.TryGetValue(outputId, out object untypedValue)
                && untypedValue is T typedValue)
            {
                value = typedValue;
                return true;
            }

            value = default;
            return false;
        }

        #endregion

        #region Private Methods

        internal void ReplaceValues(IEnumerable<CycleOutputRuntimeEntry> entries)
        {
            _valueByOutputId.Clear();
            foreach (CycleOutputRuntimeEntry entry in entries)
            {
                _valueByOutputId[entry.Definition.OutputId] = entry.CurrentValue;
            }
        }

        #endregion
    }

    /// <summary>
    /// Tracks evaluation and an optional in-flight profile transition for one output.
    /// </summary>
    internal sealed class CycleOutputRuntimeEntry
    {
        public CycleOutputDefinition Definition { get; }
        public ICycleOutputEvaluator Evaluator { get; }
        public object CurrentValue { get; set; }
        public object TransitionStartValue { get; }
        public float TransitionElapsedSeconds { get; set; }
        public bool IsTransitioning { get; set; }

        public CycleOutputRuntimeEntry(
            CycleOutputDefinition definition,
            ICycleOutputEvaluator evaluator,
            object currentValue,
            object transitionStartValue,
            bool isTransitioning)
        {
            Definition = definition;
            Evaluator = evaluator;
            CurrentValue = currentValue;
            TransitionStartValue = transitionStartValue;
            TransitionElapsedSeconds = 0f;
            IsTransitioning = isTransitioning;
        }
    }

    /// <summary>
    /// Owns evaluated output values and independent transitions between effective profiles.
    /// </summary>
    internal sealed class CycleOutputRuntimeSet
    {
        private readonly Dictionary<CycleOutputId, CycleOutputRuntimeEntry> _entryByOutputId = new();
        private readonly CycleOutputState _outputState = new();

        public IReadOnlyCycleOutputState OutputState => _outputState;

        #region Public Methods

        public void Configure(
            ResolvedTimeOfCycleConfiguration configuration,
            double presentationTimeSeconds,
            bool isInitialConfiguration)
        {
            Dictionary<CycleOutputId, CycleOutputRuntimeEntry> replacementEntries = new();
            for (int definitionIndex = 0;
                 definitionIndex < configuration.OutputDefinitions.Count;
                 definitionIndex++)
            {
                CycleOutputDefinition definition = configuration.OutputDefinitions[definitionIndex];
                ICycleOutputEvaluator evaluator = definition.CreateEvaluator(
                    configuration.CycleDurationSeconds,
                    configuration.Periods);
                object destinationValue = evaluator.Evaluate(presentationTimeSeconds);

                _entryByOutputId.TryGetValue(
                    definition.OutputId,
                    out CycleOutputRuntimeEntry previousEntry);
                bool canTransition = !isInitialConfiguration
                                     && definition.ProfileChangeTransitionDurationSeconds > 0f
                                     && previousEntry != null
                                     && previousEntry.Evaluator.ValueType == evaluator.ValueType;
                object currentValue = canTransition ? previousEntry.CurrentValue : destinationValue;
                CycleOutputRuntimeEntry replacementEntry = new(
                    definition,
                    evaluator,
                    currentValue,
                    currentValue,
                    canTransition);
                replacementEntries.Add(definition.OutputId, replacementEntry);
            }

            _entryByOutputId.Clear();
            foreach ((CycleOutputId outputId, CycleOutputRuntimeEntry entry) in replacementEntries)
            {
                _entryByOutputId.Add(outputId, entry);
            }

            _outputState.ReplaceValues(_entryByOutputId.Values);
        }

        public void Update(double presentationTimeSeconds, float deltaTimeSeconds)
        {
            foreach (CycleOutputRuntimeEntry entry in _entryByOutputId.Values)
            {
                object destinationValue = entry.Evaluator.Evaluate(presentationTimeSeconds);
                if (!entry.IsTransitioning)
                {
                    entry.CurrentValue = destinationValue;
                    continue;
                }

                entry.TransitionElapsedSeconds += deltaTimeSeconds;
                float transitionDurationSeconds = entry.Definition.ProfileChangeTransitionDurationSeconds;
                float transitionProgress01 = transitionDurationSeconds <= 0f
                    ? 1f
                    : Math.Clamp(entry.TransitionElapsedSeconds / transitionDurationSeconds, 0f, 1f);
                entry.CurrentValue = entry.Definition.InterpolateProfileValue(
                    entry.TransitionStartValue,
                    destinationValue,
                    transitionProgress01);

                if (transitionProgress01 >= 1f)
                {
                    entry.IsTransitioning = false;
                }
            }

            _outputState.ReplaceValues(_entryByOutputId.Values);
        }

        #endregion
    }
}
