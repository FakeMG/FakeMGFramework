using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FakeMG.FakeMGFramework.Editor
{
    [InitializeOnLoad]
    public static class AutoPlayBootstrapScene
    {
        private const string MENU_PATH = "FakeMG/Auto Play Bootstrap Scene";
        private static bool _isEnabled;
        private static string _previousScene;

        static AutoPlayBootstrapScene()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                // Save the currently open scene
                _previousScene = SceneManager.GetActiveScene().path;

                // Ensure there's at least one scene in Build Settings
                if (EditorBuildSettings.scenes.Length > 0)
                {
                    string firstScenePath = EditorBuildSettings.scenes[0].path;

                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.OpenScene(firstScenePath);
                    }
                    else
                    {
                        EditorApplication.isPlaying = false;
                    }
                }
                else
                {
                    Debug.LogError("No scenes in Build Settings! Add at least one scene.");
                    EditorApplication.isPlaying = false;
                }
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                // Restore the previous scene after stopping
                if (!string.IsNullOrEmpty(_previousScene))
                {
                    EditorSceneManager.OpenScene(_previousScene);
                }
            }
        }

        [MenuItem(MENU_PATH)]
        private static void ToggleEnabled()
        {
            _isEnabled = !_isEnabled;
            EditorPrefs.SetBool("AutoPlayBootstrapScene_Enabled", _isEnabled);
            Menu.SetChecked(MENU_PATH, _isEnabled);
            Debug.Log("Auto Play Bootstrap Scene: " + (_isEnabled ? "Enabled" : "Disabled"));
        }
    }
}