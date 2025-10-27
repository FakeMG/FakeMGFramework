using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FakeMG.Framework.Editor
{
    [InitializeOnLoad]
    public static class AutoPlayBootstrapScene
    {
        private static bool s_isEnabled;
        private static string s_previousScene;

        static AutoPlayBootstrapScene()
        {
            s_isEnabled = EditorPrefs.GetBool("AutoPlayBootstrapScene_Enabled", false);
            Menu.SetChecked(FakeMGEditorMenus.AUTO_PLAY_BOOTSTRAP, s_isEnabled);
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!s_isEnabled) return;

            if (state == PlayModeStateChange.ExitingEditMode)
            {
                // Save the currently open scene
                s_previousScene = SceneManager.GetActiveScene().path;

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
                if (!string.IsNullOrEmpty(s_previousScene))
                {
                    EditorSceneManager.OpenScene(s_previousScene);
                }
            }
        }

        [MenuItem(FakeMGEditorMenus.AUTO_PLAY_BOOTSTRAP)]
        private static void ToggleEnabled()
        {
            s_isEnabled = !s_isEnabled;
            EditorPrefs.SetBool("AutoPlayBootstrapScene_Enabled", s_isEnabled);
            Menu.SetChecked(FakeMGEditorMenus.AUTO_PLAY_BOOTSTRAP, s_isEnabled);
            string status = s_isEnabled ? "<color=green>Enabled</color>" : "<color=red>Disabled</color>";
            Debug.Log($"Auto Play Bootstrap Scene: {status}");
        }
    }
}