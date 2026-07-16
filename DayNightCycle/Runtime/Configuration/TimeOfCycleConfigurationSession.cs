namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Resolves and atomically applies the active profile and override layers to runtime collaborators.
    /// </summary>
    internal sealed class TimeOfCycleConfigurationSession
    {
        private readonly TimeOfCycleProfileSO _sharedProfileSO;
        private readonly CycleClock _clock;
        private readonly TimeCommandCoordinator _commandCoordinator;
        private readonly CycleOutputCoordinator _outputCoordinator;

        private TimeOfCycleOverrideSO _contextOverrideSO;
        private TimeOfCycleOverrideSO _runtimeOverrideSO;

        public ResolvedTimeOfCycleConfiguration CurrentConfiguration { get; private set; }

        public TimeOfCycleConfigurationSession(
            TimeOfCycleProfileSO sharedProfileSO,
            CycleClock clock,
            TimeCommandCoordinator commandCoordinator,
            CycleOutputCoordinator outputCoordinator)
        {
            _sharedProfileSO = sharedProfileSO;
            _clock = clock;
            _commandCoordinator = commandCoordinator;
            _outputCoordinator = outputCoordinator;
        }

        #region Public Methods

        public bool TryInitialize(out string errorMessage)
        {
            if (!TryResolve(null, null, out ResolvedTimeOfCycleConfiguration configuration, out errorMessage))
            {
                return false;
            }

            CurrentConfiguration = configuration;
            _clock.Configure(
                configuration,
                configuration.DefaultStartingTimeSeconds,
                configuration.DefaultStartingTimeSeconds);
            _commandCoordinator.Configure(configuration);
            _outputCoordinator.Configure(configuration, _clock.PresentationTimeSeconds, true);
            return true;
        }

        public bool TrySetContextOverride(
            TimeOfCycleOverrideSO contextOverrideSO,
            out CyclePeriodChange? periodChange,
            out string errorMessage)
        {
            if (!TryApply(contextOverrideSO, _runtimeOverrideSO, out periodChange, out errorMessage))
            {
                return false;
            }

            _contextOverrideSO = contextOverrideSO;
            return true;
        }

        public bool TrySetRuntimeOverride(
            TimeOfCycleOverrideSO runtimeOverrideSO,
            out CyclePeriodChange? periodChange,
            out string errorMessage)
        {
            if (!TryApply(_contextOverrideSO, runtimeOverrideSO, out periodChange, out errorMessage))
            {
                return false;
            }

            _runtimeOverrideSO = runtimeOverrideSO;
            return true;
        }

        #endregion

        #region Private Methods

        private bool TryApply(
            TimeOfCycleOverrideSO contextOverrideSO,
            TimeOfCycleOverrideSO runtimeOverrideSO,
            out CyclePeriodChange? periodChange,
            out string errorMessage)
        {
            periodChange = null;
            if (!TryResolve(
                    contextOverrideSO,
                    runtimeOverrideSO,
                    out ResolvedTimeOfCycleConfiguration replacementConfiguration,
                    out errorMessage))
            {
                return false;
            }

            _commandCoordinator.Cancel();
            CyclePeriodId previousPeriodId = _clock.CurrentPeriodId;
            double authoritativeProgress01 =
                _clock.AuthoritativeTimeSeconds / CurrentConfiguration.CycleDurationSeconds;
            double presentationProgress01 =
                _clock.PresentationTimeSeconds / CurrentConfiguration.CycleDurationSeconds;

            CurrentConfiguration = replacementConfiguration;
            _clock.Configure(
                replacementConfiguration,
                authoritativeProgress01 * replacementConfiguration.CycleDurationSeconds,
                presentationProgress01 * replacementConfiguration.CycleDurationSeconds);
            _commandCoordinator.Configure(replacementConfiguration);
            _outputCoordinator.Configure(replacementConfiguration, _clock.PresentationTimeSeconds, false);

            if (!previousPeriodId.Equals(_clock.CurrentPeriodId))
            {
                periodChange = new CyclePeriodChange(previousPeriodId, _clock.CurrentPeriodId);
            }

            return true;
        }

        private bool TryResolve(
            TimeOfCycleOverrideSO contextOverrideSO,
            TimeOfCycleOverrideSO runtimeOverrideSO,
            out ResolvedTimeOfCycleConfiguration configuration,
            out string errorMessage)
        {
            if (!TimeOfCycleConfigurationResolver.TryResolve(
                    _sharedProfileSO,
                    contextOverrideSO,
                    runtimeOverrideSO,
                    out configuration,
                    out errorMessage))
            {
                return false;
            }

            return _outputCoordinator.TryValidateContracts(configuration, out errorMessage);
        }

        #endregion
    }
}
