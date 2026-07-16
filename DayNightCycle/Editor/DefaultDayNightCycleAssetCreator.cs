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
        private const string PROFILE_PATH = PROFILE_FOLDER_PATH + "/Default Day Night Cycle Profile.asset";
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
            EnsureAssetFoldersExist();
            Material proceduralSkyMaterial = CreateOrUpdateProceduralSkyMaterial();
            TimeOfCycleProfileSO profileSO = CreateOrUpdateProfile();
            CreateOrUpdatePrefab(profileSO, proceduralSkyMaterial);
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
        }

        private static void CreateFolderIfMissing(string parentPath, string folderName)
        {
            string folderPath = parentPath + "/" + folderName;
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder(parentPath, folderName);
            }
        }

        private static Material CreateOrUpdateProceduralSkyMaterial()
        {
            Shader proceduralSkyShader = Shader.Find("Skybox/Procedural");
            if (proceduralSkyShader == null)
            {
                Echo.Error("Cannot create the day-night sky material because Skybox/Procedural is unavailable.");
                return null;
            }

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

        private static TimeOfCycleProfileSO CreateOrUpdateProfile()
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
                CreateOutputs());
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

        private static IReadOnlyList<CycleOutputDefinition> CreateOutputs()
        {
            AnimationCurve smoothCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            List<CycleOutputDefinition> outputs = new()
            {
                CreateMainLightEnabledOutput(),
                new RotationCycleOutputDefinition(
                    DayNightEnvironmentOutputIds.MAIN_LIGHT_ROTATION,
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
                    DayNightEnvironmentOutputIds.MAIN_LIGHT_COLOR,
                    new ColorCyclePoint(0d, new Color(0.28f, 0.36f, 0.62f)),
                    new ColorCyclePoint(19800d, new Color(1f, 0.55f, 0.32f)),
                    new ColorCyclePoint(DAY_START_TIME_SECONDS, new Color(1f, 0.96f, 0.84f)),
                    new ColorCyclePoint(DUSK_START_TIME_SECONDS, new Color(1f, 0.42f, 0.2f)),
                    new ColorCyclePoint(NIGHT_START_TIME_SECONDS, new Color(0.28f, 0.36f, 0.62f))),
                CreateFloatOutput(
                    DayNightEnvironmentOutputIds.MAIN_LIGHT_INTENSITY,
                    new FloatCyclePoint(0d, 0.04f),
                    new FloatCyclePoint(DAWN_START_TIME_SECONDS, 0.15f),
                    new FloatCyclePoint(DAY_START_TIME_SECONDS, 1f),
                    new FloatCyclePoint(DUSK_START_TIME_SECONDS, 0.25f),
                    new FloatCyclePoint(NIGHT_START_TIME_SECONDS, 0.04f)),
                CreateFloatOutput(
                    DayNightEnvironmentOutputIds.MAIN_LIGHT_SHADOW_STRENGTH,
                    new FloatCyclePoint(0d, 0.1f),
                    new FloatCyclePoint(DAY_START_TIME_SECONDS, 0.85f),
                    new FloatCyclePoint(DUSK_START_TIME_SECONDS, 0.45f),
                    new FloatCyclePoint(NIGHT_START_TIME_SECONDS, 0.1f)),
                CreateConstantIntOutput(DayNightEnvironmentOutputIds.SKY_SUN_DISK, 2),
                CreateFloatOutput(
                    DayNightEnvironmentOutputIds.SKY_SUN_SIZE,
                    new FloatCyclePoint(0d, 0.025f),
                    new FloatCyclePoint(DAWN_START_TIME_SECONDS, 0.06f),
                    new FloatCyclePoint(DAY_START_TIME_SECONDS, 0.04f),
                    new FloatCyclePoint(DUSK_START_TIME_SECONDS, 0.065f),
                    new FloatCyclePoint(NIGHT_START_TIME_SECONDS, 0.025f)),
                CreateConstantFloatOutput(DayNightEnvironmentOutputIds.SKY_SUN_SIZE_CONVERGENCE, 5f),
                CreateFloatOutput(
                    DayNightEnvironmentOutputIds.SKY_ATMOSPHERE_THICKNESS,
                    new FloatCyclePoint(0d, 0.35f),
                    new FloatCyclePoint(DAWN_START_TIME_SECONDS, 1.3f),
                    new FloatCyclePoint(DAY_START_TIME_SECONDS, 0.9f),
                    new FloatCyclePoint(DUSK_START_TIME_SECONDS, 1.45f),
                    new FloatCyclePoint(NIGHT_START_TIME_SECONDS, 0.35f)),
                CreateColorOutput(
                    DayNightEnvironmentOutputIds.SKY_TINT,
                    new ColorCyclePoint(0d, new Color(0.08f, 0.12f, 0.28f)),
                    new ColorCyclePoint(DAWN_START_TIME_SECONDS, new Color(0.78f, 0.42f, 0.38f)),
                    new ColorCyclePoint(DAY_START_TIME_SECONDS, new Color(0.45f, 0.62f, 0.9f)),
                    new ColorCyclePoint(DUSK_START_TIME_SECONDS, new Color(0.82f, 0.32f, 0.28f)),
                    new ColorCyclePoint(NIGHT_START_TIME_SECONDS, new Color(0.08f, 0.12f, 0.28f))),
                CreateColorOutput(
                    DayNightEnvironmentOutputIds.SKY_GROUND_COLOR,
                    new ColorCyclePoint(0d, new Color(0.03f, 0.04f, 0.08f)),
                    new ColorCyclePoint(DAWN_START_TIME_SECONDS, new Color(0.32f, 0.16f, 0.12f)),
                    new ColorCyclePoint(DAY_START_TIME_SECONDS, new Color(0.34f, 0.32f, 0.28f)),
                    new ColorCyclePoint(DUSK_START_TIME_SECONDS, new Color(0.28f, 0.12f, 0.09f)),
                    new ColorCyclePoint(NIGHT_START_TIME_SECONDS, new Color(0.03f, 0.04f, 0.08f))),
                CreateFloatOutput(
                    DayNightEnvironmentOutputIds.SKY_EXPOSURE,
                    new FloatCyclePoint(0d, 0.35f),
                    new FloatCyclePoint(DAWN_START_TIME_SECONDS, 0.75f),
                    new FloatCyclePoint(DAY_START_TIME_SECONDS, 1.25f),
                    new FloatCyclePoint(DUSK_START_TIME_SECONDS, 0.7f),
                    new FloatCyclePoint(NIGHT_START_TIME_SECONDS, 0.35f)),
                CreateConstantBoolOutput(DayNightEnvironmentOutputIds.FOG_ENABLED, true),
                CreateConstantIntOutput(DayNightEnvironmentOutputIds.FOG_MODE, (int)FogMode.ExponentialSquared),
                CreateColorOutput(
                    DayNightEnvironmentOutputIds.FOG_COLOR,
                    new ColorCyclePoint(0d, new Color(0.055f, 0.07f, 0.14f)),
                    new ColorCyclePoint(DAWN_START_TIME_SECONDS, new Color(0.48f, 0.3f, 0.28f)),
                    new ColorCyclePoint(DAY_START_TIME_SECONDS, new Color(0.62f, 0.72f, 0.82f)),
                    new ColorCyclePoint(DUSK_START_TIME_SECONDS, new Color(0.48f, 0.24f, 0.22f)),
                    new ColorCyclePoint(NIGHT_START_TIME_SECONDS, new Color(0.055f, 0.07f, 0.14f))),
                CreateFloatOutput(
                    DayNightEnvironmentOutputIds.FOG_DENSITY,
                    new FloatCyclePoint(0d, 0.008f),
                    new FloatCyclePoint(DAY_START_TIME_SECONDS, 0.002f),
                    new FloatCyclePoint(DUSK_START_TIME_SECONDS, 0.004f),
                    new FloatCyclePoint(NIGHT_START_TIME_SECONDS, 0.008f)),
                CreateConstantFloatOutput(DayNightEnvironmentOutputIds.FOG_START_DISTANCE_METERS, 0f),
                CreateConstantFloatOutput(DayNightEnvironmentOutputIds.FOG_END_DISTANCE_METERS, 300f),
                CreateConstantIntOutput(DayNightEnvironmentOutputIds.AMBIENT_MODE, (int)AmbientMode.Trilight),
                CreateFloatOutput(
                    DayNightEnvironmentOutputIds.AMBIENT_INTENSITY,
                    new FloatCyclePoint(0d, 0.28f),
                    new FloatCyclePoint(DAY_START_TIME_SECONDS, 1f),
                    new FloatCyclePoint(DUSK_START_TIME_SECONDS, 0.55f),
                    new FloatCyclePoint(NIGHT_START_TIME_SECONDS, 0.28f)),
                CreateAmbientColorOutput(DayNightEnvironmentOutputIds.AMBIENT_FLAT_COLOR, 0.22f),
                CreateColorOutput(
                    DayNightEnvironmentOutputIds.AMBIENT_SKY_COLOR,
                    new ColorCyclePoint(0d, new Color(0.035f, 0.055f, 0.14f)),
                    new ColorCyclePoint(DAY_START_TIME_SECONDS, new Color(0.48f, 0.62f, 0.82f)),
                    new ColorCyclePoint(DUSK_START_TIME_SECONDS, new Color(0.5f, 0.24f, 0.22f)),
                    new ColorCyclePoint(NIGHT_START_TIME_SECONDS, new Color(0.035f, 0.055f, 0.14f))),
                CreateColorOutput(
                    DayNightEnvironmentOutputIds.AMBIENT_EQUATOR_COLOR,
                    new ColorCyclePoint(0d, new Color(0.025f, 0.035f, 0.07f)),
                    new ColorCyclePoint(DAY_START_TIME_SECONDS, new Color(0.34f, 0.4f, 0.46f)),
                    new ColorCyclePoint(DUSK_START_TIME_SECONDS, new Color(0.32f, 0.16f, 0.13f)),
                    new ColorCyclePoint(NIGHT_START_TIME_SECONDS, new Color(0.025f, 0.035f, 0.07f))),
                CreateColorOutput(
                    DayNightEnvironmentOutputIds.AMBIENT_GROUND_COLOR,
                    new ColorCyclePoint(0d, new Color(0.012f, 0.014f, 0.025f)),
                    new ColorCyclePoint(DAY_START_TIME_SECONDS, new Color(0.16f, 0.15f, 0.13f)),
                    new ColorCyclePoint(DUSK_START_TIME_SECONDS, new Color(0.12f, 0.07f, 0.06f)),
                    new ColorCyclePoint(NIGHT_START_TIME_SECONDS, new Color(0.012f, 0.014f, 0.025f))),
            };

            return outputs;
        }

        private static CycleOutputDefinition CreateMainLightEnabledOutput()
        {
            return CreateConstantBoolOutput(DayNightEnvironmentOutputIds.MAIN_LIGHT_ENABLED, true);
        }

        private static CycleOutputDefinition CreateConstantBoolOutput(string outputId, bool value)
        {
            return new BoolCycleOutputDefinition(
                outputId,
                PROFILE_TRANSITION_DURATION_SECONDS,
                new[] { new BoolCyclePoint(0d, value) },
                new List<BoolPeriodValue>());
        }

        private static CycleOutputDefinition CreateConstantIntOutput(string outputId, int value)
        {
            return new IntCycleOutputDefinition(
                outputId,
                PROFILE_TRANSITION_DURATION_SECONDS,
                new[] { new IntCyclePoint(0d, value) },
                new List<IntPeriodValue>());
        }

        private static CycleOutputDefinition CreateConstantFloatOutput(string outputId, float value)
        {
            return CreateFloatOutput(outputId, new FloatCyclePoint(0d, value));
        }

        private static CycleOutputDefinition CreateFloatOutput(
            string outputId,
            params FloatCyclePoint[] points)
        {
            return new FloatCycleOutputDefinition(
                outputId,
                PROFILE_TRANSITION_DURATION_SECONDS,
                AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
                points);
        }

        private static CycleOutputDefinition CreateAmbientColorOutput(string outputId, float brightness)
        {
            return CreateColorOutput(outputId, new ColorCyclePoint(0d, Color.white * brightness));
        }

        private static CycleOutputDefinition CreateColorOutput(
            string outputId,
            params ColorCyclePoint[] points)
        {
            return new ColorCycleOutputDefinition(
                outputId,
                PROFILE_TRANSITION_DURATION_SECONDS,
                AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
                points);
        }

        private static void CreateOrUpdatePrefab(
            TimeOfCycleProfileSO profileSO,
            Material proceduralSkyMaterial)
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

                lightApplicator.ConfigureForEditor(directionalLight);
                skyApplicator.ConfigureForEditor(proceduralSkyMaterial, refreshController);
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
