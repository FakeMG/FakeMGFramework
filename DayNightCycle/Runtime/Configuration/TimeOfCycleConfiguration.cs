using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Defines the identifier and inclusive start time of one named cycle period.
    /// </summary>
    [Serializable]
    public sealed class CyclePeriodDefinition
    {
        [SerializeField] private string _periodId;
        [SerializeField] private double _startTimeSeconds;

        public CyclePeriodId PeriodId => new(_periodId);
        public double StartTimeSeconds => _startTimeSeconds;

        public CyclePeriodDefinition(string periodId, double startTimeSeconds)
        {
            _periodId = periodId;
            _startTimeSeconds = startTimeSeconds;
        }
    }

    /// <summary>
    /// Holds one validated and precedence-resolved configuration used by the runtime.
    /// </summary>
    internal sealed class ResolvedTimeOfCycleConfiguration
    {
        public double CycleDurationSeconds { get; }
        public double DefaultStartingTimeSeconds { get; }
        public double DefaultAdvancementRateCycleSecondsPerRealSecond { get; }
        public bool DoesAutomaticAdvancementBeginEnabled { get; }
        public float DefaultCommandTransitionDurationSeconds { get; }
        public AnimationCurve DefaultCommandTransitionCurve { get; }
        public IReadOnlyList<CyclePeriodDefinition> Periods { get; }
        public IReadOnlyList<CycleOutputDefinition> OutputDefinitions { get; }

        public ResolvedTimeOfCycleConfiguration(
            double cycleDurationSeconds,
            double defaultStartingTimeSeconds,
            double defaultAdvancementRateCycleSecondsPerRealSecond,
            bool doesAutomaticAdvancementBeginEnabled,
            float defaultCommandTransitionDurationSeconds,
            AnimationCurve defaultCommandTransitionCurve,
            IReadOnlyList<CyclePeriodDefinition> periods,
            IReadOnlyList<CycleOutputDefinition> outputDefinitions)
        {
            CycleDurationSeconds = cycleDurationSeconds;
            DefaultStartingTimeSeconds = defaultStartingTimeSeconds;
            DefaultAdvancementRateCycleSecondsPerRealSecond =
                defaultAdvancementRateCycleSecondsPerRealSecond;
            DoesAutomaticAdvancementBeginEnabled = doesAutomaticAdvancementBeginEnabled;
            DefaultCommandTransitionDurationSeconds = defaultCommandTransitionDurationSeconds;
            DefaultCommandTransitionCurve = defaultCommandTransitionCurve;
            Periods = periods;
            OutputDefinitions = outputDefinitions;
        }
    }

    /// <summary>
    /// Resolves profile layers and rejects invalid effective configurations atomically.
    /// </summary>
    internal static class TimeOfCycleConfigurationResolver
    {
        #region Public Methods

        public static bool TryResolve(
            TimeOfCycleProfileSO sharedProfileSO,
            TimeOfCycleOverrideSO contextOverrideSO,
            TimeOfCycleOverrideSO runtimeOverrideSO,
            out ResolvedTimeOfCycleConfiguration configuration,
            out string errorMessage)
        {
            configuration = null;

            if (sharedProfileSO == null)
            {
                errorMessage = "Time-of-cycle configuration requires a shared profile.";
                return false;
            }

            double cycleDurationSeconds = sharedProfileSO.CycleDurationSeconds;
            double defaultStartingTimeSeconds = sharedProfileSO.DefaultStartingTimeSeconds;
            double defaultAdvancementRateCycleSecondsPerRealSecond =
                sharedProfileSO.DefaultAdvancementRateCycleSecondsPerRealSecond;
            bool doesAutomaticAdvancementBeginEnabled =
                sharedProfileSO.DoesAutomaticAdvancementBeginEnabled;
            float defaultCommandTransitionDurationSeconds =
                sharedProfileSO.DefaultCommandTransitionDurationSeconds;
            AnimationCurve defaultCommandTransitionCurve = sharedProfileSO.DefaultCommandTransitionCurve;
            List<CyclePeriodDefinition> periods = new(sharedProfileSO.Periods);
            Dictionary<CycleOutputId, CycleOutputDefinition> outputById = new();

            if (!TryMergeOutputs(sharedProfileSO.OutputDefinitions, outputById, out errorMessage))
            {
                return false;
            }

            if (!TryApplyOverride(
                    contextOverrideSO,
                    ref cycleDurationSeconds,
                    ref defaultStartingTimeSeconds,
                    ref defaultAdvancementRateCycleSecondsPerRealSecond,
                    ref doesAutomaticAdvancementBeginEnabled,
                    ref defaultCommandTransitionDurationSeconds,
                    ref defaultCommandTransitionCurve,
                    ref periods,
                    outputById,
                    out errorMessage)
                || !TryApplyOverride(
                    runtimeOverrideSO,
                    ref cycleDurationSeconds,
                    ref defaultStartingTimeSeconds,
                    ref defaultAdvancementRateCycleSecondsPerRealSecond,
                    ref doesAutomaticAdvancementBeginEnabled,
                    ref defaultCommandTransitionDurationSeconds,
                    ref defaultCommandTransitionCurve,
                    ref periods,
                    outputById,
                    out errorMessage))
            {
                return false;
            }

            periods.Sort(ComparePeriodsByStartTime);
            List<CycleOutputDefinition> outputDefinitions = outputById.Values.ToList();
            if (!TryValidate(
                    cycleDurationSeconds,
                    defaultStartingTimeSeconds,
                    defaultAdvancementRateCycleSecondsPerRealSecond,
                    defaultCommandTransitionDurationSeconds,
                    defaultCommandTransitionCurve,
                    periods,
                    outputDefinitions,
                    out errorMessage))
            {
                return false;
            }

            configuration = new ResolvedTimeOfCycleConfiguration(
                cycleDurationSeconds,
                defaultStartingTimeSeconds,
                defaultAdvancementRateCycleSecondsPerRealSecond,
                doesAutomaticAdvancementBeginEnabled,
                defaultCommandTransitionDurationSeconds,
                defaultCommandTransitionCurve,
                periods,
                outputDefinitions);
            errorMessage = null;
            return true;
        }

        #endregion

        #region Private Methods

        private static bool TryApplyOverride(
            TimeOfCycleOverrideSO overrideSO,
            ref double cycleDurationSeconds,
            ref double defaultStartingTimeSeconds,
            ref double defaultAdvancementRateCycleSecondsPerRealSecond,
            ref bool doesAutomaticAdvancementBeginEnabled,
            ref float defaultCommandTransitionDurationSeconds,
            ref AnimationCurve defaultCommandTransitionCurve,
            ref List<CyclePeriodDefinition> periods,
            Dictionary<CycleOutputId, CycleOutputDefinition> outputById,
            out string errorMessage)
        {
            errorMessage = null;
            if (overrideSO == null)
            {
                return true;
            }

            if (overrideSO.DoesOverrideCycleDuration)
            {
                cycleDurationSeconds = overrideSO.CycleDurationSeconds;
            }

            if (overrideSO.DoesOverrideDefaultStartingTime)
            {
                defaultStartingTimeSeconds = overrideSO.DefaultStartingTimeSeconds;
            }

            if (overrideSO.DoesOverrideDefaultAdvancementRate)
            {
                defaultAdvancementRateCycleSecondsPerRealSecond =
                    overrideSO.DefaultAdvancementRateCycleSecondsPerRealSecond;
            }

            if (overrideSO.DoesOverrideInitialAutomaticAdvancementState)
            {
                doesAutomaticAdvancementBeginEnabled = overrideSO.DoesAutomaticAdvancementBeginEnabled;
            }

            if (overrideSO.DoesOverrideDefaultCommandTransition)
            {
                defaultCommandTransitionDurationSeconds = overrideSO.DefaultCommandTransitionDurationSeconds;
                defaultCommandTransitionCurve = overrideSO.DefaultCommandTransitionCurve;
            }

            if (overrideSO.DoesOverridePeriods)
            {
                periods = new List<CyclePeriodDefinition>(overrideSO.Periods);
            }

            return TryMergeOutputs(overrideSO.OutputDefinitions, outputById, out errorMessage);
        }

        private static bool TryMergeOutputs(
            IReadOnlyList<CycleOutputDefinition> definitions,
            Dictionary<CycleOutputId, CycleOutputDefinition> outputById,
            out string errorMessage)
        {
            HashSet<CycleOutputId> layerOutputIds = new();
            for (int outputIndex = 0; outputIndex < definitions.Count; outputIndex++)
            {
                CycleOutputDefinition definition = definitions[outputIndex];
                if (definition == null)
                {
                    errorMessage = $"Output definition at index {outputIndex} is null.";
                    return false;
                }

                CycleOutputId outputId = definition.OutputId;
                if (!outputId.IsValid)
                {
                    errorMessage = $"Output definition at index {outputIndex} has an empty identifier.";
                    return false;
                }

                if (!layerOutputIds.Add(outputId))
                {
                    errorMessage = $"Output identifier '{outputId}' occurs more than once in one profile layer.";
                    return false;
                }

                if (outputById.TryGetValue(outputId, out CycleOutputDefinition inheritedDefinition)
                    && inheritedDefinition.ValueType != definition.ValueType)
                {
                    errorMessage =
                        $"Output '{outputId}' changes type from {inheritedDefinition.ValueType.Name} " +
                        $"to {definition.ValueType.Name} across profile layers.";
                    return false;
                }

                outputById[outputId] = definition;
            }

            errorMessage = null;
            return true;
        }

        private static bool TryValidate(
            double cycleDurationSeconds,
            double defaultStartingTimeSeconds,
            double defaultAdvancementRateCycleSecondsPerRealSecond,
            float defaultCommandTransitionDurationSeconds,
            AnimationCurve defaultCommandTransitionCurve,
            IReadOnlyList<CyclePeriodDefinition> periods,
            IReadOnlyList<CycleOutputDefinition> outputDefinitions,
            out string errorMessage)
        {
            if (cycleDurationSeconds <= 0d)
            {
                errorMessage = "Cycle duration must be greater than zero seconds.";
                return false;
            }

            if (defaultStartingTimeSeconds < 0d || defaultStartingTimeSeconds >= cycleDurationSeconds)
            {
                errorMessage = "Default starting time must be inside the cycle.";
                return false;
            }

            if (defaultAdvancementRateCycleSecondsPerRealSecond < 0d)
            {
                errorMessage = "Default advancement rate cannot be negative.";
                return false;
            }

            if (defaultCommandTransitionDurationSeconds < 0f || defaultCommandTransitionCurve == null)
            {
                errorMessage = "Default command transition requires a non-negative duration and an easing curve.";
                return false;
            }

            if (periods.Count == 0)
            {
                errorMessage = "At least one named period is required.";
                return false;
            }

            HashSet<CyclePeriodId> periodIds = new();
            HashSet<double> periodStartTimesSeconds = new();
            for (int periodIndex = 0; periodIndex < periods.Count; periodIndex++)
            {
                CyclePeriodDefinition period = periods[periodIndex];
                if (period == null || !period.PeriodId.IsValid)
                {
                    errorMessage = $"Period at index {periodIndex} is null or has an empty identifier.";
                    return false;
                }

                if (!periodIds.Add(period.PeriodId))
                {
                    errorMessage = $"Period identifier '{period.PeriodId}' is duplicated.";
                    return false;
                }

                if (period.StartTimeSeconds < 0d || period.StartTimeSeconds >= cycleDurationSeconds)
                {
                    errorMessage = $"Period '{period.PeriodId}' starts outside the cycle.";
                    return false;
                }

                if (!periodStartTimesSeconds.Add(period.StartTimeSeconds))
                {
                    errorMessage = $"More than one period starts at {period.StartTimeSeconds} seconds.";
                    return false;
                }
            }

            for (int outputIndex = 0; outputIndex < outputDefinitions.Count; outputIndex++)
            {
                CycleOutputDefinition definition = outputDefinitions[outputIndex];
                if (!definition.TryValidate(cycleDurationSeconds, periodIds, out errorMessage))
                {
                    errorMessage = $"Output '{definition.OutputId}' is invalid: {errorMessage}";
                    return false;
                }
            }

            errorMessage = null;
            return true;
        }

        private static int ComparePeriodsByStartTime(CyclePeriodDefinition left, CyclePeriodDefinition right)
        {
            return left.StartTimeSeconds.CompareTo(right.StartTimeSeconds);
        }

        #endregion
    }
}
