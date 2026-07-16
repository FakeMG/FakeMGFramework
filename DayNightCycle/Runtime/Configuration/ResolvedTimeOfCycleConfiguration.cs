using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
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
}
