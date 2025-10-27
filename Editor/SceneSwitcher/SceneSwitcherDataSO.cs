using UnityEditor;
using UnityEngine;

namespace FakeMG.Framework.Editor.SceneSwitcher
{
    public class SceneSwitcherDataSO : ScriptableObject
    {
        private const string ASSET_NAME = "SceneSwitcherData.asset";

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
