#if UNITY_EDITOR
using System.Collections.Generic;
using FakeMG.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FakeMG.DayNightCycle.Editor
{
    /// <summary>
    /// Creates or refreshes the reusable default profile, procedural-sky material, and standalone prefab.
    /// </summary>
    public static class DefaultDayNightCycleAssetCreator
    {
        private const string FEATURE_ROOT_PATH =
            "Assets/Thirdparty/FakeMGFramework/DayNightCycle";
        private const string PROFILE_FOLDER_PATH = FEATURE_ROOT_PATH + "/Profiles";
        private const string MATERIAL_FOLDER_PATH = FEATURE_ROOT_PATH + "/Materials";
        private const string PREFAB_FOLDER_PATH = FEATURE_ROOT_PATH + "/Prefabs";
        private const string OUTPUT_KEY_FOLDER_PATH = FEATURE_ROOT_PATH + "/Output Keys";
        private const string PROFILE_PATH = PROFILE_FOLDER_PATH + "/Default Day Night Cycle Profile.asset";
        private const string OUTPUT_SCHEMA_PATH = OUTPUT_KEY_FOLDER_PATH + "/Default Environment Output Schema.asset";
        private const string MATERIAL_PATH = MATERIAL_FOLDER_PATH + "/Default Procedural Sky.mat";
        private const string PREFAB_PATH = PREFAB_FOLDER_PATH + "/Day Night Cycle.prefab";

        private const double CYCLE_DURATION_SECONDS = 86400d;
        private const double DAWN_START_TIME_SECONDS = 18000d;
        private const double DAY_START_TIME_SECONDS = 25200d;
        private const double DUSK_START_TIME_SECONDS = 64800d;
        private const double NIGHT_START_TIME_SECONDS = 72000d;
        private const float PROFILE_TRANSITION_DURATION_SECONDS = 1f;

        #region Public Methods

        [MenuItem(FakeMGEditorMenus.DAY_NIGHT_CYCLE + "/Create Default Assets")]
        public static void CreateDefaultAssets()
        {
            if (!TryPreflight(out Shader proceduralSkyShader, out string errorMessage))
            {
                Echo.Error($"Cannot create default day-night assets. {errorMessage}");
                return;
            }

            EnsureAssetFoldersExist();
            DayNightEnvironmentOutputSchemaSO outputSchemaSO = CreateOrUpdateOutputSchema();
            Material proceduralSkyMaterial = CreateOrUpdateProceduralSkyMaterial(proceduralSkyShader);
            TimeOfCycleProfileSO profileSO = CreateOrUpdateProfile(outputSchemaSO);
            CreateOrUpdatePrefab(profileSO, proceduralSkyMaterial, outputSchemaSO);
            DayNightCycleLegacyAssetMigration.MigrateProfilesOverridesAndPrefabs();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Echo.Log("Created default day-night cycle profile, material, and prefab.");
        }

        #endregion

        #region Private Methods

        private static void EnsureAssetFoldersExist()
        {
            CreateFolderIfMissing(FEATURE_ROOT_PATH, "Profiles");
            CreateFolderIfMissing(FEATURE_ROOT_PATH, "Materials");
            CreateFolderIfMissing(FEATURE_ROOT_PATH, "Prefabs");
            CreateFolderIfMissing(FEATURE_ROOT_PATH, "Output Keys");
        }

        private static void CreateFolderIfMissing(string parentPath, string folderName)
        {
            string folderPath = parentPath + "/" + folderName;
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder(parentPath, folderName);
            }
        }

        private static bool TryPreflight(out Shader proceduralSkyShader, out string errorMessage)
        {
            proceduralSkyShader = Shader.Find("Skybox/Procedural");
            if (proceduralSkyShader == null)
            {
                errorMessage = "Skybox/Procedural is unavailable.";
                return false;
            }

            (string Path, System.Type ExpectedType)[] requirements =
            {
                (PROFILE_PATH, typeof(TimeOfCycleProfileSO)),
                (MATERIAL_PATH, typeof(Material)),
                (PREFAB_PATH, typeof(GameObject)),
                (OUTPUT_SCHEMA_PATH, typeof(DayNightEnvironmentOutputSchemaSO)),
                (GetOutputKeyPath("Main Light Enabled"), typeof(BoolCycleOutputKeySO)),
                (GetOutputKeyPath("Main Light Rotation"), typeof(RotationCycleOutputKeySO)),
                (GetOutputKeyPath("Main Light Color"), typeof(ColorCycleOutputKeySO)),
                (GetOutputKeyPath("Main Light Intensity"), typeof(FloatCycleOutputKeySO)),
                (GetOutputKeyPath("Main Light Shadow Strength"), typeof(FloatCycleOutputKeySO)),
                (GetOutputKeyPath("Sky Sun Disk"), typeof(IntCycleOutputKeySO)),
                (GetOutputKeyPath("Sky Sun Size"), typeof(FloatCycleOutputKeySO)),
                (GetOutputKeyPath("Sky Sun Size Convergence"), typeof(FloatCycleOutputKeySO)),
                (GetOutputKeyPath("Sky Atmosphere Thickness"), typeof(FloatCycleOutputKeySO)),
                (GetOutputKeyPath("Sky Tint"), typeof(ColorCycleOutputKeySO)),
                (GetOutputKeyPath("Sky Ground Color"), typeof(ColorCycleOutputKeySO)),
                (GetOutputKeyPath("Sky Exposure"), typeof(FloatCycleOutputKeySO)),
                (GetOutputKeyPath("Fog Enabled"), typeof(BoolCycleOutputKeySO)),
                (GetOutputKeyPath("Fog Mode"), typeof(IntCycleOutputKeySO)),
                (GetOutputKeyPath("Fog Color"), typeof(ColorCycleOutputKeySO)),
                (GetOutputKeyPath("Fog Density"), typeof(FloatCycleOutputKeySO)),
                (GetOutputKeyPath("Fog Start Distance Meters"), typeof(FloatCycleOutputKeySO)),
                (GetOutputKeyPath("Fog End Distance Meters"), typeof(FloatCycleOutputKeySO)),
                (GetOutputKeyPath("Ambient Mode"), typeof(IntCycleOutputKeySO)),
                (GetOutputKeyPath("Ambient Intensity"), typeof(FloatCycleOutputKeySO)),
                (GetOutputKeyPath("Ambient Flat Color"), typeof(ColorCycleOutputKeySO)),
                (GetOutputKeyPath("Ambient Sky Color"), typeof(ColorCycleOutputKeySO)),
                (GetOutputKeyPath("Ambient Equator Color"), typeof(ColorCycleOutputKeySO)),
                (GetOutputKeyPath("Ambient Ground Color"), typeof(ColorCycleOutputKeySO)),
            };
            for (int requirementIndex = 0; requirementIndex < requirements.Length; requirementIndex++)
            {
                Object existingAsset = AssetDatabase.LoadMainAssetAtPath(requirements[requirementIndex].Path);
                if (existingAsset == null
                    || requirements[requirementIndex].ExpectedType.IsInstanceOfType(existingAsset))
                {
                    continue;
                }

                errorMessage =
                    $"Asset '{requirements[requirementIndex].Path}' is " +
                    $"{existingAsset.GetType().Name}, expected " +
                    $"{requirements[requirementIndex].ExpectedType.Name}.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        private static Material CreateOrUpdateProceduralSkyMaterial(Shader proceduralSkyShader)
        {

            Material material = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH);
            if (material == null)
            {
                material = new Material(proceduralSkyShader);
                AssetDatabase.CreateAsset(material, MATERIAL_PATH);
            }
            else
            {
                material.shader = proceduralSkyShader;
            }

            material.name = "Default Procedural Sky";
            material.SetInt("_SunDisk", 2);
            material.SetFloat("_SunSize", 0.04f);
            material.SetFloat("_SunSizeConvergence", 5f);
            material.SetFloat("_AtmosphereThickness", 1f);
            material.SetColor("_SkyTint", new Color(0.45f, 0.55f, 0.75f));
            material.SetColor("_GroundColor", new Color(0.3f, 0.28f, 0.25f));
            material.SetFloat("_Exposure", 1.1f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static DayNightEnvironmentOutputSchemaSO CreateOrUpdateOutputSchema()
        {
            DayNightEnvironmentOutputSchemaSO outputSchemaSO =
                AssetDatabase.LoadAssetAtPath<DayNightEnvironmentOutputSchemaSO>(OUTPUT_SCHEMA_PATH);
            if (outputSchemaSO == null)
            {
                outputSchemaSO = ScriptableObject.CreateInstance<DayNightEnvironmentOutputSchemaSO>();
                AssetDatabase.CreateAsset(outputSchemaSO, OUTPUT_SCHEMA_PATH);
            }

            outputSchemaSO.ConfigureForEditor(
                mainLightEnabled: CreateOrLoadOutputKey<BoolCycleOutputKeySO>("Main Light Enabled", "main_light_enabled"),
                mainLightRotation: CreateOrLoadOutputKey<RotationCycleOutputKeySO>("Main Light Rotation", "main_light_rotation"),
                mainLightColor: CreateOrLoadOutputKey<ColorCycleOutputKeySO>("Main Light Color", "main_light_color"),
                mainLightIntensity: CreateOrLoadOutputKey<FloatCycleOutputKeySO>("Main Light Intensity", "main_light_intensity"),
                mainLightShadowStrength: CreateOrLoadOutputKey<FloatCycleOutputKeySO>("Main Light Shadow Strength", "main_light_shadow_strength"),
                skySunDisk: CreateOrLoadOutputKey<IntCycleOutputKeySO>("Sky Sun Disk", "sky_sun_disk"),
                skySunSize: CreateOrLoadOutputKey<FloatCycleOutputKeySO>("Sky Sun Size", "sky_sun_size"),
                skySunSizeConvergence: CreateOrLoadOutputKey<FloatCycleOutputKeySO>("Sky Sun Size Convergence", "sky_sun_size_convergence"),
                skyAtmosphereThickness: CreateOrLoadOutputKey<FloatCycleOutputKeySO>("Sky Atmosphere Thickness", "sky_atmosphere_thickness"),
                skyTint: CreateOrLoadOutputKey<ColorCycleOutputKeySO>("Sky Tint", "sky_tint"),
                skyGroundColor: CreateOrLoadOutputKey<ColorCycleOutputKeySO>("Sky Ground Color", "sky_ground_color"),
                skyExposure: CreateOrLoadOutputKey<FloatCycleOutputKeySO>("Sky Exposure", "sky_exposure"),
                fogEnabled: CreateOrLoadOutputKey<BoolCycleOutputKeySO>("Fog Enabled", "fog_enabled"),
                fogMode: CreateOrLoadOutputKey<IntCycleOutputKeySO>("Fog Mode", "fog_mode"),
                fogColor: CreateOrLoadOutputKey<ColorCycleOutputKeySO>("Fog Color", "fog_color"),
                fogDensity: CreateOrLoadOutputKey<FloatCycleOutputKeySO>("Fog Density", "fog_density"),
                fogStartDistanceMeters: CreateOrLoadOutputKey<FloatCycleOutputKeySO>("Fog Start Distance Meters", "fog_start_distance_meters"),
                fogEndDistanceMeters: CreateOrLoadOutputKey<FloatCycleOutputKeySO>("Fog End Distance Meters", "fog_end_distance_meters"),
                ambientMode: CreateOrLoadOutputKey<IntCycleOutputKeySO>("Ambient Mode", "ambient_mode"),
                ambientIntensity: CreateOrLoadOutputKey<FloatCycleOutputKeySO>("Ambient Intensity", "ambient_intensity"),
                ambientFlatColor: CreateOrLoadOutputKey<ColorCycleOutputKeySO>("Ambient Flat Color", "ambient_flat_color"),
                ambientSkyColor: CreateOrLoadOutputKey<ColorCycleOutputKeySO>("Ambient Sky Color", "ambient_sky_color"),
                ambientEquatorColor: CreateOrLoadOutputKey<ColorCycleOutputKeySO>("Ambient Equator Color", "ambient_equator_color"),
                ambientGroundColor: CreateOrLoadOutputKey<ColorCycleOutputKeySO>("Ambient Ground Color", "ambient_ground_color"));
            EditorUtility.SetDirty(outputSchemaSO);
            return outputSchemaSO;
        }

        private static T CreateOrLoadOutputKey<T>(string assetName, string legacyId)
            where T : CycleOutputKeySO
        {
            string assetPath = GetOutputKeyPath(assetName);
            T outputKeySO = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (outputKeySO != null)
            {
                outputKeySO.ConfigureLegacyIdForEditor(legacyId);
                EditorUtility.SetDirty(outputKeySO);
                return outputKeySO;
            }

            outputKeySO = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(outputKeySO, assetPath);
            outputKeySO.ConfigureLegacyIdForEditor(legacyId);
            EditorUtility.SetDirty(outputKeySO);
            return outputKeySO;
        }

        private static string GetOutputKeyPath(string assetName)
        {
            return OUTPUT_KEY_FOLDER_PATH + "/" + assetName + ".asset";
        }

        private static TimeOfCycleProfileSO CreateOrUpdateProfile(
            DayNightEnvironmentOutputSchemaSO outputSchemaSO)
        {
            TimeOfCycleProfileSO profileSO =
                AssetDatabase.LoadAssetAtPath<TimeOfCycleProfileSO>(PROFILE_PATH);
            if (profileSO == null)
            {
                Object existingAsset = AssetDatabase.LoadMainAssetAtPath(PROFILE_PATH);
                if (existingAsset != null)
                {
                    AssetDatabase.DeleteAsset(PROFILE_PATH);
                }

                profileSO = ScriptableObject.CreateInstance<TimeOfCycleProfileSO>();
                AssetDatabase.CreateAsset(profileSO, PROFILE_PATH);
            }

            profileSO.ConfigureForEditor(
                CYCLE_DURATION_SECONDS,
                28800d,
                60d,
                true,
                1f,
                AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
                CreatePeriods(),
                CreateOutputs(outputSchemaSO));
            EditorUtility.SetDirty(profileSO);
            return profileSO;
        }

        private static IReadOnlyList<CyclePeriodDefinition> CreatePeriods()
        {
            return new List<CyclePeriodDefinition>
            {
                new("dawn", DAWN_START_TIME_SECONDS),
                new("day", DAY_START_TIME_SECONDS),
                new("dusk", DUSK_START_TIME_SECONDS),
                new("night", NIGHT_START_TIME_SECONDS),
            };
        }

        private static IReadOnlyList<CycleOutputDefinition> CreateOutputs(
            DayNightEnvironmentOutputSchemaSO outputSchemaSO)
        {
            AnimationCurve smoothCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            List<CycleOutputDefinition> outputs = new()
            {
                CreateMainLightEnabledOutput(outputSchemaSO),
                new RotationCycleOutputDefinition(
                    outputSchemaSO.MainLightRotation,
                    PROFILE_TRANSITION_DURATION_SECONDS,
                    smoothCurve,
                    new[]
                    {
                        new RotationCyclePoint(0d, new Vector3(-90f, 330f, 0f)),
                        new RotationCyclePoint(21600d, new Vector3(0f, 330f, 0f)),
                        new RotationCyclePoint(43200d, new Vector3(90f, 330f, 0f)),
                        new RotationCyclePoint(64800d, new Vector3(180f, 330f, 0f)),
                    }),
                CreateColorOutput(
                    outputSchemaSO.MainLightColor,
                    new ColorCyclePoint(0d, new Color(0.28f, 0.36f, 0.62f)),
                    new ColorCyclePoint(19800d, new Color(1f, 0.55f, 0.32f)),
                    new ColorCyclePoint(DAY_START_TIME_SECONDS, new Color(1f, 0.96f, 0.84f)),
                    new ColorCyclePoint(DUSK_START_TIME_SECONDS, new Color(1f, 0.42f, 0.2f)),
                    new ColorCyclePoint(NIGHT_START_TIME_SECONDS, new Color(0.28f, 0.36f, 0.62f))),
                CreateFloatOutput(
                    outputSchemaSO.MainLightIntensity,
                    new FloatCyclePoint(0d, 0.04f),
                    new FloatCyclePoint(DAWN_START_TIME_SECONDS, 0.15f),
                    new FloatCyclePoint(DAY_START_TIME_SECONDS, 1f),
                    new FloatCyclePoint(DUSK_START_TIME_SECONDS, 0.25f),
                    new FloatCyclePoint(NIGHT_START_TIME_SECONDS, 0.04f)),
                CreateFloatOutput(
                    outputSchemaSO.MainLightShadowStrength,
                    new FloatCyclePoint(0d, 0.1f),
                    new FloatCyclePoint(DAY_START_TIME_SECONDS, 0.85f),
                    new FloatCyclePoint(DUSK_START_TIME_SECONDS, 0.45f),
                    new FloatCyclePoint(NIGHT_START_TIME_SECONDS, 0.1f)),
                CreateConstantIntOutput(outputSchemaSO.SkySunDisk, 2),
                CreateFloatOutput(
                    outputSchemaSO.SkySunSize,
                    new FloatCyclePoint(0d, 0.025f),
                    new FloatCyclePoint(DAWN_START_TIME_SECONDS, 0.06f),
                    new FloatCyclePoint(DAY_START_TIME_SECONDS, 0.04f),
                    new FloatCyclePoint(DUSK_START_TIME_SECONDS, 0.065f),
                    new FloatCyclePoint(NIGHT_START_TIME_SECONDS, 0.025f)),
                CreateConstantFloatOutput(outputSchemaSO.SkySunSizeConvergence, 5f),
                CreateFloatOutput(
                    outputSchemaSO.SkyAtmosphereThickness,
                    new FloatCyclePoint(0d, 0.35f),
                    new FloatCyclePoint(DAWN_START_TIME_SECONDS, 1.3f),
                    new FloatCyclePoint(DAY_START_TIME_SECONDS, 0.9f),
                    new FloatCyclePoint(DUSK_START_TIME_SECONDS, 1.45f),
                    new FloatCyclePoint(NIGHT_START_TIME_SECONDS, 0.35f)),
                CreateColorOutput(
                    outputSchemaSO.SkyTint,
                    new ColorCyclePoint(0d, new Color(0.08f, 0.12f, 0.28f)),
                    new ColorCyclePoint(DAWN_START_TIME_SECONDS, new Color(0.78f, 0.42f, 0.38f)),
                    new ColorCyclePoint(DAY_START_TIME_SECONDS, new Color(0.45f, 0.62f, 0.9f)),
                    new ColorCyclePoint(DUSK_START_TIME_SECONDS, new Color(0.82f, 0.32f, 0.28f)),
                    new ColorCyclePoint(NIGHT_START_TIME_SECONDS, new Color(0.08f, 0.12f, 0.28f))),
                CreateColorOutput(
                    outputSchemaSO.SkyGroundColor,
                    new ColorCyclePoint(0d, new Color(0.03f, 0.04f, 0.08f)),
                    new ColorCyclePoint(DAWN_START_TIME_SECONDS, new Color(0.32f, 0.16f, 0.12f)),
                    new ColorCyclePoint(DAY_START_TIME_SECONDS, new Color(0.34f, 0.32f, 0.28f)),
                    new ColorCyclePoint(DUSK_START_TIME_SECONDS, new Color(0.28f, 0.12f, 0.09f)),
                    new ColorCyclePoint(NIGHT_START_TIME_SECONDS, new Color(0.03f, 0.04f, 0.08f))),
                CreateFloatOutput(
                    outputSchemaSO.SkyExposure,
                    new FloatCyclePoint(0d, 0.35f),
                    new FloatCyclePoint(DAWN_START_TIME_SECONDS, 0.75f),
                    new FloatCyclePoint(DAY_START_TIME_SECONDS, 1.25f),
                    new FloatCyclePoint(DUSK_START_TIME_SECONDS, 0.7f),
                    new FloatCyclePoint(NIGHT_START_TIME_SECONDS, 0.35f)),
                CreateConstantBoolOutput(outputSchemaSO.FogEnabled, true),
                CreateConstantIntOutput(outputSchemaSO.FogMode, (int)FogMode.ExponentialSquared),
                CreateColorOutput(
                    outputSchemaSO.FogColor,
                    new ColorCyclePoint(0d, new Color(0.055f, 0.07f, 0.14f)),
                    new ColorCyclePoint(DAWN_START_TIME_SECONDS, new Color(0.48f, 0.3f, 0.28f)),
                    new ColorCyclePoint(DAY_START_TIME_SECONDS, new Color(0.62f, 0.72f, 0.82f)),
                    new ColorCyclePoint(DUSK_START_TIME_SECONDS, new Color(0.48f, 0.24f, 0.22f)),
                    new ColorCyclePoint(NIGHT_START_TIME_SECONDS, new Color(0.055f, 0.07f, 0.14f))),
                CreateFloatOutput(
                    outputSchemaSO.FogDensity,
                    new FloatCyclePoint(0d, 0.008f),
                    new FloatCyclePoint(DAY_START_TIME_SECONDS, 0.002f),
                    new FloatCyclePoint(DUSK_START_TIME_SECONDS, 0.004f),
                    new FloatCyclePoint(NIGHT_START_TIME_SECONDS, 0.008f)),
                CreateConstantFloatOutput(outputSchemaSO.FogStartDistanceMeters, 0f),
                CreateConstantFloatOutput(outputSchemaSO.FogEndDistanceMeters, 300f),
                CreateConstantIntOutput(outputSchemaSO.AmbientMode, (int)AmbientMode.Trilight),
                CreateFloatOutput(
                    outputSchemaSO.AmbientIntensity,
                    new FloatCyclePoint(0d, 0.28f),
                    new FloatCyclePoint(DAY_START_TIME_SECONDS, 1f),
                    new FloatCyclePoint(DUSK_START_TIME_SECONDS, 0.55f),
                    new FloatCyclePoint(NIGHT_START_TIME_SECONDS, 0.28f)),
                CreateAmbientColorOutput(outputSchemaSO.AmbientFlatColor, 0.22f),
                CreateColorOutput(
                    outputSchemaSO.AmbientSkyColor,
                    new ColorCyclePoint(0d, new Color(0.035f, 0.055f, 0.14f)),
                    new ColorCyclePoint(DAY_START_TIME_SECONDS, new Color(0.48f, 0.62f, 0.82f)),
                    new ColorCyclePoint(DUSK_START_TIME_SECONDS, new Color(0.5f, 0.24f, 0.22f)),
                    new ColorCyclePoint(NIGHT_START_TIME_SECONDS, new Color(0.035f, 0.055f, 0.14f))),
                CreateColorOutput(
                    outputSchemaSO.AmbientEquatorColor,
                    new ColorCyclePoint(0d, new Color(0.025f, 0.035f, 0.07f)),
                    new ColorCyclePoint(DAY_START_TIME_SECONDS, new Color(0.34f, 0.4f, 0.46f)),
                    new ColorCyclePoint(DUSK_START_TIME_SECONDS, new Color(0.32f, 0.16f, 0.13f)),
                    new ColorCyclePoint(NIGHT_START_TIME_SECONDS, new Color(0.025f, 0.035f, 0.07f))),
                CreateColorOutput(
                    outputSchemaSO.AmbientGroundColor,
                    new ColorCyclePoint(0d, new Color(0.012f, 0.014f, 0.025f)),
                    new ColorCyclePoint(DAY_START_TIME_SECONDS, new Color(0.16f, 0.15f, 0.13f)),
                    new ColorCyclePoint(DUSK_START_TIME_SECONDS, new Color(0.12f, 0.07f, 0.06f)),
                    new ColorCyclePoint(NIGHT_START_TIME_SECONDS, new Color(0.012f, 0.014f, 0.025f))),
            };

            return outputs;
        }

        private static CycleOutputDefinition CreateMainLightEnabledOutput(
            DayNightEnvironmentOutputSchemaSO outputSchemaSO)
        {
            return CreateConstantBoolOutput(outputSchemaSO.MainLightEnabled, true);
        }

        private static CycleOutputDefinition CreateConstantBoolOutput(
            BoolCycleOutputKeySO outputKeySO,
            bool value)
        {
            return new BoolCycleOutputDefinition(
                outputKeySO,
                PROFILE_TRANSITION_DURATION_SECONDS,
                new[] { new BoolCyclePoint(0d, value) },
                new List<BoolPeriodValue>());
        }

        private static CycleOutputDefinition CreateConstantIntOutput(
            IntCycleOutputKeySO outputKeySO,
            int value)
        {
            return new IntCycleOutputDefinition(
                outputKeySO,
                PROFILE_TRANSITION_DURATION_SECONDS,
                new[] { new IntCyclePoint(0d, value) },
                new List<IntPeriodValue>());
        }

        private static CycleOutputDefinition CreateConstantFloatOutput(
            FloatCycleOutputKeySO outputKeySO,
            float value)
        {
            return CreateFloatOutput(outputKeySO, new FloatCyclePoint(0d, value));
        }

        private static CycleOutputDefinition CreateFloatOutput(
            FloatCycleOutputKeySO outputKeySO,
            params FloatCyclePoint[] points)
        {
            return new FloatCycleOutputDefinition(
                outputKeySO,
                PROFILE_TRANSITION_DURATION_SECONDS,
                AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
                points);
        }

        private static CycleOutputDefinition CreateAmbientColorOutput(
            ColorCycleOutputKeySO outputKeySO,
            float brightness)
        {
            return CreateColorOutput(outputKeySO, new ColorCyclePoint(0d, Color.white * brightness));
        }

        private static CycleOutputDefinition CreateColorOutput(
            ColorCycleOutputKeySO outputKeySO,
            params ColorCyclePoint[] points)
        {
            return new ColorCycleOutputDefinition(
                outputKeySO,
                PROFILE_TRANSITION_DURATION_SECONDS,
                AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
                points);
        }

        private static void CreateOrUpdatePrefab(
            TimeOfCycleProfileSO profileSO,
            Material proceduralSkyMaterial,
            DayNightEnvironmentOutputSchemaSO outputSchemaSO)
        {
            GameObject root = new("Day Night Cycle");
            try
            {
                TimeOfCycleLifetimeScope lifetimeScope = root.AddComponent<TimeOfCycleLifetimeScope>();
                EnvironmentCubemapRefreshController refreshController =
                    root.AddComponent<EnvironmentCubemapRefreshController>();
                ProceduralSkyOutputApplicator skyApplicator =
                    root.AddComponent<ProceduralSkyOutputApplicator>();
                FogRenderSettingsOutputApplicator fogApplicator =
                    root.AddComponent<FogRenderSettingsOutputApplicator>();
                AmbientRenderSettingsOutputApplicator ambientApplicator =
                    root.AddComponent<AmbientRenderSettingsOutputApplicator>();

                GameObject sunObject = new("Sun Light");
                sunObject.transform.SetParent(root.transform, false);
                sunObject.transform.rotation = Quaternion.Euler(35f, 330f, 0f);
                Light directionalLight = sunObject.AddComponent<Light>();
                directionalLight.type = LightType.Directional;
                directionalLight.shadows = LightShadows.Soft;
                directionalLight.intensity = 1f;
                DirectionalLightOutputApplicator lightApplicator =
                    sunObject.AddComponent<DirectionalLightOutputApplicator>();

                lightApplicator.ConfigureForEditor(directionalLight, outputSchemaSO);
                skyApplicator.ConfigureForEditor(proceduralSkyMaterial, refreshController, outputSchemaSO);
                fogApplicator.ConfigureForEditor(outputSchemaSO);
                ambientApplicator.ConfigureForEditor(outputSchemaSO);
                lifetimeScope.ConfigureForEditor(
                    profileSO,
                    new MonoBehaviour[]
                    {
                        lightApplicator,
                        skyApplicator,
                        fogApplicator,
                        ambientApplicator,
                    });

                PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        #endregion
    }
}
#endif
