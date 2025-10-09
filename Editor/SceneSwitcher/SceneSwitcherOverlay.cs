using System.IO;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace FakeMG.FakeMGFramework.Editor.SceneSwitcher
{
    [Overlay(typeof(SceneView), "Scene Switcher", true)]
    public class SceneSwitcherOverlay : Overlay
    {
        private VisualElement _sceneContainer;
        private SceneSwitcherDataSO _data;

        public override VisualElement CreatePanelContent()
        {
            _data = SceneSwitcherDataSO.GetOrCreate();

            var mainContainer = new VisualElement
            {
                style =
                {
                    minWidth = 0,
                    minHeight = 0
                }
            };

            _sceneContainer = new VisualElement
            {
                style =
                {
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

        private void RefreshSceneButtons()
        {
            if (_sceneContainer == null) return;

            _sceneContainer.Clear();

            if (_data == null)
            {
                _data = SceneSwitcherDataSO.GetOrCreate();
            }

            for (int i = 0; i < 9; i++)
            {
                var sceneAsset = _data.GetSceneAtIndex(i);
                if (!sceneAsset) continue;

                string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
                if (string.IsNullOrWhiteSpace(scenePath) || !File.Exists(scenePath)) continue;

                string label = Path.GetFileNameWithoutExtension(scenePath);
                int sceneNumber = i + 1;
                string capturedPath = scenePath;

                var button = new Button(() =>
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.OpenScene(capturedPath);
                    }
                })
                {
                    text = $"{sceneNumber}. {label}",
                    tooltip = $"Switch to {label} (Shift+{sceneNumber})",
                    style =
                    {
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

        public override void OnCreated()
        {
            base.OnCreated();
            EditorApplication.projectChanged += RefreshSceneButtons;
            SceneSwitcherWindow.OnScenesUpdated += RefreshSceneButtons;
        }

        public override void OnWillBeDestroyed()
        {
            base.OnWillBeDestroyed();
            EditorApplication.projectChanged -= RefreshSceneButtons;
            SceneSwitcherWindow.OnScenesUpdated -= RefreshSceneButtons;
        }
    }
}