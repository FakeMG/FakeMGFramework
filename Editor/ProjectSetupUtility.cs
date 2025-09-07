using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace FakeMG.FakeMGFramework.Editor
{
    public static class ProjectSetupUtility
    {
        [MenuItem("FakeMG/Create Default Folders")]
        public static void CreateDefaultFolders()
        {
            CreateFolder("_Project/Feature",
                "Shaders",
                "ScriptableObjects",
                "Prefabs",
                "Sprites",
                "Textures",
                "Materials",
                "Models",
                "Animations",
                "Audios");

            AssetDatabase.Refresh();
        }

        private static void CreateFolder(string root, params string[] dir)
        {
            var fullPath = Path.Combine(Application.dataPath, root);
            foreach (var newDirectory in dir)
            {
                Directory.CreateDirectory(Path.Combine(fullPath, newDirectory));
            }
        }

        [MenuItem("FakeMG/Add Necessary Packages")]
        public static void AddNecessaryPackages()
        {
            Client.Add("com.unity.cinemachine");
            Client.Add("com.unity.addressables");
            Client.Add("com.unity.localization");
        }
    }
}