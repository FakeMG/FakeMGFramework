using System.Collections.Generic;
using System.Linq;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.TimeCycle
{
    /// <summary>
    /// Stores reusable defaults, named periods, and time-driven output definitions.
    /// </summary>
    [CreateAssetMenu(menuName = FakeMGEditorMenus.ROOT + "/Time Cycle/Time Of Cycle Profile SO")]
    public sealed class TimeOfCycleProfileSO : ScriptableObject
    {
        private const double DEFAULT_CYCLE_DURATION_SECONDS = 86400d;
        private const double DEFAULT_STARTING_PROGRESS_01 = 1d / 3d;
        private const double DEFAULT_ADVANCEMENT_RATE_CYCLE_SECONDS_PER_REAL_SECOND = 60d;
        private const float DEFAULT_COMMAND_TRANSITION_DURATION_SECONDS = 1f;

        [Header("Cycle")]
        [SerializeField] private double _cycleDurationSeconds = DEFAULT_CYCLE_DURATION_SECONDS;
        [SerializeField, CycleProgress] private double _defaultStartingProgress01 = DEFAULT_STARTING_PROGRESS_01;
        [SerializeField]
        private double _defaultAdvancementRateCycleSecondsPerRealSecond = DEFAULT_ADVANCEMENT_RATE_CYCLE_SECONDS_PER_REAL_SECOND;
        [SerializeField] private bool _doesAutomaticAdvancementBeginEnabled = true;

        [Header("Default Command Transition")]
        [SerializeField] private float _defaultCommandTransitionDurationSeconds = DEFAULT_COMMAND_TRANSITION_DURATION_SECONDS;
        [SerializeField]
        private AnimationCurve _defaultCommandTransitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Periods")]
        [SerializeField] private List<CyclePeriodDefinition> _periods = new();

        [Header("Outputs")]
        [SerializeReference] private List<CycleOutputDefinition> _outputDefinitions = new();

        public double CycleDurationSeconds => _cycleDurationSeconds;
        public double DefaultStartingProgress01 => _defaultStartingProgress01;
        public double DefaultAdvancementRateCycleSecondsPerRealSecond => _defaultAdvancementRateCycleSecondsPerRealSecond;
        public bool DoesAutomaticAdvancementBeginEnabled => _doesAutomaticAdvancementBeginEnabled;
        public float DefaultCommandTransitionDurationSeconds => _defaultCommandTransitionDurationSeconds;
        public AnimationCurve DefaultCommandTransitionCurve => _defaultCommandTransitionCurve;
        public IReadOnlyList<CyclePeriodDefinition> Periods => _periods;
        public IReadOnlyList<CycleOutputDefinition> OutputDefinitions => _outputDefinitions;

#if UNITY_EDITOR
        #region Public Methods

        public void ConfigureForEditor(
            double cycleDurationSeconds,
            double defaultStartingProgress01,
            double defaultAdvancementRateCycleSecondsPerRealSecond,
            bool doesAutomaticAdvancementBeginEnabled,
            float defaultCommandTransitionDurationSeconds,
            AnimationCurve defaultCommandTransitionCurve,
            IEnumerable<CyclePeriodDefinition> periods,
            IEnumerable<CycleOutputDefinition> outputDefinitions)
        {
            _cycleDurationSeconds = cycleDurationSeconds;
            _defaultStartingProgress01 = defaultStartingProgress01;
            _defaultAdvancementRateCycleSecondsPerRealSecond = defaultAdvancementRateCycleSecondsPerRealSecond;
            _doesAutomaticAdvancementBeginEnabled = doesAutomaticAdvancementBeginEnabled;
            _defaultCommandTransitionDurationSeconds = defaultCommandTransitionDurationSeconds;
            _defaultCommandTransitionCurve = defaultCommandTransitionCurve;
            _periods = periods.ToList();
            _outputDefinitions = outputDefinitions.ToList();
        }

        #endregion
#endif
    }
}
