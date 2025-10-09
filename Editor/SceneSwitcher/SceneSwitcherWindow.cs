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
        private const string MENU_PATH = "FakeMG/Scene Switcher";
        private const int MAX_SCENES = 9;

        public static event Action OnScenesUpdated;

        private SceneSwitcherDataSO _data;
        private SerializedObject _serializedObject;

        [MenuItem(MENU_PATH + "/Window")]
        public static void ShowWindow()
        {
            GetWindow<SceneSwitcherWindow>("Scene Switcher");
        }

        private void OnEnable()
        {
            _data = SceneSwitcherDataSO.GetOrCreate();
            _serializedObject = new SerializedObject(_data);
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
                EditorUtility.SetDirty(_data);
                AssetDatabase.SaveAssets();
                OnScenesUpdated?.Invoke();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Use Shift + number keys (1-9) to quickly switch between assigned scenes.",
                MessageType.Info);
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
            var data = SceneSwitcherDataSO.GetOrCreate();
            var sceneAsset = data.GetSceneAtIndex(index);

            if (sceneAsset)
            {
                string path = AssetDatabase.GetAssetPath(sceneAsset);
                TryOpenSceneStatic(path);
            }
            else
            {
                Debug.LogWarning($"No scene assigned to slot {index + 1}");
            }
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