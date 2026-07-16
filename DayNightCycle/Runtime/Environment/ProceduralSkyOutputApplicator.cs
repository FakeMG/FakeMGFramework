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
        [SerializeField] private string _sunDiskOutputId = DayNightEnvironmentOutputIds.SKY_SUN_DISK;
        [SerializeField] private string _sunSizeOutputId = DayNightEnvironmentOutputIds.SKY_SUN_SIZE;
        [SerializeField]
        private string _sunSizeConvergenceOutputId = DayNightEnvironmentOutputIds.SKY_SUN_SIZE_CONVERGENCE;
        [SerializeField]
        private string _atmosphereThicknessOutputId = DayNightEnvironmentOutputIds.SKY_ATMOSPHERE_THICKNESS;
        [SerializeField] private string _skyTintOutputId = DayNightEnvironmentOutputIds.SKY_TINT;
        [SerializeField] private string _groundColorOutputId = DayNightEnvironmentOutputIds.SKY_GROUND_COLOR;
        [SerializeField] private string _exposureOutputId = DayNightEnvironmentOutputIds.SKY_EXPOSURE;

        private Material _previousSkyboxMaterial;
        private Material _runtimeSkyMaterial;

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
            if (outputState.TryGetValue(new CycleOutputId(_sunDiskOutputId), out int sunDiskMode))
            {
                _runtimeSkyMaterial.SetInt(SUN_DISK_PROPERTY_ID, Mathf.Clamp(sunDiskMode, 0, 2));
            }

            ApplyFloat(outputState, _sunSizeOutputId, SUN_SIZE_PROPERTY_ID, 0f, 1f);
            ApplyFloat(outputState, _sunSizeConvergenceOutputId, SUN_SIZE_CONVERGENCE_PROPERTY_ID, 0f, 20f);
            ApplyFloat(outputState, _atmosphereThicknessOutputId, ATMOSPHERE_THICKNESS_PROPERTY_ID, 0f, 5f);
            ApplyColor(outputState, _skyTintOutputId, SKY_TINT_PROPERTY_ID);
            ApplyColor(outputState, _groundColorOutputId, GROUND_COLOR_PROPERTY_ID);
            ApplyFloat(outputState, _exposureOutputId, EXPOSURE_PROPERTY_ID, 0f, 8f);
            _cubemapRefreshController.RequestRefresh();
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            Material proceduralSkyMaterialTemplate,
            EnvironmentCubemapRefreshController cubemapRefreshController)
        {
            _proceduralSkyMaterialTemplate = proceduralSkyMaterialTemplate;
            _cubemapRefreshController = cubemapRefreshController;
        }
#endif

        #endregion

        #region Private Methods

        private void ApplyFloat(
            IReadOnlyCycleOutputState outputState,
            string outputId,
            int propertyId,
            float minimumValue,
            float maximumValue)
        {
            if (outputState.TryGetValue(new CycleOutputId(outputId), out float value))
            {
                _runtimeSkyMaterial.SetFloat(propertyId, Mathf.Clamp(value, minimumValue, maximumValue));
            }
        }

        private void ApplyColor(
            IReadOnlyCycleOutputState outputState,
            string outputId,
            int propertyId)
        {
            if (outputState.TryGetValue(new CycleOutputId(outputId), out Color value))
            {
                _runtimeSkyMaterial.SetColor(propertyId, value);
            }
        }

        #endregion
    }
}
