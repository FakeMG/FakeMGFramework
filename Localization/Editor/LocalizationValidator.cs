using System.Collections.Generic;
using System.Linq;
using FakeMG.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FakeMG.Localization.Editor
{
    public class LocalizationValidator : IPreprocessBuildWithReport
    {
        // Interface requirement: runs automatically before Build
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report) => ValidateAllKeys(true);

        [MenuItem(FakeMGEditorMenus.LOCALIZATION_VALIDATE_ALL_KEYS)]
        public static void ValidateAllKeysManual() => ValidateAllKeys(false);

        public static void ValidateAllKeys(bool silentOnSuccess)
        {
            var locType = LocKeysResolver.FindLocType();
            if (locType == null)
            {
                Echo.Error("Generated Loc class not found. Run Localization Sync first.");
                return;
            }

            var validKeys = LocKeysResolver.GetKeyFields()
                .Select(f => f.GetRawConstantValue().ToString())
                .ToHashSet();

            var locKeyFieldsByType = BuildLocKeyFieldCache();
            if (locKeyFieldsByType.Count == 0)
            {
                if (!silentOnSuccess)
                    Echo.Log("<color=green> No [LocKey] fields found. Nothing to validate.</color>");
                return;
            }

            int errorCount = 0;

            var settings = LocalizationSettingsSO.GetOrCreate();
            string searchPath = settings.ValidationSearchPath;

            string[] soGuids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { searchPath });
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { searchPath });
            string[] guids = soGuids.Union(prefabGuids).ToArray();

            try
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);

                    EditorUtility.DisplayProgressBar(
                        "Validating Localization Keys",
                        path,
                        (float)i / guids.Length);

                    errorCount += ValidateScriptableObject(path, locKeyFieldsByType, validKeys);
                    errorCount += ValidatePrefab(path, locKeyFieldsByType, validKeys);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (errorCount > 0)
            {
                EditorUtility.DisplayDialog("Localization Errors",
                    $"{errorCount} missing keys found! Check the Console for details.", "OK");
            }
            else if (!silentOnSuccess)
            {
                Echo.Log("<color=green> All keys validated successfully!</color>");
            }
        }

        private static Dictionary<System.Type, List<string>> BuildLocKeyFieldCache()
        {
            var cache = new Dictionary<System.Type, List<string>>();
            var fields = TypeCache.GetFieldsWithAttribute<LocKeyAttribute>();

            foreach (var field in fields)
            {
                if (!cache.TryGetValue(field.DeclaringType, out var fieldNames))
                {
                    fieldNames = new List<string>();
                    cache[field.DeclaringType] = fieldNames;
                }
                fieldNames.Add(field.Name);
            }

            return cache;
        }

        private static bool TryGetLocKeyFields(
            System.Type type,
            Dictionary<System.Type, List<string>> cache,
            out List<string> fieldNames)
        {
            while (type != null)
            {
                if (cache.TryGetValue(type, out fieldNames))
                    return true;
                type = type.BaseType;
            }

            fieldNames = null;
            return false;
        }

        private static int ValidateScriptableObject(
            string path,
            Dictionary<System.Type, List<string>> cache,
            HashSet<string> validKeys)
        {
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (!asset) return 0;
            if (!TryGetLocKeyFields(asset.GetType(), cache, out var fieldNames)) return 0;

            return ValidateFields(new SerializedObject(asset), fieldNames, validKeys, path, asset);
        }

        private static int ValidatePrefab(
            string path,
            Dictionary<System.Type, List<string>> cache,
            HashSet<string> validKeys)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab) return 0;

            int errors = 0;
            var components = prefab.GetComponentsInChildren<MonoBehaviour>(true);

            foreach (var component in components)
            {
                if (!component) continue;
                if (!TryGetLocKeyFields(component.GetType(), cache, out var fieldNames)) continue;

                errors += ValidateFields(new SerializedObject(component), fieldNames, validKeys, path, component);
            }

            return errors;
        }

        private static int ValidateFields(
            SerializedObject serializedObject,
            List<string> fieldNames,
            HashSet<string> validKeys,
            string assetPath,
            Object context)
        {
            int errors = 0;

            foreach (string fieldName in fieldNames)
            {
                var property = serializedObject.FindProperty(fieldName);
                if (property == null) continue;

                string currentKey = property.stringValue;
                if (string.IsNullOrEmpty(currentKey) || validKeys.Contains(currentKey)) continue;

                Echo.Error($"Missing Key '{currentKey}' found in <b>{assetPath}</b>", context: context);
                errors++;
            }

            return errors;
        }
    }
}