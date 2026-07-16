using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Applies time-driven fog state to Unity RenderSettings and restores the previous global state on teardown.
    /// </summary>
    [DefaultExecutionOrder(TimeOfCycleExecutionOrder.ENVIRONMENT_APPLICATOR)]
    public sealed class FogRenderSettingsOutputApplicator : MonoBehaviour, ITimeOfCycleOutputApplicator
    {
        [SerializeField] private string _enabledOutputId = DayNightEnvironmentOutputIds.FOG_ENABLED;
        [SerializeField] private string _modeOutputId = DayNightEnvironmentOutputIds.FOG_MODE;
        [SerializeField] private string _colorOutputId = DayNightEnvironmentOutputIds.FOG_COLOR;
        [SerializeField] private string _densityOutputId = DayNightEnvironmentOutputIds.FOG_DENSITY;
        [SerializeField]
        private string _startDistanceOutputId = DayNightEnvironmentOutputIds.FOG_START_DISTANCE_METERS;
        [SerializeField]
        private string _endDistanceOutputId = DayNightEnvironmentOutputIds.FOG_END_DISTANCE_METERS;

        private bool _wasFogEnabled;
        private FogMode _previousFogMode;
        private Color _previousFogColor;
        private float _previousFogDensity;
        private float _previousFogStartDistanceMeters;
        private float _previousFogEndDistanceMeters;

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
            if (outputState.TryGetValue(new CycleOutputId(_enabledOutputId), out bool isEnabled))
            {
                RenderSettings.fog = isEnabled;
            }

            if (outputState.TryGetValue(new CycleOutputId(_modeOutputId), out int fogMode))
            {
                RenderSettings.fogMode = (FogMode)Mathf.Clamp(fogMode, (int)FogMode.Linear, (int)FogMode.ExponentialSquared);
            }

            if (outputState.TryGetValue(new CycleOutputId(_colorOutputId), out Color color))
            {
                RenderSettings.fogColor = color;
            }

            if (outputState.TryGetValue(new CycleOutputId(_densityOutputId), out float density))
            {
                RenderSettings.fogDensity = Mathf.Max(0f, density);
            }

            float startDistanceMeters = RenderSettings.fogStartDistance;
            float endDistanceMeters = RenderSettings.fogEndDistance;
            if (outputState.TryGetValue(new CycleOutputId(_startDistanceOutputId), out float configuredStartMeters))
            {
                startDistanceMeters = Mathf.Max(0f, configuredStartMeters);
            }

            if (outputState.TryGetValue(new CycleOutputId(_endDistanceOutputId), out float configuredEndMeters))
            {
                endDistanceMeters = Mathf.Max(0f, configuredEndMeters);
            }

            RenderSettings.fogStartDistance = Mathf.Min(startDistanceMeters, endDistanceMeters);
            RenderSettings.fogEndDistance = Mathf.Max(startDistanceMeters, endDistanceMeters);
        }

        #endregion
    }
}
