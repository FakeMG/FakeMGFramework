using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Owns the compiler-checked output-key contract shared by the default profile and environment applicators.
    /// </summary>
    [CreateAssetMenu(menuName = "FakeMG/Day Night Cycle/Environment Output Schema")]
    public sealed class DayNightEnvironmentOutputSchemaSO : ScriptableObject
    {
        [Header("Main Light")]
        [SerializeField] private BoolCycleOutputKeySO _mainLightEnabled;
        [SerializeField] private RotationCycleOutputKeySO _mainLightRotation;
        [SerializeField] private ColorCycleOutputKeySO _mainLightColor;
        [SerializeField] private FloatCycleOutputKeySO _mainLightIntensity;
        [SerializeField] private FloatCycleOutputKeySO _mainLightShadowStrength;

        [Header("Sky")]
        [SerializeField] private IntCycleOutputKeySO _skySunDisk;
        [SerializeField] private FloatCycleOutputKeySO _skySunSize;
        [SerializeField] private FloatCycleOutputKeySO _skySunSizeConvergence;
        [SerializeField] private FloatCycleOutputKeySO _skyAtmosphereThickness;
        [SerializeField] private ColorCycleOutputKeySO _skyTint;
        [SerializeField] private ColorCycleOutputKeySO _skyGroundColor;
        [SerializeField] private FloatCycleOutputKeySO _skyExposure;

        [Header("Fog")]
        [SerializeField] private BoolCycleOutputKeySO _fogEnabled;
        [SerializeField] private IntCycleOutputKeySO _fogMode;
        [SerializeField] private ColorCycleOutputKeySO _fogColor;
        [SerializeField] private FloatCycleOutputKeySO _fogDensity;
        [SerializeField] private FloatCycleOutputKeySO _fogStartDistanceMeters;
        [SerializeField] private FloatCycleOutputKeySO _fogEndDistanceMeters;

        [Header("Ambient")]
        [SerializeField] private IntCycleOutputKeySO _ambientMode;
        [SerializeField] private FloatCycleOutputKeySO _ambientIntensity;
        [SerializeField] private ColorCycleOutputKeySO _ambientFlatColor;
        [SerializeField] private ColorCycleOutputKeySO _ambientSkyColor;
        [SerializeField] private ColorCycleOutputKeySO _ambientEquatorColor;
        [SerializeField] private ColorCycleOutputKeySO _ambientGroundColor;

        public BoolCycleOutputKeySO MainLightEnabled => _mainLightEnabled;
        public RotationCycleOutputKeySO MainLightRotation => _mainLightRotation;
        public ColorCycleOutputKeySO MainLightColor => _mainLightColor;
        public FloatCycleOutputKeySO MainLightIntensity => _mainLightIntensity;
        public FloatCycleOutputKeySO MainLightShadowStrength => _mainLightShadowStrength;
        public IntCycleOutputKeySO SkySunDisk => _skySunDisk;
        public FloatCycleOutputKeySO SkySunSize => _skySunSize;
        public FloatCycleOutputKeySO SkySunSizeConvergence => _skySunSizeConvergence;
        public FloatCycleOutputKeySO SkyAtmosphereThickness => _skyAtmosphereThickness;
        public ColorCycleOutputKeySO SkyTint => _skyTint;
        public ColorCycleOutputKeySO SkyGroundColor => _skyGroundColor;
        public FloatCycleOutputKeySO SkyExposure => _skyExposure;
        public BoolCycleOutputKeySO FogEnabled => _fogEnabled;
        public IntCycleOutputKeySO FogMode => _fogMode;
        public ColorCycleOutputKeySO FogColor => _fogColor;
        public FloatCycleOutputKeySO FogDensity => _fogDensity;
        public FloatCycleOutputKeySO FogStartDistanceMeters => _fogStartDistanceMeters;
        public FloatCycleOutputKeySO FogEndDistanceMeters => _fogEndDistanceMeters;
        public IntCycleOutputKeySO AmbientMode => _ambientMode;
        public FloatCycleOutputKeySO AmbientIntensity => _ambientIntensity;
        public ColorCycleOutputKeySO AmbientFlatColor => _ambientFlatColor;
        public ColorCycleOutputKeySO AmbientSkyColor => _ambientSkyColor;
        public ColorCycleOutputKeySO AmbientEquatorColor => _ambientEquatorColor;
        public ColorCycleOutputKeySO AmbientGroundColor => _ambientGroundColor;

        public IReadOnlyList<CycleOutputKeySO> MainLightKeys => new CycleOutputKeySO[]
        {
            _mainLightEnabled, _mainLightRotation, _mainLightColor, _mainLightIntensity, _mainLightShadowStrength,
        };

        public IReadOnlyList<CycleOutputKeySO> SkyKeys => new CycleOutputKeySO[]
        {
            _skySunDisk, _skySunSize, _skySunSizeConvergence, _skyAtmosphereThickness,
            _skyTint, _skyGroundColor, _skyExposure,
        };

        public IReadOnlyList<CycleOutputKeySO> FogKeys => new CycleOutputKeySO[]
        {
            _fogEnabled, _fogMode, _fogColor, _fogDensity, _fogStartDistanceMeters, _fogEndDistanceMeters,
        };

        public IReadOnlyList<CycleOutputKeySO> AmbientKeys => new CycleOutputKeySO[]
        {
            _ambientMode, _ambientIntensity, _ambientFlatColor, _ambientSkyColor,
            _ambientEquatorColor, _ambientGroundColor,
        };

#if UNITY_EDITOR
        #region Public Methods

        public void ConfigureForEditor(
            BoolCycleOutputKeySO mainLightEnabled,
            RotationCycleOutputKeySO mainLightRotation,
            ColorCycleOutputKeySO mainLightColor,
            FloatCycleOutputKeySO mainLightIntensity,
            FloatCycleOutputKeySO mainLightShadowStrength,
            IntCycleOutputKeySO skySunDisk,
            FloatCycleOutputKeySO skySunSize,
            FloatCycleOutputKeySO skySunSizeConvergence,
            FloatCycleOutputKeySO skyAtmosphereThickness,
            ColorCycleOutputKeySO skyTint,
            ColorCycleOutputKeySO skyGroundColor,
            FloatCycleOutputKeySO skyExposure,
            BoolCycleOutputKeySO fogEnabled,
            IntCycleOutputKeySO fogMode,
            ColorCycleOutputKeySO fogColor,
            FloatCycleOutputKeySO fogDensity,
            FloatCycleOutputKeySO fogStartDistanceMeters,
            FloatCycleOutputKeySO fogEndDistanceMeters,
            IntCycleOutputKeySO ambientMode,
            FloatCycleOutputKeySO ambientIntensity,
            ColorCycleOutputKeySO ambientFlatColor,
            ColorCycleOutputKeySO ambientSkyColor,
            ColorCycleOutputKeySO ambientEquatorColor,
            ColorCycleOutputKeySO ambientGroundColor)
        {
            _mainLightEnabled = mainLightEnabled;
            _mainLightRotation = mainLightRotation;
            _mainLightColor = mainLightColor;
            _mainLightIntensity = mainLightIntensity;
            _mainLightShadowStrength = mainLightShadowStrength;
            _skySunDisk = skySunDisk;
            _skySunSize = skySunSize;
            _skySunSizeConvergence = skySunSizeConvergence;
            _skyAtmosphereThickness = skyAtmosphereThickness;
            _skyTint = skyTint;
            _skyGroundColor = skyGroundColor;
            _skyExposure = skyExposure;
            _fogEnabled = fogEnabled;
            _fogMode = fogMode;
            _fogColor = fogColor;
            _fogDensity = fogDensity;
            _fogStartDistanceMeters = fogStartDistanceMeters;
            _fogEndDistanceMeters = fogEndDistanceMeters;
            _ambientMode = ambientMode;
            _ambientIntensity = ambientIntensity;
            _ambientFlatColor = ambientFlatColor;
            _ambientSkyColor = ambientSkyColor;
            _ambientEquatorColor = ambientEquatorColor;
            _ambientGroundColor = ambientGroundColor;
        }

        #endregion
#endif
    }
}
