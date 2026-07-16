using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Defines one profile-owned value evaluated from presentation time.
    /// </summary>
    [Serializable]
    public abstract class CycleOutputDefinition
    {
        [SerializeField] private CycleOutputKeySO _outputKeySO;
        [FormerlySerializedAs("_outputId")]
        [SerializeField, HideInInspector] private string _legacyOutputId;
        [SerializeField] private float _profileChangeTransitionDurationSeconds = 1f;

        public CycleOutputKeySO OutputKeySO => _outputKeySO;
        public float ProfileChangeTransitionDurationSeconds => _profileChangeTransitionDurationSeconds;
        public abstract Type ValueType { get; }

#if UNITY_EDITOR
        public string LegacyOutputId => _legacyOutputId;

        public void ConfigureOutputKeyForEditor(CycleOutputKeySO outputKeySO)
        {
            _outputKeySO = outputKeySO;
            _legacyOutputId = string.Empty;
        }
#endif

        protected CycleOutputDefinition()
        {
        }

        protected CycleOutputDefinition(
            CycleOutputKeySO outputKeySO,
            float profileChangeTransitionDurationSeconds)
        {
            _outputKeySO = outputKeySO;
            _profileChangeTransitionDurationSeconds = profileChangeTransitionDurationSeconds;
        }

        #region Public Methods

        public abstract ICycleOutputEvaluator CreateEvaluator(
            double cycleDurationSeconds,
            IReadOnlyList<CyclePeriodDefinition> periods);

        public abstract object InterpolateProfileValue(
            object previousValue,
            object destinationValue,
            float progress01);

        public virtual bool TryValidate(
            double cycleDurationSeconds,
            ISet<CyclePeriodId> periodIds,
            out string errorMessage)
        {
            if (_outputKeySO == null)
            {
                errorMessage = "An output key asset is required.";
                return false;
            }

            if (_outputKeySO.ValueType != ValueType)
            {
                errorMessage =
                    $"Output key '{_outputKeySO.name}' expects {_outputKeySO.ValueType.Name}, " +
                    $"but the definition produces {ValueType.Name}.";
                return false;
            }

            if (!CycleNumericValidation.IsFiniteNonNegative(_profileChangeTransitionDurationSeconds))
            {
                errorMessage = "Profile transition duration must be finite and non-negative.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        #endregion
    }
}
