using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Identifies one named period within a repeating cycle.
    /// </summary>
    [Serializable]
    public readonly struct CyclePeriodId : IEquatable<CyclePeriodId>
    {
        [SerializeField] private readonly string _value;

        public string Value => _value;
        public bool IsValid => !string.IsNullOrWhiteSpace(_value);

        public CyclePeriodId(string value)
        {
            _value = value;
        }

        public bool Equals(CyclePeriodId other)
        {
            return string.Equals(_value, other._value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is CyclePeriodId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value == null ? 0 : StringComparer.Ordinal.GetHashCode(_value);
        }

        public override string ToString()
        {
            return _value ?? string.Empty;
        }
    }

    /// <summary>
    /// Identifies an evaluated output independently from any scene consumer.
    /// </summary>
    [Serializable]
    public readonly struct CycleOutputId : IEquatable<CycleOutputId>
    {
        [SerializeField] private readonly string _value;

        public string Value => _value;
        public bool IsValid => !string.IsNullOrWhiteSpace(_value);

        public CycleOutputId(string value)
        {
            _value = value;
        }

        public bool Equals(CycleOutputId other)
        {
            return string.Equals(_value, other._value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is CycleOutputId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value == null ? 0 : StringComparer.Ordinal.GetHashCode(_value);
        }

        public override string ToString()
        {
            return _value ?? string.Empty;
        }
    }

    /// <summary>
    /// Describes the observable state of the repeating cycle at one frame.
    /// </summary>
    public readonly struct TimeOfCycleState
    {
        public double AuthoritativeTimeSeconds { get; }
        public double PresentationTimeSeconds { get; }
        public double NormalizedCycleProgress01 { get; }
        public CyclePeriodId CurrentPeriodId { get; }
        public bool IsAutomaticAdvancementActive { get; }
        public double AdvancementRateCycleSecondsPerRealSecond { get; }

        public TimeOfCycleState(
            double authoritativeTimeSeconds,
            double presentationTimeSeconds,
            double normalizedCycleProgress01,
            CyclePeriodId currentPeriodId,
            bool isAutomaticAdvancementActive,
            double advancementRateCycleSecondsPerRealSecond)
        {
            AuthoritativeTimeSeconds = authoritativeTimeSeconds;
            PresentationTimeSeconds = presentationTimeSeconds;
            NormalizedCycleProgress01 = normalizedCycleProgress01;
            CurrentPeriodId = currentPeriodId;
            IsAutomaticAdvancementActive = isAutomaticAdvancementActive;
            AdvancementRateCycleSecondsPerRealSecond = advancementRateCycleSecondsPerRealSecond;
        }
    }

    /// <summary>
    /// Reports the previous and current named periods after gameplay time changes.
    /// </summary>
    public readonly struct CyclePeriodChange
    {
        public CyclePeriodId PreviousPeriodId { get; }
        public CyclePeriodId CurrentPeriodId { get; }

        public CyclePeriodChange(CyclePeriodId previousPeriodId, CyclePeriodId currentPeriodId)
        {
            PreviousPeriodId = previousPeriodId;
            CurrentPeriodId = currentPeriodId;
        }
    }

    /// <summary>
    /// Selects how a time command moves through the repeating cycle.
    /// </summary>
    public enum TimeCommandMode
    {
        Immediate,
        SimulatedAdvance,
        SmoothPresentationJump,
    }

    /// <summary>
    /// Selects which cyclic path a transition follows toward its destination.
    /// </summary>
    public enum TimeMovementDirection
    {
        Forward = 1,
        Backward = -1,
    }

    /// <summary>
    /// Reports how a requested time command ended.
    /// </summary>
    public enum TimeCommandStatus
    {
        Completed,
        Replaced,
        Cancelled,
        Rejected,
    }

    /// <summary>
    /// Carries the final status of one time command request.
    /// </summary>
    public readonly struct TimeCommandResult
    {
        public long CommandId { get; }
        public TimeCommandStatus Status { get; }

        public TimeCommandResult(long commandId, TimeCommandStatus status)
        {
            CommandId = commandId;
            Status = status;
        }
    }

    /// <summary>
    /// Configures real-time duration and easing for a time command.
    /// </summary>
    [Serializable]
    public readonly struct TimeCommandTransition
    {
        public float DurationSeconds { get; }
        public AnimationCurve EasingCurve { get; }
        public bool UsesProfileDefault => DurationSeconds < 0f;

        public TimeCommandTransition(float durationSeconds, AnimationCurve easingCurve = null)
        {
            DurationSeconds = durationSeconds;
            EasingCurve = easingCurve;
        }

        public static TimeCommandTransition ProfileDefault()
        {
            return new TimeCommandTransition(-1f);
        }
    }

    /// <summary>
    /// Represents a one-time request to change authoritative or presentation time.
    /// </summary>
    public readonly struct TimeCommand
    {
        public TimeCommandMode Mode { get; }
        public TimeMovementDirection Direction { get; }
        public double TargetTimeSeconds { get; }
        public TimeCommandTransition Transition { get; }

        private TimeCommand(
            TimeCommandMode mode,
            TimeMovementDirection direction,
            double targetTimeSeconds,
            TimeCommandTransition transition)
        {
            Mode = mode;
            Direction = direction;
            TargetTimeSeconds = targetTimeSeconds;
            Transition = transition;
        }

        #region Public Methods

        public static TimeCommand Immediate(double targetTimeSeconds)
        {
            return new TimeCommand(
                TimeCommandMode.Immediate,
                TimeMovementDirection.Forward,
                targetTimeSeconds,
                new TimeCommandTransition(0f));
        }

        public static TimeCommand SimulatedAdvance(
            double targetTimeSeconds,
            TimeMovementDirection direction)
        {
            return new TimeCommand(
                TimeCommandMode.SimulatedAdvance,
                direction,
                targetTimeSeconds,
                TimeCommandTransition.ProfileDefault());
        }

        public static TimeCommand SimulatedAdvance(
            double targetTimeSeconds,
            TimeMovementDirection direction,
            TimeCommandTransition transition)
        {
            return new TimeCommand(TimeCommandMode.SimulatedAdvance, direction, targetTimeSeconds, transition);
        }

        public static TimeCommand SmoothPresentationJump(
            double targetTimeSeconds,
            TimeMovementDirection direction)
        {
            return new TimeCommand(
                TimeCommandMode.SmoothPresentationJump,
                direction,
                targetTimeSeconds,
                TimeCommandTransition.ProfileDefault());
        }

        public static TimeCommand SmoothPresentationJump(
            double targetTimeSeconds,
            TimeMovementDirection direction,
            TimeCommandTransition transition)
        {
            return new TimeCommand(TimeCommandMode.SmoothPresentationJump, direction, targetTimeSeconds, transition);
        }

        #endregion
    }

    /// <summary>
    /// Selects the single behavior owned by a persistent external controller.
    /// </summary>
    public enum TimeControlBehavior
    {
        PauseAutomaticAdvancement,
        OverrideAdvancementRate,
        ProvideAuthoritativeTime,
    }

    /// <summary>
    /// Supplies authoritative time while an external direct-time request is active.
    /// </summary>
    public interface ITimeOfCycleTimeProvider
    {
        bool IsValid { get; }
        bool TryGetCurrentTimeSeconds(out double currentTimeSeconds);
    }

    /// <summary>
    /// Defines one persistent request participating in priority arbitration.
    /// </summary>
    public sealed class TimeControlRequest
    {
        public TimeControlBehavior Behavior { get; }
        public int Priority { get; }
        public bool AllowsTimeCommands { get; }
        public double AdvancementRateCycleSecondsPerRealSecond { get; }
        public ITimeOfCycleTimeProvider TimeProvider { get; }

        private TimeControlRequest(
            TimeControlBehavior behavior,
            int priority,
            bool allowsTimeCommands,
            double advancementRateCycleSecondsPerRealSecond,
            ITimeOfCycleTimeProvider timeProvider)
        {
            Behavior = behavior;
            Priority = priority;
            AllowsTimeCommands = allowsTimeCommands;
            AdvancementRateCycleSecondsPerRealSecond = advancementRateCycleSecondsPerRealSecond;
            TimeProvider = timeProvider;
        }

        #region Public Methods

        public static TimeControlRequest Pause(int priority, bool allowsTimeCommands)
        {
            return new TimeControlRequest(
                TimeControlBehavior.PauseAutomaticAdvancement,
                priority,
                allowsTimeCommands,
                0d,
                null);
        }

        public static TimeControlRequest OverrideAdvancementRate(
            double advancementRateCycleSecondsPerRealSecond,
            int priority,
            bool allowsTimeCommands)
        {
            return new TimeControlRequest(
                TimeControlBehavior.OverrideAdvancementRate,
                priority,
                allowsTimeCommands,
                advancementRateCycleSecondsPerRealSecond,
                null);
        }

        public static TimeControlRequest ProvideAuthoritativeTime(
            ITimeOfCycleTimeProvider timeProvider,
            int priority,
            bool allowsTimeCommands)
        {
            return new TimeControlRequest(
                TimeControlBehavior.ProvideAuthoritativeTime,
                priority,
                allowsTimeCommands,
                0d,
                timeProvider);
        }

        #endregion
    }

    /// <summary>
    /// Defines the complete public control surface of a time-of-cycle runtime.
    /// </summary>
    public interface ITimeOfCycle
    {
        TimeOfCycleState CurrentState { get; }

        event Action<CyclePeriodChange> OnPeriodChanged;
        event Action OnCycleCompleted;
        event Action<TimeCommandResult> OnTimeCommandCompleted;

        void SetAutomaticAdvancementEnabled(bool isEnabled);
        bool TrySetAdvancementRate(double advancementRateCycleSecondsPerRealSecond);
        UniTask<TimeCommandResult> ExecuteTimeCommandAsync(
            TimeCommand command,
            CancellationToken cancellationToken = default);
        IDisposable RegisterControl(TimeControlRequest request);
        bool SetContextOverride(TimeOfCycleOverrideSO contextOverrideSO);
        bool SetRuntimeOverride(TimeOfCycleOverrideSO runtimeOverrideSO);
    }
}
