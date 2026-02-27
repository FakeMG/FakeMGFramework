using UnityEditor;
using UnityEngine;

namespace FakeMG.Framework.Editor.SceneSwitcher
{
    public class SceneSwitcherDataSO : ScriptableObject
    {
        private const string ASSET_NAME = "SceneSwitcherData.asset";
        private const string DEFAULT_ASSET_PATH = "Assets/" + ASSET_NAME;

        [SerializeField] private SceneAsset _scene1;
        [SerializeField] private SceneAsset _scene2;
        [SerializeField] private SceneAsset _scene3;
        [SerializeField] private SceneAsset _scene4;
        [SerializeField] private SceneAsset _scene5;
        [SerializeField] private SceneAsset _scene6;
        [SerializeField] private SceneAsset _scene7;
        [SerializeField] private SceneAsset _scene8;
        [SerializeField] private SceneAsset _scene9;

        public SceneAsset Scene1 => _scene1;
        public SceneAsset Scene2 => _scene2;
        public SceneAsset Scene3 => _scene3;
        public SceneAsset Scene4 => _scene4;
        public SceneAsset Scene5 => _scene5;
        public SceneAsset Scene6 => _scene6;
        public SceneAsset Scene7 => _scene7;
        public SceneAsset Scene8 => _scene8;
        public SceneAsset Scene9 => _scene9;

        public SceneAsset GetSceneAtIndex(int index)
        {
            return index switch
            {
                0 => _scene1,
                1 => _scene2,
                2 => _scene3,
                3 => _scene4,
                4 => _scene5,
                5 => _scene6,
                6 => _scene7,
                7 => _scene8,
                8 => _scene9,
                _ => null
            };
        }

        public static SceneSwitcherDataSO GetOrCreate()
        {
            var data = LoadFromDefaultPath();

            if (data)
            {
                return data;
            }

            data = TryLoadFromAnyLocation();

            if (data)
            {
                return data;
            }

            return CreateAtDefaultPath();
        }

        private static SceneSwitcherDataSO LoadFromDefaultPath()
        {
            var data = AssetDatabase.LoadAssetAtPath<SceneSwitcherDataSO>(DEFAULT_ASSET_PATH);
            return data;
        }

        private static SceneSwitcherDataSO TryLoadFromAnyLocation()
        {
            string[] assetGuids = AssetDatabase.FindAssets($"t:{nameof(SceneSwitcherDataSO)}");
            string selectedPath = null;

            foreach (string guid in assetGuids)
            {
                string currentPath = AssetDatabase.GUIDToAssetPath(guid);

                if (string.IsNullOrEmpty(selectedPath) || string.CompareOrdinal(currentPath, selectedPath) < 0)
                {
                    selectedPath = currentPath;
                }
            }

            if (string.IsNullOrEmpty(selectedPath))
            {
                return null;
            }

            if (assetGuids.Length > 1)
            {
                Debug.LogWarning($"[SceneSwitcher] Multiple {nameof(SceneSwitcherDataSO)} assets found. Using {selectedPath}.");
            }

            return AssetDatabase.LoadAssetAtPath<SceneSwitcherDataSO>(selectedPath);
        }

        private static SceneSwitcherDataSO CreateAtDefaultPath()
        {
            string assetPath = GetAvailableDestinationPath();
            var data = CreateInstance<SceneSwitcherDataSO>();

            AssetDatabase.CreateAsset(data, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[SceneSwitcher] Created SceneSwitcherData asset at {assetPath}");
            return data;
        }

        private static string GetAvailableDestinationPath()
        {
            bool hasAssetAtDefaultPath = AssetDatabase.LoadMainAssetAtPath(DEFAULT_ASSET_PATH);

            if (hasAssetAtDefaultPath)
            {
                return AssetDatabase.GenerateUniqueAssetPath(DEFAULT_ASSET_PATH);
            }

            return DEFAULT_ASSET_PATH;
        }
    }
}