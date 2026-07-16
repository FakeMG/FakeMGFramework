using System.Collections.Generic;
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
        [SerializeField] private DayNightEnvironmentOutputSchemaSO _outputSchemaSO;

        private Light _previousSun;
        private Quaternion _previousRotation;
        private Color _previousColor;
        private float _previousIntensity;
        private float _previousShadowStrength;
        private bool _wasEnabled;

        public IReadOnlyList<CycleOutputKeySO> RequiredOutputKeys => _outputSchemaSO.MainLightKeys;

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
            if (outputState.TryGetValue(_outputSchemaSO.MainLightEnabled, out bool isEnabled))
            {
                _directionalLight.enabled = isEnabled;
            }

            if (outputState.TryGetValue(_outputSchemaSO.MainLightRotation, out Quaternion rotation))
            {
                _directionalLight.transform.rotation = rotation;
            }

            if (outputState.TryGetValue(_outputSchemaSO.MainLightColor, out Color color))
            {
                _directionalLight.color = color;
            }

            if (outputState.TryGetValue(_outputSchemaSO.MainLightIntensity, out float intensity))
            {
                _directionalLight.intensity = Mathf.Max(0f, intensity);
            }

            if (outputState.TryGetValue(
                    _outputSchemaSO.MainLightShadowStrength,
                    out float shadowStrength01))
            {
                _directionalLight.shadowStrength = Mathf.Clamp01(shadowStrength01);
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            Light directionalLight,
            DayNightEnvironmentOutputSchemaSO outputSchemaSO)
        {
            _directionalLight = directionalLight;
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
