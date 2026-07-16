using System.Collections.Generic;
using System.Linq;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Validates applicator contracts, evaluates configured outputs, and applies their current values.
    /// </summary>
    internal sealed class CycleOutputCoordinator
    {
        private readonly IReadOnlyList<ITimeOfCycleOutputApplicator> _applicators;
        private readonly CycleOutputRuntimeSet _runtimeSet = new();

        public CycleOutputCoordinator(IEnumerable<ITimeOfCycleOutputApplicator> applicators)
        {
            _applicators = applicators.ToArray();
        }

        #region Public Methods

        public bool TryValidateContracts(
            ResolvedTimeOfCycleConfiguration configuration,
            out string errorMessage)
        {
            return ValidateContracts(configuration, out errorMessage);
        }

        public void Configure(
            ResolvedTimeOfCycleConfiguration configuration,
            double presentationTimeSeconds,
            bool isInitialConfiguration)
        {
            _runtimeSet.Configure(configuration, presentationTimeSeconds, isInitialConfiguration);
        }

        public void UpdateAndApply(double presentationTimeSeconds, float deltaTimeSeconds)
        {
            _runtimeSet.Update(presentationTimeSeconds, deltaTimeSeconds);
            IReadOnlyCycleOutputState outputState = _runtimeSet.OutputState;
            for (int applicatorIndex = 0; applicatorIndex < _applicators.Count; applicatorIndex++)
            {
                _applicators[applicatorIndex].Apply(outputState);
            }
        }

        #endregion

        #region Private Methods

        private bool ValidateContracts(
            ResolvedTimeOfCycleConfiguration configuration,
            out string errorMessage)
        {
            HashSet<CycleOutputKeySO> configuredKeys = new();
            for (int definitionIndex = 0;
                 definitionIndex < configuration.OutputDefinitions.Count;
                 definitionIndex++)
            {
                CycleOutputDefinition definition = configuration.OutputDefinitions[definitionIndex];
                ICycleOutputEvaluator evaluator = definition.CreateEvaluator(
                    configuration.CycleDurationSeconds,
                    configuration.Periods);
                if (evaluator.ValueType != definition.OutputKeySO.ValueType)
                {
                    errorMessage =
                        $"Evaluator for '{definition.OutputKeySO.name}' declares {evaluator.ValueType.Name}, " +
                        $"but its key requires {definition.OutputKeySO.ValueType.Name}.";
                    return false;
                }

                object initialValue = evaluator.Evaluate(configuration.DefaultStartingTimeSeconds);
                if (!CycleOutputValueValidation.IsValidRuntimeValue(
                        initialValue,
                        definition.OutputKeySO.ValueType))
                {
                    errorMessage =
                        $"Evaluator for '{definition.OutputKeySO.name}' produced an invalid initial value.";
                    return false;
                }

                configuredKeys.Add(definition.OutputKeySO);
            }

            for (int applicatorIndex = 0; applicatorIndex < _applicators.Count; applicatorIndex++)
            {
                ITimeOfCycleOutputApplicator applicator = _applicators[applicatorIndex];
                IReadOnlyList<CycleOutputKeySO> requiredKeys = applicator.RequiredOutputKeys;
                for (int keyIndex = 0; keyIndex < requiredKeys.Count; keyIndex++)
                {
                    CycleOutputKeySO requiredKeySO = requiredKeys[keyIndex];
                    if (configuredKeys.Contains(requiredKeySO))
                    {
                        continue;
                    }

                    errorMessage =
                        $"Applicator '{applicator.GetType().Name}' requires output key " +
                        $"'{requiredKeySO.name}', but the resolved profile does not define it.";
                    return false;
                }
            }

            errorMessage = null;
            return true;
        }

        #endregion
    }
}
