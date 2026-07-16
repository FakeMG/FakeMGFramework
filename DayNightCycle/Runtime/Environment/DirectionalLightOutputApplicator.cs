using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Applies configured time outputs to the primary directional light and RenderSettings sun reference.
    /// </summary>
    [DefaultExecutionOrder(TimeOfCycleExecutionOrder.ENVIRONMENT_APPLICATOR)]
    public sealed class DirectionalLightOutputApplicator : MonoBehaviour, ITimeOfCycleOutputApplicator
    {
        [SerializeField] private Light _directionalLight;
        [SerializeField] private string _enabledOutputId = DayNightEnvironmentOutputIds.MAIN_LIGHT_ENABLED;
        [SerializeField] private string _rotationOutputId = DayNightEnvironmentOutputIds.MAIN_LIGHT_ROTATION;
        [SerializeField] private string _colorOutputId = DayNightEnvironmentOutputIds.MAIN_LIGHT_COLOR;
        [SerializeField] private string _intensityOutputId = DayNightEnvironmentOutputIds.MAIN_LIGHT_INTENSITY;
        [SerializeField]
        private string _shadowStrengthOutputId = DayNightEnvironmentOutputIds.MAIN_LIGHT_SHADOW_STRENGTH;

        private Light _previousSun;
        private Quaternion _previousRotation;
        private Color _previousColor;
        private float _previousIntensity;
        private float _previousShadowStrength;
        private bool _wasEnabled;

        #region Unity Lifecycle

        private void Awake()
        {
            _previousSun = RenderSettings.sun;
            _previousRotation = _directionalLight.transform.rotation;
            _previousColor = _directionalLight.color;
            _previousIntensity = _directionalLight.intensity;
            _previousShadowStrength = _directionalLight.shadowStrength;
            _wasEnabled = _directionalLight.enabled;
            RenderSettings.sun = _directionalLight;
        }

        private void OnDestroy()
        {
            RenderSettings.sun = _previousSun;
            _directionalLight.transform.rotation = _previousRotation;
            _directionalLight.color = _previousColor;
            _directionalLight.intensity = _previousIntensity;
            _directionalLight.shadowStrength = _previousShadowStrength;
            _directionalLight.enabled = _wasEnabled;
        }

        #endregion

        #region Public Methods

        public void Apply(IReadOnlyCycleOutputState outputState)
        {
            if (outputState.TryGetValue(new CycleOutputId(_enabledOutputId), out bool isEnabled))
            {
                _directionalLight.enabled = isEnabled;
            }

            if (outputState.TryGetValue(new CycleOutputId(_rotationOutputId), out Quaternion rotation))
            {
                _directionalLight.transform.rotation = rotation;
            }

            if (outputState.TryGetValue(new CycleOutputId(_colorOutputId), out Color color))
            {
                _directionalLight.color = color;
            }

            if (outputState.TryGetValue(new CycleOutputId(_intensityOutputId), out float intensity))
            {
                _directionalLight.intensity = Mathf.Max(0f, intensity);
            }

            if (outputState.TryGetValue(
                    new CycleOutputId(_shadowStrengthOutputId),
                    out float shadowStrength01))
            {
                _directionalLight.shadowStrength = Mathf.Clamp01(shadowStrength01);
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(Light directionalLight)
        {
            _directionalLight = directionalLight;
        }
#endif

        #endregion
    }
}
