using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FakeMG.Localization.Editor
{
    [Serializable]
    public class LocalizationTableConfig
    {
        [Tooltip("Path to the CSV file for this table")]
        [SerializeField] private string _csvPath;

        [Tooltip("Name of the Unity StringTableCollection")]
        [SerializeField] private string _tableCollectionName;

        public string CsvPath => _csvPath;
        public string TableCollectionName => _tableCollectionName;
    }

    public class LocalizationSettingsSO : ScriptableObject
    {
        private const string ASSET_NAME = "LocalizationSettings.asset";
        private const string DEFAULT_ASSET_PATH = "Assets/" + ASSET_NAME;

        [Header("Tables")]
        [SerializeField] private List<LocalizationTableConfig> _tableConfigs = new();

        [Header("Paths")]
        [SerializeField] private string _generatedClassPath = "Assets/Scripts/Generated/Loc.cs";
        [SerializeField] private string _validationSearchPath = "Assets/_Project";

        public IReadOnlyList<LocalizationTableConfig> TableConfigs => _tableConfigs;
        public string GeneratedClassPath => _generatedClassPath;
        public string ValidationSearchPath => _validationSearchPath;

        public static LocalizationSettingsSO GetOrCreate()
        {
            var settings = LoadFromDefaultPath();

            if (settings)
                return settings;

            settings = TryLoadFromAnyLocation();

            if (settings)
                return settings;

            return CreateAtDefaultPath();
        }

        private static LocalizationSettingsSO LoadFromDefaultPath()
        {
            return AssetDatabase.LoadAssetAtPath<LocalizationSettingsSO>(DEFAULT_ASSET_PATH);
        }

        private static LocalizationSettingsSO TryLoadFromAnyLocation()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(LocalizationSettingsSO)}");
            string selectedPath = null;

            foreach (string guid in guids)
            {
                string currentPath = AssetDatabase.GUIDToAssetPath(guid);

                if (string.IsNullOrEmpty(selectedPath) || string.CompareOrdinal(currentPath, selectedPath) < 0)
                    selectedPath = currentPath;
            }

            if (string.IsNullOrEmpty(selectedPath))
                return null;

            if (guids.Length > 1)
                Debug.LogWarning($"[Localization] Multiple {nameof(LocalizationSettingsSO)} assets found. Using {selectedPath}.");

            return AssetDatabase.LoadAssetAtPath<LocalizationSettingsSO>(selectedPath);
        }

        private static LocalizationSettingsSO CreateAtDefaultPath()
        {
            string assetPath = GetAvailableDestinationPath();
            var settings = CreateInstance<LocalizationSettingsSO>();

            AssetDatabase.CreateAsset(settings, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Localization] Created {nameof(LocalizationSettingsSO)} asset at {assetPath}");
            return settings;
        }

        private static string GetAvailableDestinationPath()
        {
            bool hasAssetAtDefaultPath = AssetDatabase.LoadMainAssetAtPath(DEFAULT_ASSET_PATH);

            if (hasAssetAtDefaultPath)
                return AssetDatabase.GenerateUniqueAssetPath(DEFAULT_ASSET_PATH);

            return DEFAULT_ASSET_PATH;
        }
    }
}
