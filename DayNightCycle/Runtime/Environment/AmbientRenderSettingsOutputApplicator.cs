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
        [SerializeField] private string _modeOutputId = DayNightEnvironmentOutputIds.AMBIENT_MODE;
        [SerializeField] private string _intensityOutputId = DayNightEnvironmentOutputIds.AMBIENT_INTENSITY;
        [SerializeField] private string _flatColorOutputId = DayNightEnvironmentOutputIds.AMBIENT_FLAT_COLOR;
        [SerializeField] private string _skyColorOutputId = DayNightEnvironmentOutputIds.AMBIENT_SKY_COLOR;
        [SerializeField] private string _equatorColorOutputId = DayNightEnvironmentOutputIds.AMBIENT_EQUATOR_COLOR;
        [SerializeField] private string _groundColorOutputId = DayNightEnvironmentOutputIds.AMBIENT_GROUND_COLOR;

        private AmbientMode _previousAmbientMode;
        private float _previousAmbientIntensity;
        private Color _previousAmbientFlatColor;
        private Color _previousAmbientSkyColor;
        private Color _previousAmbientEquatorColor;
        private Color _previousAmbientGroundColor;
        private bool _hasLoggedUnsupportedCustomMode;

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

            if (outputState.TryGetValue(new CycleOutputId(_intensityOutputId), out float intensity))
            {
                RenderSettings.ambientIntensity = Mathf.Max(0f, intensity);
            }

            if (outputState.TryGetValue(new CycleOutputId(_flatColorOutputId), out Color flatColor))
            {
                RenderSettings.ambientLight = flatColor;
            }

            if (outputState.TryGetValue(new CycleOutputId(_skyColorOutputId), out Color skyColor))
            {
                RenderSettings.ambientSkyColor = skyColor;
            }

            if (outputState.TryGetValue(new CycleOutputId(_equatorColorOutputId), out Color equatorColor))
            {
                RenderSettings.ambientEquatorColor = equatorColor;
            }

            if (outputState.TryGetValue(new CycleOutputId(_groundColorOutputId), out Color groundColor))
            {
                RenderSettings.ambientGroundColor = groundColor;
            }
        }

        #endregion

        #region Private Methods

        private void ApplyMode(IReadOnlyCycleOutputState outputState)
        {
            if (!outputState.TryGetValue(new CycleOutputId(_modeOutputId), out int ambientModeValue))
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
