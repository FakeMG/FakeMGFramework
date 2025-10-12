using UnityEditor;
using UnityEngine;

namespace FakeMG.Framework.Editor.SceneSwitcher
{
    public class SceneSwitcherDataSO : ScriptableObject
    {
        private const string ASSET_NAME = "SceneSwitcherData.asset";

        [SerializeField] private SceneAsset scene1;
        [SerializeField] private SceneAsset scene2;
        [SerializeField] private SceneAsset scene3;
        [SerializeField] private SceneAsset scene4;
        [SerializeField] private SceneAsset scene5;
        [SerializeField] private SceneAsset scene6;
        [SerializeField] private SceneAsset scene7;
        [SerializeField] private SceneAsset scene8;
        [SerializeField] private SceneAsset scene9;

        public SceneAsset Scene1 => scene1;
        public SceneAsset Scene2 => scene2;
        public SceneAsset Scene3 => scene3;
        public SceneAsset Scene4 => scene4;
        public SceneAsset Scene5 => scene5;
        public SceneAsset Scene6 => scene6;
        public SceneAsset Scene7 => scene7;
        public SceneAsset Scene8 => scene8;
        public SceneAsset Scene9 => scene9;

        public SceneAsset GetSceneAtIndex(int index)
        {
            return index switch
            {
                0 => scene1,
                1 => scene2,
                2 => scene3,
                3 => scene4,
                4 => scene5,
                5 => scene6,
                6 => scene7,
                7 => scene8,
                8 => scene9,
                _ => null
            };
        }

        public static SceneSwitcherDataSO GetOrCreate()
        {
            string scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(CreateInstance<SceneSwitcherDataSO>()));
            string scriptDirectory = System.IO.Path.GetDirectoryName(scriptPath);
            string assetPath = System.IO.Path.Combine(scriptDirectory, ASSET_NAME).Replace("\\", "/");

            var data = AssetDatabase.LoadAssetAtPath<SceneSwitcherDataSO>(assetPath);

            if (!data)
            {
                data = CreateInstance<SceneSwitcherDataSO>();
                AssetDatabase.CreateAsset(data, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"Created SceneSwitcherData asset at {assetPath}");
            }

            return data;
        }
    }
}
