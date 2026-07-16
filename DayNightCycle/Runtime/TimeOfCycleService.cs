using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using UnityEngine;
using VContainer.Unity;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Coordinates configuration, clock, commands, persistent control, outputs, and public notifications.
    /// </summary>
    public sealed class TimeOfCycleService :
        ITimeOfCycle,
        ITimeControlContext,
        IInitializable,
        ITickable,
        IDisposable
    {
        private readonly CycleOutputCoordinator _outputCoordinator;
        private readonly CycleClock _clock = new();
        private readonly TimeControlArbiter _controlArbiter;
        private readonly TimeCommandCoordinator _commandCoordinator;
        private readonly TimeOfCycleConfigurationSession _configurationSession;

        private double _advancementRateCycleSecondsPerRealSecond;
        private bool _isAutomaticAdvancementEnabled;
        private bool _isInitialized;

        public TimeOfCycleState CurrentState { get; private set; }

        public event Action<CyclePeriodChange> OnPeriodChanged;
        public event Action OnCycleCompleted;
        public event Action<TimeCommandResult> OnTimeCommandCompleted;

        public TimeOfCycleService(
            TimeOfCycleProfileSO sharedProfileSO,
            IEnumerable<ITimeOfCycleOutputApplicator> outputApplicators)
        {
            _outputCoordinator = new CycleOutputCoordinator(outputApplicators);
            _controlArbiter = new TimeControlArbiter(ApplyActiveControlPolicy);
            _commandCoordinator = new TimeCommandCoordinator(
                _clock,
                PublishClockNotifications,
                PublishCommandCompletion);
            _configurationSession = new TimeOfCycleConfigurationSession(
                sharedProfileSO,
                _clock,
                _commandCoordinator,
                _outputCoordinator);
        }

        #region Public Methods

        public void Initialize()
        {
            if (!_configurationSession.TryInitialize(out string errorMessage))
            {
                Echo.Error($"Time-of-cycle initialization failed. {errorMessage}");
                return;
            }

            ResolvedTimeOfCycleConfiguration configuration =
                _configurationSession.CurrentConfiguration;
            _advancementRateCycleSecondsPerRealSecond =
                configuration.DefaultAdvancementRateCycleSecondsPerRealSecond;
            _isAutomaticAdvancementEnabled = configuration.DoesAutomaticAdvancementBeginEnabled;
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
            if (_controlArbiter.RemoveInvalidRegistrations())
            {
                Echo.Warning("One or more invalid direct-time control registrations were released.");
            }

            if (_commandCoordinator.IsActive)
            {
                _commandCoordinator.Tick(deltaTimeSeconds);
            }

            if (!_commandCoordinator.IsActive)
            {
                TickPersistentControlOrAutomaticAdvancement(deltaTimeSeconds);
            }

            ApplyOutputs(deltaTimeSeconds);
            UpdatePublicState();
        }

        public void Dispose()
        {
            _commandCoordinator.Cancel();
            _controlArbiter.Clear();
        }

        public void SetAutomaticAdvancementEnabled(bool isEnabled)
        {
            _isAutomaticAdvancementEnabled = isEnabled;
            UpdatePublicState();
        }

        public bool TrySetAdvancementRate(double advancementRateCycleSecondsPerRealSecond)
        {
            if (!CycleNumericValidation.IsFiniteNonNegative(advancementRateCycleSecondsPerRealSecond))
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
            bool areCommandsAllowed =
                _controlArbiter.ActiveRequest == null || _controlArbiter.ActiveRequest.AllowsTimeCommands;
            UniTask<TimeCommandResult> completionTask = _commandCoordinator.ExecuteAsync(
                command,
                _isInitialized,
                areCommandsAllowed,
                cancellationToken,
                out bool hasChangedPresentationImmediately);
            if (hasChangedPresentationImmediately)
            {
                ApplyOutputs(0f);
            }

            UpdatePublicState();
            return completionTask;
        }

        public IDisposable RegisterControl(TimeControlRequest request)
        {
            string errorMessage = "The request is null.";
            if (request?.Strategy == null || !request.Strategy.TryValidate(out errorMessage))
            {
                Echo.Warning($"Time control request was rejected. {errorMessage}");
                return EmptyTimeControlRegistration.Instance;
            }

            IDisposable registration = _controlArbiter.Register(request);
            UpdatePublicState();
            return registration;
        }

        public bool SetContextOverride(TimeOfCycleOverrideSO contextOverrideSO)
        {
            return TrySetOverride(
                _configurationSession.TrySetContextOverride,
                contextOverrideSO);
        }

        public bool SetRuntimeOverride(TimeOfCycleOverrideSO runtimeOverrideSO)
        {
            return TrySetOverride(
                _configurationSession.TrySetRuntimeOverride,
                runtimeOverrideSO);
        }

        #endregion

        #region Private Methods

        private void TickPersistentControlOrAutomaticAdvancement(float deltaTimeSeconds)
        {
            TimeControlRequest activeRequest = _controlArbiter.ActiveRequest;
            if (activeRequest == null)
            {
                if (_isAutomaticAdvancementEnabled)
                {
                    TryAdvanceAuthoritativeTime(
                        _advancementRateCycleSecondsPerRealSecond * deltaTimeSeconds);
                }

                return;
            }

            if (!activeRequest.Strategy.TryApply(this, deltaTimeSeconds))
            {
                Echo.Warning("The active direct-time provider became invalid and was released.");
                _controlArbiter.ReleaseActiveRequest();
            }
        }

        bool ITimeControlContext.TryAdvanceAuthoritativeTime(double signedDeltaSeconds)
        {
            return TryAdvanceAuthoritativeTime(signedDeltaSeconds);
        }

        bool ITimeControlContext.TrySetAuthoritativeTime(double authoritativeTimeSeconds)
        {
            if (!CycleNumericValidation.IsFinite(authoritativeTimeSeconds))
            {
                Echo.Warning("A persistent controller produced a non-finite authoritative time.");
                return false;
            }

            SetDestination(_clock.NormalizeTime(authoritativeTimeSeconds));
            return true;
        }

        private bool TryAdvanceAuthoritativeTime(double signedDeltaSeconds)
        {
            if (!_clock.TryAdvanceAuthoritativeTime(
                    signedDeltaSeconds,
                    out IReadOnlyList<CycleClockNotification> notifications,
                    out string errorMessage))
            {
                Echo.Warning($"Time-of-cycle advancement was rejected. {errorMessage}");
                return false;
            }

            PublishClockNotifications(notifications);
            _clock.PresentationTimeSeconds = _clock.AuthoritativeTimeSeconds;
            return true;
        }

        private void SetDestination(double destinationTimeSeconds)
        {
            if (_clock.SetAuthoritativeDestination(destinationTimeSeconds, out CyclePeriodChange periodChange))
            {
                OnPeriodChanged?.Invoke(periodChange);
            }

            _clock.PresentationTimeSeconds = destinationTimeSeconds;
        }

        private bool TrySetOverride(
            TryApplyOverride tryApplyOverride,
            TimeOfCycleOverrideSO overrideSO)
        {
            if (!_isInitialized)
            {
                Echo.Warning("Time-of-cycle profile overrides cannot change before initialization completes.");
                return false;
            }

            if (!tryApplyOverride(overrideSO, out CyclePeriodChange? periodChange, out string errorMessage))
            {
                Echo.Warning($"Time-of-cycle profile change was rejected. {errorMessage}");
                return false;
            }

            if (periodChange.HasValue)
            {
                OnPeriodChanged?.Invoke(periodChange.Value);
            }

            ApplyOutputs(0f);
            UpdatePublicState();
            return true;
        }

        private void ApplyActiveControlPolicy()
        {
            if (_commandCoordinator.IsActive
                && _controlArbiter.ActiveRequest != null
                && !_controlArbiter.ActiveRequest.AllowsTimeCommands)
            {
                _commandCoordinator.Cancel();
            }

            UpdatePublicState();
        }

        private void PublishClockNotifications(IReadOnlyList<CycleClockNotification> notifications)
        {
            for (int notificationIndex = 0;
                 notificationIndex < notifications.Count;
                 notificationIndex++)
            {
                CycleClockNotification notification = notifications[notificationIndex];
                if (notification.IsCycleCompleted)
                {
                    OnCycleCompleted?.Invoke();
                }
                else
                {
                    OnPeriodChanged?.Invoke(notification.PeriodChange);
                }
            }
        }

        private void PublishCommandCompletion(TimeCommandResult result)
        {
            OnTimeCommandCompleted?.Invoke(result);
        }

        private void ApplyOutputs(float deltaTimeSeconds)
        {
            _outputCoordinator.UpdateAndApply(_clock.PresentationTimeSeconds, deltaTimeSeconds);
        }

        private void UpdatePublicState()
        {
            bool isAutomaticAdvancementActive = false;
            double effectiveRateCycleSecondsPerRealSecond = 0d;
            if (!_commandCoordinator.IsActive)
            {
                if (_controlArbiter.ActiveRequest != null)
                {
                    isAutomaticAdvancementActive =
                        _controlArbiter.ActiveRequest.Strategy.IsAutomaticAdvancementActive;
                    effectiveRateCycleSecondsPerRealSecond =
                        _controlArbiter.ActiveRequest.Strategy.EffectiveRateCycleSecondsPerRealSecond;
                }
                else if (_isAutomaticAdvancementEnabled)
                {
                    isAutomaticAdvancementActive = true;
                    effectiveRateCycleSecondsPerRealSecond =
                        _advancementRateCycleSecondsPerRealSecond;
                }
            }

            ResolvedTimeOfCycleConfiguration configuration =
                _configurationSession.CurrentConfiguration;
            double normalizedCycleProgress01 = configuration == null
                ? 0d
                : _clock.AuthoritativeTimeSeconds / configuration.CycleDurationSeconds;
            CurrentState = new TimeOfCycleState(
                _clock.AuthoritativeTimeSeconds,
                _clock.PresentationTimeSeconds,
                normalizedCycleProgress01,
                _clock.CurrentPeriodId,
                isAutomaticAdvancementActive,
                effectiveRateCycleSecondsPerRealSecond);
        }

        #endregion

        private delegate bool TryApplyOverride(
            TimeOfCycleOverrideSO overrideSO,
            out CyclePeriodChange? periodChange,
            out string errorMessage);
    }
}
