using System;
using System.Collections.Generic;
using FakeMG.Framework;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Owns evaluated output values and independent transitions between effective profiles.
    /// </summary>
    internal sealed class CycleOutputRuntimeSet
    {
        private readonly Dictionary<CycleOutputKeySO, CycleOutputRuntimeEntry> _entryByOutputKey = new();
        private readonly CycleOutputState _outputState = new();

        public IReadOnlyCycleOutputState OutputState => _outputState;

        #region Public Methods

        public void Configure(
            ResolvedTimeOfCycleConfiguration configuration,
            double presentationTimeSeconds,
            bool isInitialConfiguration)
        {
            Dictionary<CycleOutputKeySO, CycleOutputRuntimeEntry> replacementEntries = new();
            for (int definitionIndex = 0;
                 definitionIndex < configuration.OutputDefinitions.Count;
                 definitionIndex++)
            {
                CycleOutputDefinition definition = configuration.OutputDefinitions[definitionIndex];
                ICycleOutputEvaluator evaluator = definition.CreateEvaluator(
                    configuration.CycleDurationSeconds,
                    configuration.Periods);
                object destinationValue = evaluator.Evaluate(presentationTimeSeconds);
                if (!CycleOutputValueValidation.IsValidRuntimeValue(
                        destinationValue,
                        definition.OutputKeySO.ValueType))
                {
                    Echo.Error(
                        $"Output evaluator for '{definition.OutputKeySO.name}' produced an invalid " +
                        $"{definition.OutputKeySO.ValueType.Name} value during configuration.");
                    destinationValue = CycleOutputValueValidation.GetDefaultValue(
                        definition.OutputKeySO.ValueType);
                }

                _entryByOutputKey.TryGetValue(
                    definition.OutputKeySO,
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
                replacementEntries.Add(definition.OutputKeySO, replacementEntry);
            }

            _entryByOutputKey.Clear();
            foreach ((CycleOutputKeySO outputKeySO, CycleOutputRuntimeEntry entry) in replacementEntries)
            {
                _entryByOutputKey.Add(outputKeySO, entry);
            }

            _outputState.ReplaceValues(_entryByOutputKey.Values);
        }

        public void Update(double presentationTimeSeconds, float deltaTimeSeconds)
        {
            foreach (CycleOutputRuntimeEntry entry in _entryByOutputKey.Values)
            {
                object destinationValue = entry.Evaluator.Evaluate(presentationTimeSeconds);
                if (!CycleOutputValueValidation.IsValidRuntimeValue(
                        destinationValue,
                        entry.Definition.OutputKeySO.ValueType))
                {
                    LogInvalidValueOnce(entry);
                    continue;
                }
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
                if (!CycleOutputValueValidation.IsValidRuntimeValue(
                        entry.CurrentValue,
                        entry.Definition.OutputKeySO.ValueType))
                {
                    entry.CurrentValue = entry.TransitionStartValue;
                    LogInvalidValueOnce(entry);
                    entry.IsTransitioning = false;
                    continue;
                }

                if (transitionProgress01 >= 1f)
                {
                    entry.IsTransitioning = false;
                }
            }

            _outputState.ReplaceValues(_entryByOutputKey.Values);
        }

        #endregion

        #region Private Methods

        private static void LogInvalidValueOnce(CycleOutputRuntimeEntry entry)
        {
            if (entry.HasLoggedInvalidValue)
            {
                return;
            }

            Echo.Error(
                $"Output evaluator for '{entry.Definition.OutputKeySO.name}' produced an invalid " +
                $"{entry.Definition.OutputKeySO.ValueType.Name} value. The previous value was retained.");
            entry.HasLoggedInvalidValue = true;
        }

        #endregion
    }
}
