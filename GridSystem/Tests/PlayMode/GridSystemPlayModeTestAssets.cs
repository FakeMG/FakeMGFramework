using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FakeMG.GridSystem.Tests.PlayMode
{
    internal static class GridSystemPlayModeTestAssets
    {
        public const string GRID_MANAGER_PREFAB_GUID = "c400609af128a0c4c906ebd10534065b";
        public const string GRID_FOOTPRINT_PREFAB_GUID = "6ad3613ca4f4f654284043be56fa7039";
        public const string CAMERA_PREFAB_GUID = "649180d0c3b9a904b9dca0cbfec00cdc";
        public const string PROJECTION_PREFAB_GUID = "d9e9129db79f26c4c993c7d519174da2";
        public const string FACTORY_STRUCTURE_SO_GUID = "bc9e134a9391e1a46b0813c8560c44d8";

        #region Public Methods

        public static T LoadPrefabComponent<T>(string prefabGuid) where T : Component
        {
            GameObject prefab = LoadAsset<GameObject>(prefabGuid);
            T component = prefab.GetComponentInChildren<T>(true);
            Assert.IsNotNull(component, $"Framework fixture '{prefab.name}' requires {typeof(T).Name}.");
            return component;
        }

        public static T LoadAsset<T>(string assetGuid) where T : Object
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            Assert.IsNotNull(asset, $"Missing framework {typeof(T).Name} fixture with GUID {assetGuid}.");
            return asset;
        }

        #endregion
    }
}
