using System.Collections.Generic;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Replaces selected profile configuration while leaving unspecified values inherited.
    /// </summary>
    [CreateAssetMenu(menuName = FakeMGEditorMenus.ROOT + "/Day Night Cycle/Time Of Cycle Override SO")]
    public sealed class TimeOfCycleOverrideSO : ScriptableObject
    {
        [Header("Cycle Overrides")]
        [SerializeField] private bool _doesOverrideCycleDuration;
        [SerializeField] private double _cycleDurationSeconds = 86400d;
        [SerializeField] private bool _doesOverrideDefaultStartingTime;
        [SerializeField] private double _defaultStartingTimeSeconds = 28800d;
        [SerializeField] private bool _doesOverrideDefaultAdvancementRate;
        [SerializeField] private double _defaultAdvancementRateCycleSecondsPerRealSecond = 60d;
        [SerializeField] private bool _doesOverrideInitialAutomaticAdvancementState;
        [SerializeField] private bool _doesAutomaticAdvancementBeginEnabled = true;
        [SerializeField] private bool _doesOverrideDefaultCommandTransition;
        [SerializeField] private float _defaultCommandTransitionDurationSeconds = 1f;
        [SerializeField]
        private AnimationCurve _defaultCommandTransitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Period Overrides")]
        [SerializeField] private bool _doesOverridePeriods;
        [SerializeField] private List<CyclePeriodDefinition> _periods = new();

        [Header("Output Replacements And Additions")]
        [SerializeReference] private List<CycleOutputDefinition> _outputDefinitions = new();

        public bool DoesOverrideCycleDuration => _doesOverrideCycleDuration;
        public double CycleDurationSeconds => _cycleDurationSeconds;
        public bool DoesOverrideDefaultStartingTime => _doesOverrideDefaultStartingTime;
        public double DefaultStartingTimeSeconds => _defaultStartingTimeSeconds;
        public bool DoesOverrideDefaultAdvancementRate => _doesOverrideDefaultAdvancementRate;
        public double DefaultAdvancementRateCycleSecondsPerRealSecond =>
            _defaultAdvancementRateCycleSecondsPerRealSecond;
        public bool DoesOverrideInitialAutomaticAdvancementState =>
            _doesOverrideInitialAutomaticAdvancementState;
        public bool DoesAutomaticAdvancementBeginEnabled => _doesAutomaticAdvancementBeginEnabled;
        public bool DoesOverrideDefaultCommandTransition => _doesOverrideDefaultCommandTransition;
        public float DefaultCommandTransitionDurationSeconds => _defaultCommandTransitionDurationSeconds;
        public AnimationCurve DefaultCommandTransitionCurve => _defaultCommandTransitionCurve;
        public bool DoesOverridePeriods => _doesOverridePeriods;
        public IReadOnlyList<CyclePeriodDefinition> Periods => _periods;
        public IReadOnlyList<CycleOutputDefinition> OutputDefinitions => _outputDefinitions;
    }
}
