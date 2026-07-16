using System.Collections.Generic;
using FakeMG.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Applies supported ambient-light modes and colors while preserving the previous RenderSettings state.
    /// </summary>
    [DefaultExecutionOrder(TimeOfCycleExecutionOrder.ENVIRONMENT_APPLICATOR)]
    public sealed class AmbientRenderSettingsOutputApplicator : MonoBehaviour, ITimeOfCycleOutputApplicator
    {
        [SerializeField] private DayNightEnvironmentOutputSchemaSO _outputSchemaSO;

        private AmbientMode _previousAmbientMode;
        private float _previousAmbientIntensity;
        private Color _previousAmbientFlatColor;
        private Color _previousAmbientSkyColor;
        private Color _previousAmbientEquatorColor;
        private Color _previousAmbientGroundColor;
        private bool _hasLoggedUnsupportedCustomMode;

        public IReadOnlyList<CycleOutputKeySO> RequiredOutputKeys => _outputSchemaSO.AmbientKeys;

        #region Unity Lifecycle

        private void Awake()
        {
            _previousAmbientMode = RenderSettings.ambientMode;
            _previousAmbientIntensity = RenderSettings.ambientIntensity;
            _previousAmbientFlatColor = RenderSettings.ambientLight;
            _previousAmbientSkyColor = RenderSettings.ambientSkyColor;
            _previousAmbientEquatorColor = RenderSettings.ambientEquatorColor;
            _previousAmbientGroundColor = RenderSettings.ambientGroundColor;
        }

        private void OnDestroy()
        {
            RenderSettings.ambientMode = _previousAmbientMode;
            RenderSettings.ambientIntensity = _previousAmbientIntensity;
            RenderSettings.ambientLight = _previousAmbientFlatColor;
            RenderSettings.ambientSkyColor = _previousAmbientSkyColor;
            RenderSettings.ambientEquatorColor = _previousAmbientEquatorColor;
            RenderSettings.ambientGroundColor = _previousAmbientGroundColor;
        }

        #endregion

        #region Public Methods

        public void Apply(IReadOnlyCycleOutputState outputState)
        {
            ApplyMode(outputState);

            if (outputState.TryGetValue(_outputSchemaSO.AmbientIntensity, out float intensity))
            {
                RenderSettings.ambientIntensity = Mathf.Max(0f, intensity);
            }

            if (outputState.TryGetValue(_outputSchemaSO.AmbientFlatColor, out Color flatColor))
            {
                RenderSettings.ambientLight = flatColor;
            }

            if (outputState.TryGetValue(_outputSchemaSO.AmbientSkyColor, out Color skyColor))
            {
                RenderSettings.ambientSkyColor = skyColor;
            }

            if (outputState.TryGetValue(_outputSchemaSO.AmbientEquatorColor, out Color equatorColor))
            {
                RenderSettings.ambientEquatorColor = equatorColor;
            }

            if (outputState.TryGetValue(_outputSchemaSO.AmbientGroundColor, out Color groundColor))
            {
                RenderSettings.ambientGroundColor = groundColor;
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(DayNightEnvironmentOutputSchemaSO outputSchemaSO)
        {
            _outputSchemaSO = outputSchemaSO;
        }

        public void SetOutputSchemaForEditor(DayNightEnvironmentOutputSchemaSO outputSchemaSO)
        {
            _outputSchemaSO = outputSchemaSO;
        }
#endif

        #endregion

        #region Private Methods

        private void ApplyMode(IReadOnlyCycleOutputState outputState)
        {
            if (!outputState.TryGetValue(_outputSchemaSO.AmbientMode, out int ambientModeValue))
            {
                return;
            }

            AmbientMode ambientMode = (AmbientMode)ambientModeValue;
            if (ambientMode == AmbientMode.Custom)
            {
                if (!_hasLoggedUnsupportedCustomMode)
                {
                    Echo.Warning(
                        "Custom spherical-harmonics ambient mode is not supported by the day-night applicator.",
                        context: this);
                    _hasLoggedUnsupportedCustomMode = true;
                }

                return;
            }

            if (ambientMode != AmbientMode.Skybox
                && ambientMode != AmbientMode.Trilight
                && ambientMode != AmbientMode.Flat)
            {
                Echo.Warning($"Ambient mode value {ambientModeValue} is invalid and was ignored.", context: this);
                return;
            }

            _hasLoggedUnsupportedCustomMode = false;
            RenderSettings.ambientMode = ambientMode;
        }

        #endregion
    }
}
