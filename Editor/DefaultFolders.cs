using System.IO;
using UnityEditor;
using UnityEngine;

namespace FakeMG.Framework.Editor {
    public static class DefaultFolders {
        [MenuItem("Tools/Create Default Folders", priority = 200)]
        public static void CreateDefaultFolders() {
            CreateFolder("_Project/Feature", 
                "Script", 
                "Shader",
                "ScriptableObject", 
                "Prefab", 
                "Sprite", 
                "Texture",
                "Material",
                "Model",
                "Animation", 
                "Audio");
            
            AssetDatabase.Refresh();
        }

        private static void CreateFolder(string root, params string[] dir) {
            var fullPath = Path.Combine(Application.dataPath, root);
            foreach (var newDirectory in dir) {
                Directory.CreateDirectory(Path.Combine(fullPath, newDirectory));
            }
        }

        [MenuItem("Tools/Add Necessary Packages", priority = 200)]
        public static void AddNecessaryPackages() {
            UnityEditor.PackageManager.Client.Add("com.unity.cinemachine");
            UnityEditor.PackageManager.Client.Add("com.unity.addressables");
            UnityEditor.PackageManager.Client.Add("com.unity.localization");
        }
    }
}