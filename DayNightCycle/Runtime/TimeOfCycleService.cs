using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using UnityEngine;
using VContainer.Unity;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Owns authoritative and presentation time, command execution, control arbitration, and output evaluation.
    /// </summary>
    public sealed class TimeOfCycleService :
        ITimeOfCycle,
        IInitializable,
        ITickable,
        IDisposable
    {
        private const double TIME_EPSILON_SECONDS = 0.000001d;

        private readonly TimeOfCycleProfileSO _sharedProfileSO;
        private readonly IReadOnlyList<ITimeOfCycleOutputApplicator> _outputApplicators;
        private readonly CycleOutputRuntimeSet _outputRuntimeSet = new();
        private readonly List<TimeControlRegistration> _controlRegistrations = new();

        private ResolvedTimeOfCycleConfiguration _configuration;
        private TimeOfCycleOverrideSO _contextOverrideSO;
        private TimeOfCycleOverrideSO _runtimeOverrideSO;
        private ActiveTimeCommand _activeCommand;
        private TimeControlRegistration _activeControlRegistration;
        private CyclePeriodId _currentPeriodId;
        private double _authoritativeTimeSeconds;
        private double _presentationTimeSeconds;
        private double _advancementRateCycleSecondsPerRealSecond;
        private bool _isAutomaticAdvancementEnabled;
        private bool _isInitialized;
        private long _nextCommandId;
        private long _nextControlSequence;

        public TimeOfCycleState CurrentState { get; private set; }

        public event Action<CyclePeriodChange> OnPeriodChanged;
        public event Action OnCycleCompleted;
        public event Action<TimeCommandResult> OnTimeCommandCompleted;

        public TimeOfCycleService(
            TimeOfCycleProfileSO sharedProfileSO,
            IEnumerable<ITimeOfCycleOutputApplicator> outputApplicators)
        {
            _sharedProfileSO = sharedProfileSO;
            _outputApplicators = outputApplicators.ToArray();
        }

        #region Public Methods

        public void Initialize()
        {
            if (!TimeOfCycleConfigurationResolver.TryResolve(
                    _sharedProfileSO,
                    null,
                    null,
                    out _configuration,
                    out string errorMessage))
            {
                Echo.Error($"Time-of-cycle initialization failed. {errorMessage}");
                return;
            }

            _authoritativeTimeSeconds = _configuration.DefaultStartingTimeSeconds;
            _presentationTimeSeconds = _authoritativeTimeSeconds;
            _advancementRateCycleSecondsPerRealSecond =
                _configuration.DefaultAdvancementRateCycleSecondsPerRealSecond;
            _isAutomaticAdvancementEnabled = _configuration.DoesAutomaticAdvancementBeginEnabled;
            _currentPeriodId = ResolvePeriodId(_authoritativeTimeSeconds);
            _outputRuntimeSet.Configure(_configuration, _presentationTimeSeconds, true);
            ApplyOutputs(0f);
            _isInitialized = true;
            UpdatePublicState();
        }

        public void Tick()
        {
            if (!_isInitialized)
            {
                return;
            }

            float deltaTimeSeconds = Time.deltaTime;
            ArbitrateControl();

            if (_activeCommand != null)
            {
                if (_activeCommand.CancellationToken.IsCancellationRequested)
                {
                    CompleteActiveCommand(TimeCommandStatus.Cancelled);
                }
                else
                {
                    TickActiveCommand(deltaTimeSeconds);
                }
            }

            if (_activeCommand == null)
            {
                TickPersistentControlOrAutomaticAdvancement(deltaTimeSeconds);
            }

            ApplyOutputs(deltaTimeSeconds);
            UpdatePublicState();
        }

        public void Dispose()
        {
            CompleteActiveCommand(TimeCommandStatus.Cancelled);
            _controlRegistrations.Clear();
            _activeControlRegistration = null;
        }

        public void SetAutomaticAdvancementEnabled(bool isEnabled)
        {
            _isAutomaticAdvancementEnabled = isEnabled;
            UpdatePublicState();
        }

        public bool TrySetAdvancementRate(double advancementRateCycleSecondsPerRealSecond)
        {
            if (!IsFiniteNonNegative(advancementRateCycleSecondsPerRealSecond))
            {
                Echo.Warning(
                    "Time-of-cycle advancement rate must be a finite, non-negative number. " +
                    $"Received {advancementRateCycleSecondsPerRealSecond}.");
                return false;
            }

            _advancementRateCycleSecondsPerRealSecond = advancementRateCycleSecondsPerRealSecond;
            UpdatePublicState();
            return true;
        }

        public UniTask<TimeCommandResult> ExecuteTimeCommandAsync(
            TimeCommand command,
            CancellationToken cancellationToken = default)
        {
            long commandId = ++_nextCommandId;
            if (!_isInitialized)
            {
                Echo.Warning($"Time command {commandId} was rejected because the service is not initialized.");
                return CompleteImmediateCommand(commandId, TimeCommandStatus.Rejected);
            }

            ArbitrateControl();
            if (cancellationToken.IsCancellationRequested)
            {
                return CompleteImmediateCommand(commandId, TimeCommandStatus.Cancelled);
            }

            if (_activeControlRegistration != null
                && !_activeControlRegistration.Request.AllowsTimeCommands)
            {
                Echo.Warning(
                    $"Time command {commandId} was rejected because the active controller blocks commands.");
                return CompleteImmediateCommand(commandId, TimeCommandStatus.Rejected);
            }

            if (!IsFinite(command.TargetTimeSeconds)
                || !Enum.IsDefined(typeof(TimeCommandMode), command.Mode)
                || !Enum.IsDefined(typeof(TimeMovementDirection), command.Direction))
            {
                Echo.Warning($"Time command {commandId} contains an invalid target, mode, or direction.");
                return CompleteImmediateCommand(commandId, TimeCommandStatus.Rejected);
            }

            CompleteActiveCommand(TimeCommandStatus.Replaced);
            double targetTimeSeconds = NormalizeTime(command.TargetTimeSeconds);
            if (command.Mode == TimeCommandMode.Immediate)
            {
                SetDestinationOnly(targetTimeSeconds, true);
                ApplyOutputs(0f);
                UpdatePublicState();
                return CompleteImmediateCommand(commandId, TimeCommandStatus.Completed);
            }

            ResolveCommandTransition(
                command.Transition,
                out float transitionDurationSeconds,
                out AnimationCurve transitionCurve);
            double startTimeSeconds = command.Mode == TimeCommandMode.SimulatedAdvance
                ? _authoritativeTimeSeconds
                : _presentationTimeSeconds;
            double directedDistanceSeconds = GetDirectedDistanceSeconds(
                startTimeSeconds,
                targetTimeSeconds,
                command.Direction);

            if (command.Mode == TimeCommandMode.SmoothPresentationJump)
            {
                SetAuthoritativeDestinationOnly(targetTimeSeconds);
            }

            if (directedDistanceSeconds <= TIME_EPSILON_SECONDS || transitionDurationSeconds <= 0f)
            {
                CompleteZeroDurationCommand(
                    command.Mode,
                    command.Direction,
                    targetTimeSeconds,
                    directedDistanceSeconds);
                ApplyOutputs(0f);
                UpdatePublicState();
                return CompleteImmediateCommand(commandId, TimeCommandStatus.Completed);
            }

            _activeCommand = new ActiveTimeCommand(
                commandId,
                command.Mode,
                command.Direction,
                targetTimeSeconds,
                startTimeSeconds,
                directedDistanceSeconds,
                transitionDurationSeconds,
                transitionCurve,
                cancellationToken);
            UpdatePublicState();
            return _activeCommand.CompletionSource.Task;
        }

        public IDisposable RegisterControl(TimeControlRequest request)
        {
            if (!TryValidateControlRequest(request, out string errorMessage))
            {
                Echo.Warning($"Time control request was rejected. {errorMessage}");
                return EmptyTimeControlRegistration.Instance;
            }

            TimeControlRegistration registration = new(
                this,
                request,
                ++_nextControlSequence);
            _controlRegistrations.Add(registration);
            ArbitrateControl();
            UpdatePublicState();
            return registration;
        }

        public bool SetContextOverride(TimeOfCycleOverrideSO contextOverrideSO)
        {
            return TrySetOverrides(contextOverrideSO, _runtimeOverrideSO, true);
        }

        public bool SetRuntimeOverride(TimeOfCycleOverrideSO runtimeOverrideSO)
        {
            return TrySetOverrides(_contextOverrideSO, runtimeOverrideSO, false);
        }

        #endregion

        #region Private Methods

        private void TickActiveCommand(float deltaTimeSeconds)
        {
            ActiveTimeCommand command = _activeCommand;
            command.ElapsedSeconds += deltaTimeSeconds;
            float linearProgress01 = Math.Clamp(
                command.ElapsedSeconds / command.DurationSeconds,
                0f,
                1f);
            float evaluatedProgress01 = Math.Clamp(command.TransitionCurve.Evaluate(linearProgress01), 0f, 1f);
            float directedProgress01 = Math.Max(command.DirectedProgress01, evaluatedProgress01);
            if (linearProgress01 >= 1f)
            {
                directedProgress01 = 1f;
            }

            double distanceDeltaSeconds = command.DirectedDistanceSeconds
                                          * (directedProgress01 - command.DirectedProgress01);
            double signedDistanceDeltaSeconds = distanceDeltaSeconds * (int)command.Direction;
            command.DirectedProgress01 = directedProgress01;

            if (command.Mode == TimeCommandMode.SimulatedAdvance)
            {
                AdvanceAuthoritativeTime(signedDistanceDeltaSeconds);
                _presentationTimeSeconds = _authoritativeTimeSeconds;
            }
            else
            {
                _presentationTimeSeconds = NormalizeTime(
                    command.StartTimeSeconds
                    + command.DirectedDistanceSeconds * directedProgress01 * (int)command.Direction);
            }

            if (linearProgress01 < 1f)
            {
                return;
            }

            if (command.Mode == TimeCommandMode.SimulatedAdvance)
            {
                _authoritativeTimeSeconds = command.TargetTimeSeconds;
            }

            _presentationTimeSeconds = command.TargetTimeSeconds;
            CompleteActiveCommand(TimeCommandStatus.Completed);
        }

        private void TickPersistentControlOrAutomaticAdvancement(float deltaTimeSeconds)
        {
            if (_activeControlRegistration == null)
            {
                if (_isAutomaticAdvancementEnabled)
                {
                    AdvanceAuthoritativeTime(
                        _advancementRateCycleSecondsPerRealSecond * deltaTimeSeconds);
                    _presentationTimeSeconds = _authoritativeTimeSeconds;
                }

                return;
            }

            TimeControlRequest request = _activeControlRegistration.Request;
            switch (request.Behavior)
            {
                case TimeControlBehavior.PauseAutomaticAdvancement:
                    return;
                case TimeControlBehavior.OverrideAdvancementRate:
                    AdvanceAuthoritativeTime(
                        request.AdvancementRateCycleSecondsPerRealSecond * deltaTimeSeconds);
                    _presentationTimeSeconds = _authoritativeTimeSeconds;
                    return;
                case TimeControlBehavior.ProvideAuthoritativeTime:
                    if (!request.TimeProvider.IsValid
                        || !request.TimeProvider.TryGetCurrentTimeSeconds(out double providedTimeSeconds)
                        || !IsFinite(providedTimeSeconds))
                    {
                        Echo.Warning(
                            "The active direct-time provider became invalid and was released.");
                        ReleaseControl(_activeControlRegistration);
                        return;
                    }

                    SetDestinationOnly(NormalizeTime(providedTimeSeconds), true);
                    return;
                default:
                    Echo.Warning($"Unsupported time control behavior '{request.Behavior}' was ignored.");
                    return;
            }
        }

        private void CompleteZeroDurationCommand(
            TimeCommandMode mode,
            TimeMovementDirection direction,
            double targetTimeSeconds,
            double directedDistanceSeconds)
        {
            if (mode == TimeCommandMode.SimulatedAdvance)
            {
                AdvanceAuthoritativeTime(directedDistanceSeconds * (int)direction);
                _authoritativeTimeSeconds = targetTimeSeconds;
                _presentationTimeSeconds = targetTimeSeconds;
                return;
            }

            _presentationTimeSeconds = targetTimeSeconds;
        }

        private UniTask<TimeCommandResult> CompleteImmediateCommand(
            long commandId,
            TimeCommandStatus status)
        {
            TimeCommandResult result = new(commandId, status);
            OnTimeCommandCompleted?.Invoke(result);
            return UniTask.FromResult(result);
        }

        private void CompleteActiveCommand(TimeCommandStatus status)
        {
            if (_activeCommand == null)
            {
                return;
            }

            ActiveTimeCommand command = _activeCommand;
            _activeCommand = null;
            TimeCommandResult result = new(command.CommandId, status);
            command.CompletionSource.TrySetResult(result);
            OnTimeCommandCompleted?.Invoke(result);
        }

        private void ResolveCommandTransition(
            TimeCommandTransition requestedTransition,
            out float durationSeconds,
            out AnimationCurve transitionCurve)
        {
            durationSeconds = requestedTransition.UsesProfileDefault
                ? _configuration.DefaultCommandTransitionDurationSeconds
                : Math.Max(0f, requestedTransition.DurationSeconds);
            transitionCurve = requestedTransition.EasingCurve == null
                ? _configuration.DefaultCommandTransitionCurve
                : requestedTransition.EasingCurve;
        }

        private void AdvanceAuthoritativeTime(double signedDeltaSeconds)
        {
            if (Math.Abs(signedDeltaSeconds) <= TIME_EPSILON_SECONDS)
            {
                return;
            }

            List<CycleBoundaryEvent> boundaryEvents = CreateBoundaryEvents(
                _authoritativeTimeSeconds,
                signedDeltaSeconds);
            for (int eventIndex = 0; eventIndex < boundaryEvents.Count; eventIndex++)
            {
                CycleBoundaryEvent boundaryEvent = boundaryEvents[eventIndex];
                if (boundaryEvent.Kind == CycleBoundaryEventKind.CycleCompletion)
                {
                    OnCycleCompleted?.Invoke();
                    continue;
                }

                NotifyPeriodChangedIfDifferent(boundaryEvent.DestinationPeriodId);
            }

            _authoritativeTimeSeconds = NormalizeTime(
                _authoritativeTimeSeconds + signedDeltaSeconds);
            NotifyPeriodChangedIfDifferent(ResolvePeriodId(_authoritativeTimeSeconds));
        }

        private List<CycleBoundaryEvent> CreateBoundaryEvents(
            double startTimeSeconds,
            double signedDeltaSeconds)
        {
            bool isForward = signedDeltaSeconds > 0d;
            double travelDistanceSeconds = Math.Abs(signedDeltaSeconds);
            double cycleDurationSeconds = _configuration.CycleDurationSeconds;
            List<CycleBoundaryEvent> boundaryEvents = new();

            double firstWrapDistanceSeconds = isForward
                ? cycleDurationSeconds - startTimeSeconds
                : startTimeSeconds;
            if (isForward && firstWrapDistanceSeconds <= TIME_EPSILON_SECONDS)
            {
                firstWrapDistanceSeconds = cycleDurationSeconds;
            }

            AddRepeatedBoundaryEvents(
                boundaryEvents,
                firstWrapDistanceSeconds,
                travelDistanceSeconds,
                cycleDurationSeconds,
                CycleBoundaryEvent.CycleCompletion);

            for (int periodIndex = 0; periodIndex < _configuration.Periods.Count; periodIndex++)
            {
                CyclePeriodDefinition period = _configuration.Periods[periodIndex];
                double firstBoundaryDistanceSeconds;
                CyclePeriodId destinationPeriodId;
                if (isForward)
                {
                    firstBoundaryDistanceSeconds = period.StartTimeSeconds - startTimeSeconds;
                    if (firstBoundaryDistanceSeconds <= TIME_EPSILON_SECONDS)
                    {
                        firstBoundaryDistanceSeconds += cycleDurationSeconds;
                    }

                    destinationPeriodId = period.PeriodId;
                }
                else
                {
                    firstBoundaryDistanceSeconds = startTimeSeconds - period.StartTimeSeconds;
                    if (firstBoundaryDistanceSeconds < -TIME_EPSILON_SECONDS)
                    {
                        firstBoundaryDistanceSeconds += cycleDurationSeconds;
                    }
                    else if (Math.Abs(firstBoundaryDistanceSeconds) <= TIME_EPSILON_SECONDS)
                    {
                        firstBoundaryDistanceSeconds = 0d;
                    }

                    int previousPeriodIndex = periodIndex == 0
                        ? _configuration.Periods.Count - 1
                        : periodIndex - 1;
                    destinationPeriodId = _configuration.Periods[previousPeriodIndex].PeriodId;
                }

                AddRepeatedBoundaryEvents(
                    boundaryEvents,
                    firstBoundaryDistanceSeconds,
                    travelDistanceSeconds,
                    cycleDurationSeconds,
                    distanceSeconds => CycleBoundaryEvent.PeriodBoundary(
                        distanceSeconds,
                        destinationPeriodId));
            }

            boundaryEvents.Sort(CycleBoundaryEvent.Compare);
            return boundaryEvents;
        }

        private static void AddRepeatedBoundaryEvents(
            ICollection<CycleBoundaryEvent> boundaryEvents,
            double firstBoundaryDistanceSeconds,
            double travelDistanceSeconds,
            double cycleDurationSeconds,
            Func<double, CycleBoundaryEvent> createBoundaryEvent)
        {
            for (double boundaryDistanceSeconds = firstBoundaryDistanceSeconds;
                 boundaryDistanceSeconds <= travelDistanceSeconds + TIME_EPSILON_SECONDS;
                 boundaryDistanceSeconds += cycleDurationSeconds)
            {
                if (boundaryDistanceSeconds < -TIME_EPSILON_SECONDS)
                {
                    continue;
                }

                boundaryEvents.Add(createBoundaryEvent(Math.Max(0d, boundaryDistanceSeconds)));
            }
        }

        private void SetDestinationOnly(double destinationTimeSeconds, bool doesPresentationFollow)
        {
            SetAuthoritativeDestinationOnly(destinationTimeSeconds);
            if (doesPresentationFollow)
            {
                _presentationTimeSeconds = destinationTimeSeconds;
            }
        }

        private void SetAuthoritativeDestinationOnly(double destinationTimeSeconds)
        {
            _authoritativeTimeSeconds = destinationTimeSeconds;
            NotifyPeriodChangedIfDifferent(ResolvePeriodId(destinationTimeSeconds));
        }

        private void NotifyPeriodChangedIfDifferent(CyclePeriodId destinationPeriodId)
        {
            if (_currentPeriodId.Equals(destinationPeriodId))
            {
                return;
            }

            CyclePeriodId previousPeriodId = _currentPeriodId;
            _currentPeriodId = destinationPeriodId;
            OnPeriodChanged?.Invoke(new CyclePeriodChange(previousPeriodId, destinationPeriodId));
        }

        private CyclePeriodId ResolvePeriodId(double timeSeconds)
        {
            CyclePeriodId selectedPeriodId =
                _configuration.Periods[_configuration.Periods.Count - 1].PeriodId;
            for (int periodIndex = 0; periodIndex < _configuration.Periods.Count; periodIndex++)
            {
                CyclePeriodDefinition period = _configuration.Periods[periodIndex];
                if (period.StartTimeSeconds > timeSeconds)
                {
                    break;
                }

                selectedPeriodId = period.PeriodId;
            }

            return selectedPeriodId;
        }

        private double GetDirectedDistanceSeconds(
            double startTimeSeconds,
            double targetTimeSeconds,
            TimeMovementDirection direction)
        {
            if (Math.Abs(startTimeSeconds - targetTimeSeconds) <= TIME_EPSILON_SECONDS)
            {
                return 0d;
            }

            return direction == TimeMovementDirection.Forward
                ? NormalizeTime(targetTimeSeconds - startTimeSeconds)
                : NormalizeTime(startTimeSeconds - targetTimeSeconds);
        }

        private double NormalizeTime(double timeSeconds)
        {
            double cycleDurationSeconds = _configuration.CycleDurationSeconds;
            double normalizedTimeSeconds = timeSeconds % cycleDurationSeconds;
            return normalizedTimeSeconds < 0d
                ? normalizedTimeSeconds + cycleDurationSeconds
                : normalizedTimeSeconds;
        }

        private void ApplyOutputs(float deltaTimeSeconds)
        {
            _outputRuntimeSet.Update(_presentationTimeSeconds, deltaTimeSeconds);
            IReadOnlyCycleOutputState outputState = _outputRuntimeSet.OutputState;
            for (int applicatorIndex = 0; applicatorIndex < _outputApplicators.Count; applicatorIndex++)
            {
                _outputApplicators[applicatorIndex].Apply(outputState);
            }
        }

        private bool TrySetOverrides(
            TimeOfCycleOverrideSO contextOverrideSO,
            TimeOfCycleOverrideSO runtimeOverrideSO,
            bool isContextOverrideChange)
        {
            if (!_isInitialized)
            {
                Echo.Warning("Time-of-cycle profile overrides cannot change before initialization completes.");
                return false;
            }

            if (!TimeOfCycleConfigurationResolver.TryResolve(
                    _sharedProfileSO,
                    contextOverrideSO,
                    runtimeOverrideSO,
                    out ResolvedTimeOfCycleConfiguration replacementConfiguration,
                    out string errorMessage))
            {
                Echo.Warning($"Time-of-cycle profile change was rejected. {errorMessage}");
                return false;
            }

            CompleteActiveCommand(TimeCommandStatus.Cancelled);
            double authoritativeProgress01 =
                _authoritativeTimeSeconds / _configuration.CycleDurationSeconds;
            double presentationProgress01 =
                _presentationTimeSeconds / _configuration.CycleDurationSeconds;
            _configuration = replacementConfiguration;
            _authoritativeTimeSeconds = authoritativeProgress01 * _configuration.CycleDurationSeconds;
            _presentationTimeSeconds = presentationProgress01 * _configuration.CycleDurationSeconds;
            NotifyPeriodChangedIfDifferent(ResolvePeriodId(_authoritativeTimeSeconds));
            _outputRuntimeSet.Configure(_configuration, _presentationTimeSeconds, false);
            ApplyOutputs(0f);

            if (isContextOverrideChange)
            {
                _contextOverrideSO = contextOverrideSO;
            }
            else
            {
                _runtimeOverrideSO = runtimeOverrideSO;
            }

            UpdatePublicState();
            return true;
        }

        private bool TryValidateControlRequest(TimeControlRequest request, out string errorMessage)
        {
            if (request == null)
            {
                errorMessage = "The request is null.";
                return false;
            }

            if (!Enum.IsDefined(typeof(TimeControlBehavior), request.Behavior))
            {
                errorMessage = $"Behavior '{request.Behavior}' is not supported.";
                return false;
            }

            if (request.Behavior == TimeControlBehavior.OverrideAdvancementRate
                && !IsFiniteNonNegative(request.AdvancementRateCycleSecondsPerRealSecond))
            {
                errorMessage = "Override advancement rate must be finite and non-negative.";
                return false;
            }

            if (request.Behavior == TimeControlBehavior.ProvideAuthoritativeTime
                && (request.TimeProvider == null || !request.TimeProvider.IsValid))
            {
                errorMessage = "A valid direct-time provider is required.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        private void ArbitrateControl()
        {
            for (int registrationIndex = _controlRegistrations.Count - 1;
                 registrationIndex >= 0;
                 registrationIndex--)
            {
                TimeControlRegistration registration = _controlRegistrations[registrationIndex];
                if (registration.IsReleased)
                {
                    _controlRegistrations.RemoveAt(registrationIndex);
                    continue;
                }

                TimeControlRequest request = registration.Request;
                if (request.Behavior == TimeControlBehavior.ProvideAuthoritativeTime
                    && (request.TimeProvider == null || !request.TimeProvider.IsValid))
                {
                    Echo.Warning("A direct-time control registration became invalid and was released.");
                    registration.MarkReleased();
                    _controlRegistrations.RemoveAt(registrationIndex);
                }
            }

            TimeControlRegistration selectedRegistration = null;
            for (int registrationIndex = 0;
                 registrationIndex < _controlRegistrations.Count;
                 registrationIndex++)
            {
                TimeControlRegistration candidate = _controlRegistrations[registrationIndex];
                if (selectedRegistration == null
                    || candidate.Request.Priority > selectedRegistration.Request.Priority
                    || candidate.Request.Priority == selectedRegistration.Request.Priority
                    && candidate.Sequence > selectedRegistration.Sequence)
                {
                    selectedRegistration = candidate;
                }
            }

            if (ReferenceEquals(_activeControlRegistration, selectedRegistration))
            {
                return;
            }

            _activeControlRegistration = selectedRegistration;
            if (_activeCommand != null
                && _activeControlRegistration != null
                && !_activeControlRegistration.Request.AllowsTimeCommands)
            {
                CompleteActiveCommand(TimeCommandStatus.Cancelled);
            }
        }

        private void ReleaseControl(TimeControlRegistration registration)
        {
            if (registration.IsReleased)
            {
                return;
            }

            registration.MarkReleased();
            _controlRegistrations.Remove(registration);
            ArbitrateControl();
            UpdatePublicState();
        }

        private void UpdatePublicState()
        {
            bool isAutomaticAdvancementActive;
            double effectiveRateCycleSecondsPerRealSecond;
            if (_activeCommand != null)
            {
                isAutomaticAdvancementActive = false;
                effectiveRateCycleSecondsPerRealSecond = 0d;
            }
            else if (_activeControlRegistration == null)
            {
                isAutomaticAdvancementActive = _isAutomaticAdvancementEnabled;
                effectiveRateCycleSecondsPerRealSecond = _isAutomaticAdvancementEnabled
                    ? _advancementRateCycleSecondsPerRealSecond
                    : 0d;
            }
            else if (_activeControlRegistration.Request.Behavior
                     == TimeControlBehavior.OverrideAdvancementRate)
            {
                isAutomaticAdvancementActive = true;
                effectiveRateCycleSecondsPerRealSecond =
                    _activeControlRegistration.Request.AdvancementRateCycleSecondsPerRealSecond;
            }
            else
            {
                isAutomaticAdvancementActive = false;
                effectiveRateCycleSecondsPerRealSecond = 0d;
            }

            double normalizedCycleProgress01 = _configuration == null
                ? 0d
                : _authoritativeTimeSeconds / _configuration.CycleDurationSeconds;
            CurrentState = new TimeOfCycleState(
                _authoritativeTimeSeconds,
                _presentationTimeSeconds,
                normalizedCycleProgress01,
                _currentPeriodId,
                isAutomaticAdvancementActive,
                effectiveRateCycleSecondsPerRealSecond);
        }

        private static bool IsFiniteNonNegative(double value)
        {
            return IsFinite(value) && value >= 0d;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        #endregion

        /// <summary>
        /// Holds mutable progress and completion state for the currently active command.
        /// </summary>
        private sealed class ActiveTimeCommand
        {
            public long CommandId { get; }
            public TimeCommandMode Mode { get; }
            public TimeMovementDirection Direction { get; }
            public double TargetTimeSeconds { get; }
            public double StartTimeSeconds { get; }
            public double DirectedDistanceSeconds { get; }
            public float DurationSeconds { get; }
            public AnimationCurve TransitionCurve { get; }
            public CancellationToken CancellationToken { get; }
            public UniTaskCompletionSource<TimeCommandResult> CompletionSource { get; } = new();
            public float ElapsedSeconds { get; set; }
            public float DirectedProgress01 { get; set; }

            public ActiveTimeCommand(
                long commandId,
                TimeCommandMode mode,
                TimeMovementDirection direction,
                double targetTimeSeconds,
                double startTimeSeconds,
                double directedDistanceSeconds,
                float durationSeconds,
                AnimationCurve transitionCurve,
                CancellationToken cancellationToken)
            {
                CommandId = commandId;
                Mode = mode;
                Direction = direction;
                TargetTimeSeconds = targetTimeSeconds;
                StartTimeSeconds = startTimeSeconds;
                DirectedDistanceSeconds = directedDistanceSeconds;
                DurationSeconds = durationSeconds;
                TransitionCurve = transitionCurve;
                CancellationToken = cancellationToken;
            }
        }

        /// <summary>
        /// Represents one chronological period or cycle boundary crossed during progression.
        /// </summary>
        private readonly struct CycleBoundaryEvent
        {
            public double DistanceSeconds { get; }
            public CycleBoundaryEventKind Kind { get; }
            public CyclePeriodId DestinationPeriodId { get; }

            private CycleBoundaryEvent(
                double distanceSeconds,
                CycleBoundaryEventKind kind,
                CyclePeriodId destinationPeriodId)
            {
                DistanceSeconds = distanceSeconds;
                Kind = kind;
                DestinationPeriodId = destinationPeriodId;
            }

            #region Public Methods

            public static CycleBoundaryEvent CycleCompletion(double distanceSeconds)
            {
                return new CycleBoundaryEvent(
                    distanceSeconds,
                    CycleBoundaryEventKind.CycleCompletion,
                    default);
            }

            public static CycleBoundaryEvent PeriodBoundary(
                double distanceSeconds,
                CyclePeriodId destinationPeriodId)
            {
                return new CycleBoundaryEvent(
                    distanceSeconds,
                    CycleBoundaryEventKind.PeriodBoundary,
                    destinationPeriodId);
            }

            public static int Compare(CycleBoundaryEvent left, CycleBoundaryEvent right)
            {
                int distanceComparison = left.DistanceSeconds.CompareTo(right.DistanceSeconds);
                return distanceComparison != 0
                    ? distanceComparison
                    : left.Kind.CompareTo(right.Kind);
            }

            #endregion
        }

        /// <summary>
        /// Distinguishes chronological wrap notifications from period ownership changes.
        /// </summary>
        private enum CycleBoundaryEventKind
        {
            CycleCompletion,
            PeriodBoundary,
        }

        /// <summary>
        /// Owns the lifetime of one registered external control request.
        /// </summary>
        private sealed class TimeControlRegistration : IDisposable
        {
            private readonly TimeOfCycleService _owner;

            public TimeControlRequest Request { get; }
            public long Sequence { get; }
            public bool IsReleased { get; private set; }

            public TimeControlRegistration(
                TimeOfCycleService owner,
                TimeControlRequest request,
                long sequence)
            {
                _owner = owner;
                Request = request;
                Sequence = sequence;
            }

            #region Public Methods

            public void Dispose()
            {
                _owner.ReleaseControl(this);
            }

            public void MarkReleased()
            {
                IsReleased = true;
            }

            #endregion
        }

        /// <summary>
        /// Provides a safe no-op handle when a control request is rejected.
        /// </summary>
        private sealed class EmptyTimeControlRegistration : IDisposable
        {
            public static EmptyTimeControlRegistration Instance { get; } = new();

            #region Public Methods

            public void Dispose()
            {
            }

            #endregion
        }
    }
}
