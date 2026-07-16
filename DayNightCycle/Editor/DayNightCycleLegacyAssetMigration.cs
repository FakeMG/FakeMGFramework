#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using FakeMG.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FakeMG.DayNightCycle.Editor
{
    /// <summary>
    /// Migrates legacy string-bound profiles and environment applicators to persistent typed output-key assets.
    /// </summary>
    public static class DayNightCycleLegacyAssetMigration
    {
        private const int CURRENT_MIGRATION_VERSION = 1;
        private const string MIGRATION_VERSION_KEY = "FakeMG.DayNightCycle.MigrationVersion";
        private const string OUTPUT_KEY_ROOT_PATH =
            "Assets/Thirdparty/FakeMGFramework/DayNightCycle/Output Keys";
        private const string MIGRATED_OUTPUT_KEY_FOLDER_PATH = OUTPUT_KEY_ROOT_PATH + "/Migrated";

        #region Public Methods

        [MenuItem(FakeMGEditorMenus.DAY_NIGHT_CYCLE + "/Migrate Legacy Assets")]
        public static void MigrateAllAssets()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Echo.Warning("Legacy day-night migration was cancelled because open scenes were not saved.");
                return;
            }

            EnsureMigrationFolderExists();
            Dictionary<string, CycleOutputKeySO> keyByLegacyId = LoadKeyRegistry();
            int migratedAssetCount = MigrateProfileAssets<TimeOfCycleProfileSO>(keyByLegacyId);
            migratedAssetCount += MigrateProfileAssets<TimeOfCycleOverrideSO>(keyByLegacyId);
            migratedAssetCount += MigratePrefabs(keyByLegacyId);
            migratedAssetCount += MigrateScenes(keyByLegacyId);
            AssetDatabase.SaveAssets();
            EditorPrefs.SetInt(MIGRATION_VERSION_KEY, CURRENT_MIGRATION_VERSION);
            Echo.Log(
                $"Day-night migration version {CURRENT_MIGRATION_VERSION} completed. " +
                $"Updated {migratedAssetCount} assets.");
        }

        public static void MigrateProfilesOverridesAndPrefabs()
        {
            EnsureMigrationFolderExists();
            Dictionary<string, CycleOutputKeySO> keyByLegacyId = LoadKeyRegistry();
            MigrateProfileAssets<TimeOfCycleProfileSO>(keyByLegacyId);
            MigrateProfileAssets<TimeOfCycleOverrideSO>(keyByLegacyId);
            MigratePrefabs(keyByLegacyId);
        }

        #endregion

        #region Private Methods

        private static int MigrateProfileAssets<T>(IDictionary<string, CycleOutputKeySO> keyByLegacyId)
            where T : ScriptableObject
        {
            int migratedAssetCount = 0;
            string[] assetGuids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            for (int assetIndex = 0; assetIndex < assetGuids.Length; assetIndex++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(assetGuids[assetIndex]);
                T profileAsset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                IReadOnlyList<CycleOutputDefinition> definitions = profileAsset switch
                {
                    TimeOfCycleProfileSO profileSO => profileSO.OutputDefinitions,
                    TimeOfCycleOverrideSO overrideSO => overrideSO.OutputDefinitions,
                    _ => null,
                };
                if (definitions == null)
                {
                    Echo.Error($"Unsupported legacy profile asset type '{typeof(T).Name}'.");
                    continue;
                }

                bool hasChanged = false;
                for (int definitionIndex = 0; definitionIndex < definitions.Count; definitionIndex++)
                {
                    CycleOutputDefinition definition = definitions[definitionIndex];
                    if (definition == null
                        || definition.OutputKeySO != null
                        || string.IsNullOrWhiteSpace(definition.LegacyOutputId))
                    {
                        continue;
                    }

                    CycleOutputKeySO outputKeySO = ResolveOrCreateKey(
                        definition.LegacyOutputId,
                        definition.ValueType,
                        keyByLegacyId);
                    if (outputKeySO == null)
                    {
                        Echo.Error(
                            $"Could not migrate output '{definition.LegacyOutputId}' in '{assetPath}'.");
                        continue;
                    }

                    definition.ConfigureOutputKeyForEditor(outputKeySO);
                    hasChanged = true;
                }

                if (hasChanged)
                {
                    EditorUtility.SetDirty(profileAsset);
                    migratedAssetCount++;
                }
            }

            return migratedAssetCount;
        }

        private static int MigratePrefabs(IDictionary<string, CycleOutputKeySO> keyByLegacyId)
        {
            int migratedAssetCount = 0;
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            for (int prefabIndex = 0; prefabIndex < prefabGuids.Length; prefabIndex++)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[prefabIndex]);
                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    if (!TryMigrateHierarchy(prefabRoot, prefabGuids[prefabIndex], keyByLegacyId))
                    {
                        continue;
                    }

                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                    migratedAssetCount++;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
            }

            return migratedAssetCount;
        }

        private static int MigrateScenes(IDictionary<string, CycleOutputKeySO> keyByLegacyId)
        {
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
            int migratedAssetCount = 0;
            try
            {
                string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
                for (int sceneIndex = 0; sceneIndex < sceneGuids.Length; sceneIndex++)
                {
                    string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[sceneIndex]);
                    if (!MightContainOutputApplicator(scenePath))
                    {
                        continue;
                    }

                    Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                    bool hasChanged = false;
                    GameObject[] rootObjects = scene.GetRootGameObjects();
                    for (int rootIndex = 0; rootIndex < rootObjects.Length; rootIndex++)
                    {
                        hasChanged |= TryMigrateHierarchy(
                            rootObjects[rootIndex],
                            sceneGuids[sceneIndex],
                            keyByLegacyId);
                    }

                    if (hasChanged)
                    {
                        EditorSceneManager.SaveScene(scene);
                        migratedAssetCount++;
                    }

                    EditorSceneManager.CloseScene(scene, true);
                }
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
            }

            return migratedAssetCount;
        }

        private static bool MightContainOutputApplicator(string scenePath)
        {
            string[] dependencies = AssetDatabase.GetDependencies(scenePath, false);
            for (int dependencyIndex = 0; dependencyIndex < dependencies.Length; dependencyIndex++)
            {
                string dependencyPath = dependencies[dependencyIndex];
                if (dependencyPath.EndsWith("/DirectionalLightOutputApplicator.cs", StringComparison.Ordinal)
                    || dependencyPath.EndsWith("/ProceduralSkyOutputApplicator.cs", StringComparison.Ordinal)
                    || dependencyPath.EndsWith("/FogRenderSettingsOutputApplicator.cs", StringComparison.Ordinal)
                    || dependencyPath.EndsWith("/AmbientRenderSettingsOutputApplicator.cs", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryMigrateHierarchy(
            GameObject root,
            string sourceGuid,
            IDictionary<string, CycleOutputKeySO> keyByLegacyId)
        {
            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            List<MonoBehaviour> legacyApplicators = new();
            for (int behaviourIndex = 0; behaviourIndex < behaviours.Length; behaviourIndex++)
            {
                MonoBehaviour behaviour = behaviours[behaviourIndex];
                if (behaviour is ITimeOfCycleOutputApplicator
                    && GetSchemaReference(behaviour) == null)
                {
                    legacyApplicators.Add(behaviour);
                }
            }

            if (legacyApplicators.Count == 0)
            {
                return false;
            }

            if (!TryCreateMigratedSchema(
                    sourceGuid,
                    legacyApplicators,
                    keyByLegacyId,
                    out DayNightEnvironmentOutputSchemaSO outputSchemaSO))
            {
                return false;
            }

            for (int applicatorIndex = 0; applicatorIndex < legacyApplicators.Count; applicatorIndex++)
            {
                SerializedObject serializedApplicator = new(legacyApplicators[applicatorIndex]);
                serializedApplicator.FindProperty("_outputSchemaSO").objectReferenceValue = outputSchemaSO;
                serializedApplicator.ApplyModifiedPropertiesWithoutUndo();
            }

            return true;
        }

        private static bool TryCreateMigratedSchema(
            string sourceGuid,
            IReadOnlyList<MonoBehaviour> applicators,
            IDictionary<string, CycleOutputKeySO> keyByLegacyId,
            out DayNightEnvironmentOutputSchemaSO outputSchemaSO)
        {
            string schemaPath = MIGRATED_OUTPUT_KEY_FOLDER_PATH + "/" + sourceGuid + " Environment Schema.asset";
            outputSchemaSO = AssetDatabase.LoadAssetAtPath<DayNightEnvironmentOutputSchemaSO>(schemaPath);
            bool hasCreatedSchema = outputSchemaSO == null;
            if (outputSchemaSO == null)
            {
                outputSchemaSO = ScriptableObject.CreateInstance<DayNightEnvironmentOutputSchemaSO>();
                AssetDatabase.CreateAsset(outputSchemaSO, schemaPath);
            }

            MonoBehaviour light = FindApplicator<DirectionalLightOutputApplicator>(applicators);
            MonoBehaviour sky = FindApplicator<ProceduralSkyOutputApplicator>(applicators);
            MonoBehaviour fog = FindApplicator<FogRenderSettingsOutputApplicator>(applicators);
            MonoBehaviour ambient = FindApplicator<AmbientRenderSettingsOutputApplicator>(applicators);
            try
            {
                outputSchemaSO.ConfigureForEditor(
                    mainLightEnabled: Resolve<BoolCycleOutputKeySO>(light, "_legacyEnabledOutputId", "main_light_enabled", keyByLegacyId),
                    mainLightRotation: Resolve<RotationCycleOutputKeySO>(light, "_legacyRotationOutputId", "main_light_rotation", keyByLegacyId),
                    mainLightColor: Resolve<ColorCycleOutputKeySO>(light, "_legacyColorOutputId", "main_light_color", keyByLegacyId),
                    mainLightIntensity: Resolve<FloatCycleOutputKeySO>(light, "_legacyIntensityOutputId", "main_light_intensity", keyByLegacyId),
                    mainLightShadowStrength: Resolve<FloatCycleOutputKeySO>(light, "_legacyShadowStrengthOutputId", "main_light_shadow_strength", keyByLegacyId),
                    skySunDisk: Resolve<IntCycleOutputKeySO>(sky, "_legacySunDiskOutputId", "sky_sun_disk", keyByLegacyId),
                    skySunSize: Resolve<FloatCycleOutputKeySO>(sky, "_legacySunSizeOutputId", "sky_sun_size", keyByLegacyId),
                    skySunSizeConvergence: Resolve<FloatCycleOutputKeySO>(sky, "_legacySunSizeConvergenceOutputId", "sky_sun_size_convergence", keyByLegacyId),
                    skyAtmosphereThickness: Resolve<FloatCycleOutputKeySO>(sky, "_legacyAtmosphereThicknessOutputId", "sky_atmosphere_thickness", keyByLegacyId),
                    skyTint: Resolve<ColorCycleOutputKeySO>(sky, "_legacySkyTintOutputId", "sky_tint", keyByLegacyId),
                    skyGroundColor: Resolve<ColorCycleOutputKeySO>(sky, "_legacyGroundColorOutputId", "sky_ground_color", keyByLegacyId),
                    skyExposure: Resolve<FloatCycleOutputKeySO>(sky, "_legacyExposureOutputId", "sky_exposure", keyByLegacyId),
                    fogEnabled: Resolve<BoolCycleOutputKeySO>(fog, "_legacyEnabledOutputId", "fog_enabled", keyByLegacyId),
                    fogMode: Resolve<IntCycleOutputKeySO>(fog, "_legacyModeOutputId", "fog_mode", keyByLegacyId),
                    fogColor: Resolve<ColorCycleOutputKeySO>(fog, "_legacyColorOutputId", "fog_color", keyByLegacyId),
                    fogDensity: Resolve<FloatCycleOutputKeySO>(fog, "_legacyDensityOutputId", "fog_density", keyByLegacyId),
                    fogStartDistanceMeters: Resolve<FloatCycleOutputKeySO>(fog, "_legacyStartDistanceOutputId", "fog_start_distance_meters", keyByLegacyId),
                    fogEndDistanceMeters: Resolve<FloatCycleOutputKeySO>(fog, "_legacyEndDistanceOutputId", "fog_end_distance_meters", keyByLegacyId),
                    ambientMode: Resolve<IntCycleOutputKeySO>(ambient, "_legacyModeOutputId", "ambient_mode", keyByLegacyId),
                    ambientIntensity: Resolve<FloatCycleOutputKeySO>(ambient, "_legacyIntensityOutputId", "ambient_intensity", keyByLegacyId),
                    ambientFlatColor: Resolve<ColorCycleOutputKeySO>(ambient, "_legacyFlatColorOutputId", "ambient_flat_color", keyByLegacyId),
                    ambientSkyColor: Resolve<ColorCycleOutputKeySO>(ambient, "_legacySkyColorOutputId", "ambient_sky_color", keyByLegacyId),
                    ambientEquatorColor: Resolve<ColorCycleOutputKeySO>(ambient, "_legacyEquatorColorOutputId", "ambient_equator_color", keyByLegacyId),
                    ambientGroundColor: Resolve<ColorCycleOutputKeySO>(ambient, "_legacyGroundColorOutputId", "ambient_ground_color", keyByLegacyId));
            }
            catch (InvalidOperationException exception)
            {
                if (hasCreatedSchema)
                {
                    AssetDatabase.DeleteAsset(schemaPath);
                    outputSchemaSO = null;
                }

                Echo.Error($"Could not migrate environment schema for source {sourceGuid}. {exception.Message}");
                return false;
            }

            EditorUtility.SetDirty(outputSchemaSO);
            return true;
        }

        private static T FindApplicator<T>(IReadOnlyList<MonoBehaviour> applicators) where T : MonoBehaviour
        {
            for (int applicatorIndex = 0; applicatorIndex < applicators.Count; applicatorIndex++)
            {
                if (applicators[applicatorIndex] is T typedApplicator)
                {
                    return typedApplicator;
                }
            }

            return null;
        }

        private static T Resolve<T>(
            MonoBehaviour applicator,
            string legacyPropertyName,
            string defaultLegacyId,
            IDictionary<string, CycleOutputKeySO> keyByLegacyId)
            where T : CycleOutputKeySO
        {
            string legacyId = defaultLegacyId;
            if (applicator != null)
            {
                SerializedProperty legacyProperty = new SerializedObject(applicator).FindProperty(legacyPropertyName);
                if (legacyProperty != null && !string.IsNullOrWhiteSpace(legacyProperty.stringValue))
                {
                    legacyId = legacyProperty.stringValue;
                }
            }

            if (keyByLegacyId.TryGetValue(legacyId, out CycleOutputKeySO outputKeySO)
                && outputKeySO is T typedOutputKeySO)
            {
                return typedOutputKeySO;
            }

            throw new InvalidOperationException(
                $"Legacy output '{legacyId}' has no migrated {typeof(T).Name} key.");
        }

        private static CycleOutputKeySO ResolveOrCreateKey(
            string legacyId,
            Type valueType,
            IDictionary<string, CycleOutputKeySO> keyByLegacyId)
        {
            if (keyByLegacyId.TryGetValue(legacyId, out CycleOutputKeySO existingKeySO))
            {
                if (existingKeySO.ValueType == valueType)
                {
                    return existingKeySO;
                }

                Echo.Error(
                    $"Legacy output '{legacyId}' is used as both {existingKeySO.ValueType.Name} " +
                    $"and {valueType.Name}.");
                return null;
            }

            Type keyType = GetKeyType(valueType);
            if (keyType == null)
            {
                Echo.Error($"Legacy output '{legacyId}' uses unsupported type {valueType.Name}.");
                return null;
            }

            CycleOutputKeySO outputKeySO = (CycleOutputKeySO)ScriptableObject.CreateInstance(keyType);
            outputKeySO.ConfigureLegacyIdForEditor(legacyId);
            string assetPath = AssetDatabase.GenerateUniqueAssetPath(
                MIGRATED_OUTPUT_KEY_FOLDER_PATH + "/" + SanitizeFileName(legacyId) + ".asset");
            AssetDatabase.CreateAsset(outputKeySO, assetPath);
            keyByLegacyId.Add(legacyId, outputKeySO);
            return outputKeySO;
        }

        private static Type GetKeyType(Type valueType)
        {
            if (valueType == typeof(bool)) return typeof(BoolCycleOutputKeySO);
            if (valueType == typeof(int)) return typeof(IntCycleOutputKeySO);
            if (valueType == typeof(float)) return typeof(FloatCycleOutputKeySO);
            if (valueType == typeof(Color)) return typeof(ColorCycleOutputKeySO);
            if (valueType == typeof(Quaternion)) return typeof(RotationCycleOutputKeySO);
            return null;
        }

        private static Dictionary<string, CycleOutputKeySO> LoadKeyRegistry()
        {
            Dictionary<string, CycleOutputKeySO> keyByLegacyId = new(StringComparer.Ordinal);
            string[] assetGuids = AssetDatabase.FindAssets(string.Empty, new[] { OUTPUT_KEY_ROOT_PATH });
            for (int assetIndex = 0; assetIndex < assetGuids.Length; assetIndex++)
            {
                CycleOutputKeySO outputKeySO = AssetDatabase.LoadAssetAtPath<CycleOutputKeySO>(
                    AssetDatabase.GUIDToAssetPath(assetGuids[assetIndex]));
                if (outputKeySO == null || string.IsNullOrWhiteSpace(outputKeySO.LegacyId))
                {
                    continue;
                }

                if (!keyByLegacyId.TryAdd(outputKeySO.LegacyId, outputKeySO))
                {
                    Echo.Error($"More than one output key declares legacy ID '{outputKeySO.LegacyId}'.");
                }
            }

            return keyByLegacyId;
        }

        private static DayNightEnvironmentOutputSchemaSO GetSchemaReference(MonoBehaviour applicator)
        {
            return (DayNightEnvironmentOutputSchemaSO)new SerializedObject(applicator)
                .FindProperty("_outputSchemaSO")
                .objectReferenceValue;
        }

        private static void EnsureMigrationFolderExists()
        {
            if (!AssetDatabase.IsValidFolder(MIGRATED_OUTPUT_KEY_FOLDER_PATH))
            {
                AssetDatabase.CreateFolder(OUTPUT_KEY_ROOT_PATH, "Migrated");
            }
        }

        private static string SanitizeFileName(string value)
        {
            string sanitizedValue = value;
            char[] invalidCharacters = Path.GetInvalidFileNameChars();
            for (int characterIndex = 0; characterIndex < invalidCharacters.Length; characterIndex++)
            {
                sanitizedValue = sanitizedValue.Replace(invalidCharacters[characterIndex], '_');
            }

            return sanitizedValue;
        }

        #endregion
    }
}
#endif
