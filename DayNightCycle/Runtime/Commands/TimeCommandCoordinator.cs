using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Validates and executes one-time clock commands while publishing their observable outcomes.
    /// </summary>
    internal sealed class TimeCommandCoordinator
    {
        private const double TIME_EPSILON_SECONDS = 0.000001d;

        private readonly CycleClock _clock;
        private readonly TimeCommandRunner _runner = new();
        private readonly Action<IReadOnlyList<CycleClockNotification>> _publishClockNotifications;
        private readonly Action<TimeCommandResult> _publishCommandCompletion;

        private ResolvedTimeOfCycleConfiguration _configuration;

        public bool IsActive => _runner.IsActive;

        public TimeCommandCoordinator(
            CycleClock clock,
            Action<IReadOnlyList<CycleClockNotification>> publishClockNotifications,
            Action<TimeCommandResult> publishCommandCompletion)
        {
            _clock = clock;
            _publishClockNotifications = publishClockNotifications;
            _publishCommandCompletion = publishCommandCompletion;
        }

        #region Public Methods

        public void Configure(ResolvedTimeOfCycleConfiguration configuration)
        {
            _configuration = configuration;
        }

        public UniTask<TimeCommandResult> ExecuteAsync(
            TimeCommand command,
            bool isServiceInitialized,
            bool areCommandsAllowed,
            CancellationToken cancellationToken,
            out bool hasChangedPresentationImmediately)
        {
            hasChangedPresentationImmediately = false;
            long commandId = _runner.CreateCommandId();
            if (!CanExecute(
                    commandId,
                    command,
                    isServiceInitialized,
                    areCommandsAllowed,
                    cancellationToken,
                    out TimeCommandStatus rejectionStatus))
            {
                return CompleteImmediate(commandId, rejectionStatus);
            }

            CompleteActive(TimeCommandStatus.Replaced);
            double targetTimeSeconds = _clock.NormalizeTime(command.TargetTimeSeconds);
            if (command.Mode == TimeCommandMode.Immediate)
            {
                SetDestination(targetTimeSeconds, true);
                hasChangedPresentationImmediately = true;
                return CompleteImmediate(commandId, TimeCommandStatus.Completed);
            }

            ResolveTransition(
                command.Transition,
                out float transitionDurationSeconds,
                out AnimationCurve transitionCurve);
            double startTimeSeconds = command.Mode == TimeCommandMode.SimulatedAdvance
                ? _clock.AuthoritativeTimeSeconds
                : _clock.PresentationTimeSeconds;
            double directedDistanceSeconds = _clock.GetDirectedDistanceSeconds(
                startTimeSeconds,
                targetTimeSeconds,
                command.Direction);
            if (command.Mode == TimeCommandMode.SmoothPresentationJump)
            {
                SetAuthoritativeDestination(targetTimeSeconds);
            }

            if (directedDistanceSeconds <= TIME_EPSILON_SECONDS || transitionDurationSeconds <= 0f)
            {
                CompleteZeroDurationCommand(
                    command.Mode,
                    command.Direction,
                    targetTimeSeconds,
                    directedDistanceSeconds);
                hasChangedPresentationImmediately = true;
                return CompleteImmediate(commandId, TimeCommandStatus.Completed);
            }

            return _runner.Start(
                commandId,
                command.Mode,
                command.Direction,
                targetTimeSeconds,
                startTimeSeconds,
                directedDistanceSeconds,
                transitionDurationSeconds,
                transitionCurve,
                cancellationToken);
        }

        public void Tick(float deltaTimeSeconds)
        {
            if (_runner.IsCancellationRequested)
            {
                CompleteActive(TimeCommandStatus.Cancelled);
                return;
            }

            TimeCommandFrame frame = _runner.Tick(deltaTimeSeconds);
            if (_runner.Mode == TimeCommandMode.SimulatedAdvance)
            {
                if (!TryAdvanceAuthoritativeTime(frame.SignedDistanceDeltaSeconds))
                {
                    return;
                }

                _clock.PresentationTimeSeconds = _clock.AuthoritativeTimeSeconds;
            }
            else
            {
                _clock.PresentationTimeSeconds = _clock.NormalizeTime(
                    _runner.StartTimeSeconds
                    + _runner.DirectedDistanceSeconds
                    * frame.DirectedProgress01
                    * (int)_runner.Direction);
            }

            if (!frame.HasCompleted)
            {
                return;
            }

            if (_runner.Mode == TimeCommandMode.SimulatedAdvance)
            {
                _clock.AuthoritativeTimeSeconds = _runner.TargetTimeSeconds;
            }

            _clock.PresentationTimeSeconds = _runner.TargetTimeSeconds;
            CompleteActive(TimeCommandStatus.Completed);
        }

        public void Cancel()
        {
            CompleteActive(TimeCommandStatus.Cancelled);
        }

        #endregion

        #region Private Methods

        private bool CanExecute(
            long commandId,
            TimeCommand command,
            bool isServiceInitialized,
            bool areCommandsAllowed,
            CancellationToken cancellationToken,
            out TimeCommandStatus rejectionStatus)
        {
            if (!isServiceInitialized)
            {
                Echo.Warning($"Time command {commandId} was rejected because the service is not initialized.");
                rejectionStatus = TimeCommandStatus.Rejected;
                return false;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                rejectionStatus = TimeCommandStatus.Cancelled;
                return false;
            }

            if (!areCommandsAllowed)
            {
                Echo.Warning($"Time command {commandId} was rejected because the active controller blocks commands.");
                rejectionStatus = TimeCommandStatus.Rejected;
                return false;
            }

            if (!CycleNumericValidation.IsFinite(command.TargetTimeSeconds)
                || !IsValidTransition(command.Transition)
                || command.Transition.EasingCurve != null
                && !CycleCurveValidation.TryValidate(command.Transition.EasingCurve, out _)
                || !Enum.IsDefined(typeof(TimeCommandMode), command.Mode)
                || !Enum.IsDefined(typeof(TimeMovementDirection), command.Direction))
            {
                Echo.Warning($"Time command {commandId} contains an invalid target, transition, mode, or direction.");
                rejectionStatus = TimeCommandStatus.Rejected;
                return false;
            }

            rejectionStatus = default;
            return true;
        }

        private void CompleteZeroDurationCommand(
            TimeCommandMode mode,
            TimeMovementDirection direction,
            double targetTimeSeconds,
            double directedDistanceSeconds)
        {
            if (mode == TimeCommandMode.SimulatedAdvance)
            {
                if (!TryAdvanceAuthoritativeTime(directedDistanceSeconds * (int)direction))
                {
                    return;
                }

                _clock.AuthoritativeTimeSeconds = targetTimeSeconds;
            }

            _clock.PresentationTimeSeconds = targetTimeSeconds;
        }

        private bool TryAdvanceAuthoritativeTime(double signedDeltaSeconds)
        {
            if (_clock.TryAdvanceAuthoritativeTime(
                    signedDeltaSeconds,
                    out IReadOnlyList<CycleClockNotification> notifications,
                    out string errorMessage))
            {
                _publishClockNotifications(notifications);
                return true;
            }

            Echo.Warning($"Time command advancement was rejected. {errorMessage}");
            CompleteActive(TimeCommandStatus.Rejected);
            return false;
        }

        private void SetDestination(double destinationTimeSeconds, bool doesPresentationFollow)
        {
            SetAuthoritativeDestination(destinationTimeSeconds);
            if (doesPresentationFollow)
            {
                _clock.PresentationTimeSeconds = destinationTimeSeconds;
            }
        }

        private void SetAuthoritativeDestination(double destinationTimeSeconds)
        {
            if (_clock.SetAuthoritativeDestination(destinationTimeSeconds, out CyclePeriodChange periodChange))
            {
                _publishClockNotifications(new[] { CycleClockNotification.PeriodChanged(periodChange) });
            }
        }

        private void ResolveTransition(
            TimeCommandTransition requestedTransition,
            out float durationSeconds,
            out AnimationCurve transitionCurve)
        {
            durationSeconds = requestedTransition.UsesProfileDefault
                ? _configuration.DefaultCommandTransitionDurationSeconds
                : requestedTransition.DurationSeconds;
            transitionCurve = requestedTransition.EasingCurve == null
                ? _configuration.DefaultCommandTransitionCurve
                : requestedTransition.EasingCurve;
        }

        private UniTask<TimeCommandResult> CompleteImmediate(long commandId, TimeCommandStatus status)
        {
            TimeCommandResult result = new(commandId, status);
            _publishCommandCompletion(result);
            return UniTask.FromResult(result);
        }

        private void CompleteActive(TimeCommandStatus status)
        {
            if (_runner.TryComplete(status, out TimeCommandResult result))
            {
                _publishCommandCompletion(result);
            }
        }

        private static bool IsValidTransition(TimeCommandTransition transition)
        {
            return transition.UsesProfileDefault
                   || CycleNumericValidation.IsFiniteNonNegative(transition.DurationSeconds);
        }

        #endregion
    }
}
