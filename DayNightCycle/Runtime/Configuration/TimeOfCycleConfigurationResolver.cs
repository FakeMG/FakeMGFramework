using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
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
            Dictionary<CycleOutputKeySO, CycleOutputDefinition> outputByKey = new();

            if (!TryMergeOutputs(sharedProfileSO.OutputDefinitions, outputByKey, out errorMessage))
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
                    outputByKey,
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
                    outputByKey,
                    out errorMessage))
            {
                return false;
            }

            periods.Sort(ComparePeriodsByStartTime);
            List<CycleOutputDefinition> outputDefinitions = outputByKey.Values.ToList();
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
            Dictionary<CycleOutputKeySO, CycleOutputDefinition> outputByKey,
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

            return TryMergeOutputs(overrideSO.OutputDefinitions, outputByKey, out errorMessage);
        }

        private static bool TryMergeOutputs(
            IReadOnlyList<CycleOutputDefinition> definitions,
            Dictionary<CycleOutputKeySO, CycleOutputDefinition> outputByKey,
            out string errorMessage)
        {
            HashSet<CycleOutputKeySO> layerOutputKeys = new();
            for (int outputIndex = 0; outputIndex < definitions.Count; outputIndex++)
            {
                CycleOutputDefinition definition = definitions[outputIndex];
                if (definition == null)
                {
                    errorMessage = $"Output definition at index {outputIndex} is null.";
                    return false;
                }

                CycleOutputKeySO outputKeySO = definition.OutputKeySO;
                if (outputKeySO == null)
                {
                    errorMessage = $"Output definition at index {outputIndex} has no output key asset.";
                    return false;
                }

                if (!layerOutputKeys.Add(outputKeySO))
                {
                    errorMessage = $"Output key '{outputKeySO.name}' occurs more than once in one profile layer.";
                    return false;
                }

                if (outputByKey.TryGetValue(outputKeySO, out CycleOutputDefinition inheritedDefinition)
                    && inheritedDefinition.ValueType != definition.ValueType)
                {
                    errorMessage =
                        $"Output '{outputKeySO.name}' changes type from {inheritedDefinition.ValueType.Name} " +
                        $"to {definition.ValueType.Name} across profile layers.";
                    return false;
                }

                outputByKey[outputKeySO] = definition;
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
            if (!CycleNumericValidation.IsFinite(cycleDurationSeconds) || cycleDurationSeconds <= 0d)
            {
                errorMessage = "Cycle duration must be greater than zero seconds.";
                return false;
            }

            if (!CycleNumericValidation.IsFinite(defaultStartingTimeSeconds)
                || defaultStartingTimeSeconds < 0d
                || defaultStartingTimeSeconds >= cycleDurationSeconds)
            {
                errorMessage = "Default starting time must be inside the cycle.";
                return false;
            }

            if (!CycleNumericValidation.IsFiniteNonNegative(defaultAdvancementRateCycleSecondsPerRealSecond))
            {
                errorMessage = "Default advancement rate cannot be negative.";
                return false;
            }

            if (!CycleNumericValidation.IsFiniteNonNegative(defaultCommandTransitionDurationSeconds))
            {
                errorMessage = "Default command transition duration must be finite and non-negative.";
                return false;
            }

            if (!CycleCurveValidation.TryValidate(defaultCommandTransitionCurve, out errorMessage))
            {
                errorMessage = $"Default command transition curve is invalid. {errorMessage}";
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

                if (!CycleNumericValidation.IsFinite(period.StartTimeSeconds)
                    || period.StartTimeSeconds < 0d
                    || period.StartTimeSeconds >= cycleDurationSeconds)
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
                    string outputName = definition.OutputKeySO == null ? "<missing>" : definition.OutputKeySO.name;
                    errorMessage = $"Output '{outputName}' is invalid: {errorMessage}";
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
