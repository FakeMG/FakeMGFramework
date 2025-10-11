using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FakeMG.Framework.Database.Editor
{
    /// <summary>
    /// Editor utility for building and rebuilding AssetDatabaseSO instances
    /// </summary>
    public static class DatabaseRebuilderUtility
    {
        [MenuItem("FakeMG/Rebuild All Databases")]
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
                        Debug.Log($"[DatabaseRebuilder] Rebuilt database: {asset.name}");
                    }
                    else
                    {
                        Debug.LogWarning($"[DatabaseRebuilder] Could not find RebuildDatabase method for: {asset.name}");
                        errorCount++;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[DatabaseRebuilder] Error rebuilding {asset.name}: {ex.Message}");
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
            Debug.Log($"[DatabaseRebuilder] {summary}");
        }

        public static void BuildDatabase<TAsset, TDatabase>(AssetLabelReference labelReference, TDatabase database, Dictionary<string, TAsset> items)
            where TAsset : ItemSO
            where TDatabase : AssetDatabaseSO<TAsset>
        {
            items.Clear();

            var addressableAssetSettings = AddressableAssetSettingsDefaultObject.Settings;

            var labelEntries = new List<AddressableAssetEntry>();
            addressableAssetSettings.GetAllAssets(labelEntries, true);

            var filteredEntries = labelEntries.FindAll(entry => entry.labels.Contains(labelReference.labelString));

            foreach (var entry in filteredEntries)
            {
                var asset = AssetDatabase.LoadAssetAtPath<TAsset>(entry.AssetPath);
                if (asset == null) continue;

                string id = asset.ID;
                if (string.IsNullOrEmpty(id))
                {
                    Debug.LogWarning($"Skipping {entry.AssetPath}: Missing id.");
                    continue;
                }

                items[id] = asset;
            }

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();

            Debug.Log($"[DatabaseBuilder] {database.GetType().Name} built with {items.Count} items.");
        }

        private static bool IsAssetDatabase(System.Type type)
        {
            while (type != null)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(AssetDatabaseSO<>))
                {
                    return true;
                }
                type = type.BaseType;
            }
            return false;
        }
    }
}