#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FakeMG.Framework.Database.Editor
{
    public class DatabaseAssetPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (var path in importedAssets)
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null) continue;

                Type assetType = asset.GetType();

                // Only care about concrete DatabaseSO subclasses
                if (!IsConcreteDatabaseType(assetType)) continue;

                var guids = AssetDatabase.FindAssets($"t:{assetType.Name}");
                if (guids.Length <= 1) continue;

                // Collect duplicates (everything that isn't the newly imported asset)
                var duplicatePaths = new List<string>();
                foreach (var guid in guids)
                {
                    string existingPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (existingPath != path)
                        duplicatePaths.Add(existingPath);
                }

                if (duplicatePaths.Count == 0) continue;

                bool deleteNew = EditorUtility.DisplayDialog(
                    title: $"Duplicate Database Asset: {assetType.Name}",
                    message: $"An asset of type {assetType.Name} already exists at:\n\n" +
                             $"{string.Join("\n", duplicatePaths)}\n\n" +
                             $"Only one is allowed. Delete the new one?",
                    ok: "Delete New",
                    cancel: "Keep Both (Not Recommended)"
                );

                if (deleteNew)
                {
                    AssetDatabase.DeleteAsset(path);
                    Debug.LogWarning($"[Database] Deleted duplicate {assetType.Name} at {path}.");
                }
                else
                {
                    Debug.LogError(
                        $"[Database] Duplicate {assetType.Name} kept. " +
                        $"This WILL cause undefined behaviour. Clean it up."
                    );
                }
            }
        }

        private static bool IsConcreteDatabaseType(Type type)
        {
            if (type.IsAbstract) return false;

            while (type != null && type != typeof(object))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(DatabaseSO<>))
                    return true;
                type = type.BaseType;
            }

            return false;
        }
    }
}
#endif
