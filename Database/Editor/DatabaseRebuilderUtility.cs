#if UNITY_EDITOR
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace FakeMG.Framework.Database.Editor
{
    /// <summary>
    /// Editor utility for building and rebuilding AssetDatabaseSO instances
    /// </summary>
    public static class DatabaseRebuilderUtility
    {
        private const string ASSET_SEARCH_FILTER_FORMAT = "t:{0}";
        private const string ASSETS_FOLDER = "Assets/_Project";

        [MenuItem(FakeMGEditorMenus.REBUILD_ALL_DATABASES)]
        public static void RebuildAllDatabases()
        {
            if (!EditorUtility.DisplayDialog(
                    "Rebuild All Databases",
                    "This will rebuild all AssetDatabaseSO instances in the project. Continue?",
                    "Rebuild",
                    "Cancel"))
            {
                return;
            }

            var databaseGuids = AssetDatabase.FindAssets($"t:{nameof(SerializedScriptableObject)}");
            var rebuiltCount = 0;
            var skippedCount = 0;
            var errorCount = 0;
            var rebuiltDatabases = new List<string>();

            foreach (var guid in databaseGuids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);

                if (asset == null)
                {
                    skippedCount++;
                    continue;
                }

                // Check if the asset is derived from AssetDatabaseSO
                var assetType = asset.GetType();
                if (!IsAssetDatabase(assetType))
                {
                    skippedCount++;
                    continue;
                }

                try
                {
                    // Use reflection to call the private RebuildDatabase method
                    var rebuildMethod = assetType.BaseType?.GetMethod("RebuildDatabase",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (rebuildMethod != null)
                    {
                        rebuildMethod.Invoke(asset, null);
                        rebuiltCount++;
                        rebuiltDatabases.Add(asset.name);
                        Echo.Log($"Rebuilt database: {asset.name}");
                    }
                    else
                    {
                        Echo.Warning($"Could not find RebuildDatabase method for: {asset.name}");
                        errorCount++;
                    }
                }
                catch (System.Exception ex)
                {
                    Echo.Error($"Error rebuilding {asset.name}: {ex.Message}");
                    errorCount++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var summary = $"Database Rebuild Complete!\n\n" +
                          $"Rebuilt: {rebuiltCount}\n" +
                          $"Skipped: {skippedCount}\n" +
                          $"Errors: {errorCount}";

            if (rebuiltDatabases.Count > 0)
            {
                summary += $"\n\nRebuilt Databases:\n- {string.Join("\n- ", rebuiltDatabases)}";
            }

            EditorUtility.DisplayDialog("Rebuild Complete", summary, "OK");
            Echo.Log($"{summary}");
        }

        public static void BuildDatabase<TAsset, TDatabase>(TDatabase database, Dictionary<string, TAsset> items)
            where TAsset : ScriptableObject, IIdentifiable
            where TDatabase : DatabaseSO<TAsset>
        {
            items.Clear();

            var typeName = typeof(TAsset).Name;
            var filter = string.Format(ASSET_SEARCH_FILTER_FORMAT, typeName);
            var assetGuids = AssetDatabase.FindAssets(filter, new[] { ASSETS_FOLDER });

            foreach (var guid in assetGuids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<TAsset>(assetPath);
                if (!asset) continue;

                string id = asset.ID;
                if (string.IsNullOrEmpty(id))
                {
                    Echo.Warning($"Skipping {assetPath}: Missing id.");
                    continue;
                }

                items[id] = asset;
            }

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();

            Echo.Log($"{database.GetType().Name} built with {items.Count} items.");
        }

        private static bool IsAssetDatabase(System.Type type)
        {
            while (type != null)
            {
                if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(DatabaseSO<>)))
                {
                    return true;
                }
                type = type.BaseType;
            }
            return false;
        }
    }
}
#endif