using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FakeMG.FakeMGFramework.Editor.SceneSwitcher
{
    public class SceneSwitcherWindow : EditorWindow
    {
        public const string EDITOR_PREFS_KEY = "SceneSwitcher_ScenePaths";
        private const string MENU_PATH = "FakeMG/Scene Switcher";
        private const int MAX_SCENES = 9;

        public static event Action OnScenesUpdated;

        [SerializeField] private SceneAsset scene1;
        [SerializeField] private SceneAsset scene2;
        [SerializeField] private SceneAsset scene3;
        [SerializeField] private SceneAsset scene4;
        [SerializeField] private SceneAsset scene5;
        [SerializeField] private SceneAsset scene6;
        [SerializeField] private SceneAsset scene7;
        [SerializeField] private SceneAsset scene8;
        [SerializeField] private SceneAsset scene9;

        private SerializedObject _serializedObject;

        [MenuItem(MENU_PATH + "/Window")]
        public static void ShowWindow()
        {
            GetWindow<SceneSwitcherWindow>("Scene Switcher");
        }

        private void OnEnable()
        {
            _serializedObject = new SerializedObject(this);
            LoadSceneList();
        }

        private void OnGUI()
        {
            _serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Scene Assignments (Shift + 1-9 to switch):", EditorStyles.boldLabel);

            for (int i = 1; i <= MAX_SCENES; i++)
            {
                string propertyName = $"scene{i}";
                SerializedProperty sceneProperty = _serializedObject.FindProperty(propertyName);
                EditorGUILayout.PropertyField(sceneProperty, new GUIContent($"Scene {i}"));
            }

            if (_serializedObject.ApplyModifiedProperties())
            {
                SaveSceneList();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Use Shift + number keys (1-9) to quickly switch between assigned scenes.",
                MessageType.Info);
        }

        private void SaveSceneList()
        {
            var scenePaths = new List<string>();
            var sceneAssets = new[] { scene1, scene2, scene3, scene4, scene5, scene6, scene7, scene8, scene9 };

            foreach (var sceneAsset in sceneAssets)
            {
                if (sceneAsset)
                {
                    string path = AssetDatabase.GetAssetPath(sceneAsset);
                    scenePaths.Add(path);
                }
                else
                {
                    scenePaths.Add("");
                }
            }

            EditorPrefs.SetString(EDITOR_PREFS_KEY, string.Join(";", scenePaths));
            OnScenesUpdated?.Invoke();
        }

        private void LoadSceneList()
        {
            var scenePaths = EditorPrefs.GetString(EDITOR_PREFS_KEY, "").Split(';');

            for (int i = 0; i < MAX_SCENES && i < scenePaths.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(scenePaths[i]))
                {
                    var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePaths[i]);
                    switch (i)
                    {
                        case 0: scene1 = sceneAsset; break;
                        case 1: scene2 = sceneAsset; break;
                        case 2: scene3 = sceneAsset; break;
                        case 3: scene4 = sceneAsset; break;
                        case 4: scene5 = sceneAsset; break;
                        case 5: scene6 = sceneAsset; break;
                        case 6: scene7 = sceneAsset; break;
                        case 7: scene8 = sceneAsset; break;
                        case 8: scene9 = sceneAsset; break;
                    }
                }
            }
        }

        [MenuItem(MENU_PATH + "/Switch Scene 1 _#1")]
        public static void SwitchScene1()
        {
            SwitchToSceneByIndex(0);
        }

        [MenuItem(MENU_PATH + "/Switch Scene 2 _#2")]
        public static void SwitchScene2()
        {
            SwitchToSceneByIndex(1);
        }

        [MenuItem(MENU_PATH + "/Switch Scene 3 _#3")]
        public static void SwitchScene3()
        {
            SwitchToSceneByIndex(2);
        }

        [MenuItem(MENU_PATH + "/Switch Scene 4 _#4")]
        public static void SwitchScene4()
        {
            SwitchToSceneByIndex(3);
        }

        [MenuItem(MENU_PATH + "/Switch Scene 5 _#5")]
        public static void SwitchScene5()
        {
            SwitchToSceneByIndex(4);
        }

        [MenuItem(MENU_PATH + "/Switch Scene 6 _#6")]
        public static void SwitchScene6()
        {
            SwitchToSceneByIndex(5);
        }

        [MenuItem(MENU_PATH + "/Switch Scene 7 _#7")]
        public static void SwitchScene7()
        {
            SwitchToSceneByIndex(6);
        }

        [MenuItem(MENU_PATH + "/Switch Scene 8 _#8")]
        public static void SwitchScene8()
        {
            SwitchToSceneByIndex(7);
        }

        [MenuItem(MENU_PATH + "/Switch Scene 9 _#9")]
        public static void SwitchScene9()
        {
            SwitchToSceneByIndex(8);
        }

        private static void SwitchToSceneByIndex(int index)
        {
            var scenePaths = LoadSceneListStatic();

            if (index < scenePaths.Count && !string.IsNullOrWhiteSpace(scenePaths[index]))
            {
                TryOpenSceneStatic(scenePaths[index]);
            }
            else
            {
                Debug.LogWarning($"No scene assigned to slot {index + 1}");
            }
        }

        private static List<string> LoadSceneListStatic()
        {
            return EditorPrefs.GetString(EDITOR_PREFS_KEY, "")
                .Split(';')
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();
        }

        private static void TryOpenSceneStatic(string path)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(path);
            }
        }
    }
}