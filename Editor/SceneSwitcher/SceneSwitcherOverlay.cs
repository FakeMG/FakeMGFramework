using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace FakeMG.FakeMGFramework.Editor.SceneSwitcher {
    [Overlay(typeof(SceneView), "Scene Switcher", true)]
    public class SceneSwitcherOverlay : Overlay {
        private VisualElement _sceneContainer;

        public override VisualElement CreatePanelContent() {
            var mainContainer = new VisualElement {
                style = {
                    minWidth = 0,
                    minHeight = 0
                }
            };

            _sceneContainer = new VisualElement {
                style = {
                    flexDirection = FlexDirection.Row,
                    flexWrap = Wrap.Wrap,
                    paddingTop = 4,
                    paddingBottom = 4,
                    paddingLeft = 4,
                    paddingRight = 4
                }
            };

            mainContainer.Add(_sceneContainer);

            RefreshSceneButtons();

            return mainContainer;
        }

        private void RefreshSceneButtons() {
            _sceneContainer.Clear();

            var scenePaths = GetScenePathsFromWindow();
            
            for (int i = 0; i < scenePaths.Count; i++) {
                string scenePath = scenePaths[i];
                if (string.IsNullOrWhiteSpace(scenePath) || !File.Exists(scenePath)) continue;

                string label = Path.GetFileNameWithoutExtension(scenePath);
                int sceneNumber = i + 1;

                var button = new Button(() => {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) {
                        EditorSceneManager.OpenScene(scenePath);
                    }
                }) {
                    text = $"{sceneNumber}. {label}",
                    tooltip = $"Switch to {label} (Shift+{sceneNumber})",
                    style = {
                        paddingLeft = 6,
                        paddingRight = 6,
                        marginRight = 2,
                        marginBottom = 2,
                        unityFontStyleAndWeight = FontStyle.Bold
                    }
                };

                _sceneContainer.Add(button);
            }
        }

        private List<string> GetScenePathsFromWindow() {
            return EditorPrefs.GetString(SceneSwitcherWindow.EDITOR_PREFS_KEY, "")
                .Split(';')
                .ToList();
        }

        public override void OnCreated() {
            base.OnCreated();
            EditorApplication.projectChanged += RefreshSceneButtons;
            SceneSwitcherWindow.OnScenesUpdated += RefreshSceneButtons;
        }

        public override void OnWillBeDestroyed() {
            base.OnWillBeDestroyed();
            EditorApplication.projectChanged -= RefreshSceneButtons;
            SceneSwitcherWindow.OnScenesUpdated -= RefreshSceneButtons;
        }
    }
}