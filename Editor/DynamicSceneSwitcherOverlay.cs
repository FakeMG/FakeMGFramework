using System.IO;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace FakeMG.FakeMGFramework.Editor {
    [Overlay(typeof(SceneView), "Scene Switcher (Auto)", true)]
    public class DynamicSceneSwitcherOverlay : Overlay {
        public override VisualElement CreatePanelContent() {
            var container = new VisualElement {
                style = {
                    flexDirection = FlexDirection.Row,
                    flexWrap = Wrap.Wrap,
                    paddingTop = 4,
                    paddingBottom = 4,
                    paddingLeft = 4,
                    paddingRight = 4
                }
            };
            // container.style.gap = 4;

            var scenes = EditorBuildSettings.scenes;

            foreach (var scene in scenes) {
                if (!scene.enabled) continue;

                string scenePath = scene.path;
                string label = Path.GetFileNameWithoutExtension(scenePath);

                var button = new Button(() => {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) {
                        EditorSceneManager.OpenScene(scenePath);
                    }
                }) {
                    text = label,
                    tooltip = $"Switch to {label}",
                    style = {
                        paddingLeft = 6,
                        paddingRight = 6,
                        unityFontStyleAndWeight = FontStyle.Bold
                    }
                };

                container.Add(button);
            }

            return container;
        }
    }
}