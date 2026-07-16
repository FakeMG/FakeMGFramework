using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Applies supported procedural-sky outputs to a runtime material clone without mutating its source asset.
    /// </summary>
    [DefaultExecutionOrder(TimeOfCycleExecutionOrder.ENVIRONMENT_APPLICATOR)]
    public sealed class ProceduralSkyOutputApplicator : MonoBehaviour, ITimeOfCycleOutputApplicator
    {
        private static readonly int SUN_DISK_PROPERTY_ID = Shader.PropertyToID("_SunDisk");
        private static readonly int SUN_SIZE_PROPERTY_ID = Shader.PropertyToID("_SunSize");
        private static readonly int SUN_SIZE_CONVERGENCE_PROPERTY_ID = Shader.PropertyToID("_SunSizeConvergence");
        private static readonly int ATMOSPHERE_THICKNESS_PROPERTY_ID = Shader.PropertyToID("_AtmosphereThickness");
        private static readonly int SKY_TINT_PROPERTY_ID = Shader.PropertyToID("_SkyTint");
        private static readonly int GROUND_COLOR_PROPERTY_ID = Shader.PropertyToID("_GroundColor");
        private static readonly int EXPOSURE_PROPERTY_ID = Shader.PropertyToID("_Exposure");

        [SerializeField] private Material _proceduralSkyMaterialTemplate;
        [SerializeField] private EnvironmentCubemapRefreshController _cubemapRefreshController;
        [SerializeField] private DayNightEnvironmentOutputSchemaSO _outputSchemaSO;

        private Material _previousSkyboxMaterial;
        private Material _runtimeSkyMaterial;

        public IReadOnlyList<CycleOutputKeySO> RequiredOutputKeys => _outputSchemaSO.SkyKeys;

        #region Unity Lifecycle

        private void Awake()
        {
            _previousSkyboxMaterial = RenderSettings.skybox;
            _runtimeSkyMaterial = new Material(_proceduralSkyMaterialTemplate)
            {
                name = $"{_proceduralSkyMaterialTemplate.name} Runtime",
            };
            RenderSettings.skybox = _runtimeSkyMaterial;
            _cubemapRefreshController.RequestRefresh();
        }

        private void OnDestroy()
        {
            RenderSettings.skybox = _previousSkyboxMaterial;
            _cubemapRefreshController.RequestRefresh();

            if (Application.isPlaying)
            {
                Destroy(_runtimeSkyMaterial);
            }
            else
            {
                DestroyImmediate(_runtimeSkyMaterial);
            }
        }

        #endregion

        #region Public Methods

        public void Apply(IReadOnlyCycleOutputState outputState)
        {
            if (outputState.TryGetValue(_outputSchemaSO.SkySunDisk, out int sunDiskMode))
            {
                _runtimeSkyMaterial.SetInt(SUN_DISK_PROPERTY_ID, Mathf.Clamp(sunDiskMode, 0, 2));
            }

            ApplyFloat(outputState, _outputSchemaSO.SkySunSize, SUN_SIZE_PROPERTY_ID, 0f, 1f);
            ApplyFloat(outputState, _outputSchemaSO.SkySunSizeConvergence, SUN_SIZE_CONVERGENCE_PROPERTY_ID, 0f, 20f);
            ApplyFloat(outputState, _outputSchemaSO.SkyAtmosphereThickness, ATMOSPHERE_THICKNESS_PROPERTY_ID, 0f, 5f);
            ApplyColor(outputState, _outputSchemaSO.SkyTint, SKY_TINT_PROPERTY_ID);
            ApplyColor(outputState, _outputSchemaSO.SkyGroundColor, GROUND_COLOR_PROPERTY_ID);
            ApplyFloat(outputState, _outputSchemaSO.SkyExposure, EXPOSURE_PROPERTY_ID, 0f, 8f);
            _cubemapRefreshController.RequestRefresh();
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            Material proceduralSkyMaterialTemplate,
            EnvironmentCubemapRefreshController cubemapRefreshController,
            DayNightEnvironmentOutputSchemaSO outputSchemaSO)
        {
            _proceduralSkyMaterialTemplate = proceduralSkyMaterialTemplate;
            _cubemapRefreshController = cubemapRefreshController;
            _outputSchemaSO = outputSchemaSO;
        }

        public void SetOutputSchemaForEditor(DayNightEnvironmentOutputSchemaSO outputSchemaSO)
        {
            _outputSchemaSO = outputSchemaSO;
        }
#endif

        #endregion

        #region Private Methods

        private void ApplyFloat(
            IReadOnlyCycleOutputState outputState,
            FloatCycleOutputKeySO outputKeySO,
            int propertyId,
            float minimumValue,
            float maximumValue)
        {
            if (outputState.TryGetValue(outputKeySO, out float value))
            {
                _runtimeSkyMaterial.SetFloat(propertyId, Mathf.Clamp(value, minimumValue, maximumValue));
            }
        }

        private void ApplyColor(
            IReadOnlyCycleOutputState outputState,
            ColorCycleOutputKeySO outputKeySO,
            int propertyId)
        {
            if (outputState.TryGetValue(outputKeySO, out Color value))
            {
                _runtimeSkyMaterial.SetColor(propertyId, value);
            }
        }

        #endregion
    }
}
