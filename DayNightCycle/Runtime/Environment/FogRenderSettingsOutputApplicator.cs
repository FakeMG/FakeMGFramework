using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Applies time-driven fog state to Unity RenderSettings and restores the previous global state on teardown.
    /// </summary>
    [DefaultExecutionOrder(TimeOfCycleExecutionOrder.ENVIRONMENT_APPLICATOR)]
    public sealed class FogRenderSettingsOutputApplicator : MonoBehaviour, ITimeOfCycleOutputApplicator
    {
        [SerializeField] private DayNightEnvironmentOutputSchemaSO _outputSchemaSO;

        private bool _wasFogEnabled;
        private FogMode _previousFogMode;
        private Color _previousFogColor;
        private float _previousFogDensity;
        private float _previousFogStartDistanceMeters;
        private float _previousFogEndDistanceMeters;

        public IReadOnlyList<CycleOutputKeySO> RequiredOutputKeys => _outputSchemaSO.FogKeys;

        #region Unity Lifecycle

        private void Awake()
        {
            _wasFogEnabled = RenderSettings.fog;
            _previousFogMode = RenderSettings.fogMode;
            _previousFogColor = RenderSettings.fogColor;
            _previousFogDensity = RenderSettings.fogDensity;
            _previousFogStartDistanceMeters = RenderSettings.fogStartDistance;
            _previousFogEndDistanceMeters = RenderSettings.fogEndDistance;
        }

        private void OnDestroy()
        {
            RenderSettings.fog = _wasFogEnabled;
            RenderSettings.fogMode = _previousFogMode;
            RenderSettings.fogColor = _previousFogColor;
            RenderSettings.fogDensity = _previousFogDensity;
            RenderSettings.fogStartDistance = _previousFogStartDistanceMeters;
            RenderSettings.fogEndDistance = _previousFogEndDistanceMeters;
        }

        #endregion

        #region Public Methods

        public void Apply(IReadOnlyCycleOutputState outputState)
        {
            if (outputState.TryGetValue(_outputSchemaSO.FogEnabled, out bool isEnabled))
            {
                RenderSettings.fog = isEnabled;
            }

            if (outputState.TryGetValue(_outputSchemaSO.FogMode, out int fogMode))
            {
                RenderSettings.fogMode = (FogMode)Mathf.Clamp(fogMode, (int)FogMode.Linear, (int)FogMode.ExponentialSquared);
            }

            if (outputState.TryGetValue(_outputSchemaSO.FogColor, out Color color))
            {
                RenderSettings.fogColor = color;
            }

            if (outputState.TryGetValue(_outputSchemaSO.FogDensity, out float density))
            {
                RenderSettings.fogDensity = Mathf.Max(0f, density);
            }

            float startDistanceMeters = RenderSettings.fogStartDistance;
            float endDistanceMeters = RenderSettings.fogEndDistance;
            if (outputState.TryGetValue(_outputSchemaSO.FogStartDistanceMeters, out float configuredStartMeters))
            {
                startDistanceMeters = Mathf.Max(0f, configuredStartMeters);
            }

            if (outputState.TryGetValue(_outputSchemaSO.FogEndDistanceMeters, out float configuredEndMeters))
            {
                endDistanceMeters = Mathf.Max(0f, configuredEndMeters);
            }

            RenderSettings.fogStartDistance = Mathf.Min(startDistanceMeters, endDistanceMeters);
            RenderSettings.fogEndDistance = Mathf.Max(startDistanceMeters, endDistanceMeters);
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
    }
}
