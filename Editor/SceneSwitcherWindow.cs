using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FakeMG.Framework.Editor {
    public class SceneSwitcherWindow : EditorWindow {
        private const string EDITOR_PREFS_KEY = "SceneSwitcher_ScenePaths";
        private List<string> _scenePaths = new();

        [MenuItem("Tools/Scene Switcher")]
        public static void ShowWindow() {
            GetWindow<SceneSwitcherWindow>("Scene Switcher");
        }

        private void OnEnable() {
            LoadSceneList();
        }

        private void OnGUI() {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Drag & Drop Scenes Below", EditorStyles.boldLabel);
            HandleDragAndDrop();

            if (_scenePaths.Count == 0) {
                EditorGUILayout.HelpBox("No scenes assigned. Drag scene assets here.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Click to Switch Scene:", EditorStyles.boldLabel);

            for (int i = 0; i < _scenePaths.Count; i++) {
                var path = _scenePaths[i];
                if (!File.Exists(path)) continue;

                string sceneName = Path.GetFileNameWithoutExtension(path);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button($"{i + 1}. {sceneName}", GUILayout.Height(30))) {
                    TryOpenScene(path);
                }

                if (GUILayout.Button("X", GUILayout.Width(30))) {
                    _scenePaths.RemoveAt(i);
                    SaveSceneList();
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();
            }

            HandleShortcuts();
        }

        private void HandleDragAndDrop() {
            var evt = Event.current;
            var dropArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drop scenes here", EditorStyles.helpBox);

            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform) {
                if (!dropArea.Contains(evt.mousePosition)) return;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (evt.type == EventType.DragPerform) {
                    DragAndDrop.AcceptDrag();
                    foreach (var obj in DragAndDrop.objectReferences) {
                        var path = AssetDatabase.GetAssetPath(obj);
                        if (path.EndsWith(".unity") && !_scenePaths.Contains(path)) {
                            _scenePaths.Add(path);
                        }
                    }

                    SaveSceneList();
                }

                evt.Use();
            }
        }

        private void TryOpenScene(string path) {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) {
                EditorSceneManager.OpenScene(path);
            }
        }

        private void SaveSceneList() {
            EditorPrefs.SetString(EDITOR_PREFS_KEY, string.Join(";", _scenePaths));
        }

        private void LoadSceneList() {
            _scenePaths = EditorPrefs.GetString(EDITOR_PREFS_KEY, "")
                .Split(';')
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();
        }

        private void HandleShortcuts() {
            var evt = Event.current;
            if (evt.type == EventType.KeyDown && evt.alt) {
                for (int i = 0; i < Mathf.Min(9, _scenePaths.Count); i++) {
                    if (evt.keyCode == KeyCode.Alpha1 + i) {
                        TryOpenScene(_scenePaths[i]);
                        evt.Use();
                    }
                }
            }
        }
    }
}