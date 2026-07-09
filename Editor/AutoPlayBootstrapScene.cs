using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FakeMG.Framework.Editor
{
    [InitializeOnLoad]
    public static class AutoPlayBootstrapScene
    {
        private const string EnabledKey = "AutoPlayBootstrapScene_Enabled";

        private static bool s_isEnabled;
        private static string s_previousScene;

        static AutoPlayBootstrapScene()
        {
            s_isEnabled = EditorPrefs.GetBool(EnabledKey, false);

            // Menu items aren't registered yet during domain load, so SetChecked here is a no-op.
            EditorApplication.delayCall += RefreshMenuCheckmark;

            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!s_isEnabled)
            {
                return;
            }

            if (state == PlayModeStateChange.ExitingEditMode)
            {
                if (ShouldSkipAutoBootstrap())
                {
                    return;
                }

                s_previousScene = SceneManager.GetActiveScene().path;

                if (!TryGetBootstrapScenePath(out string bootstrapScenePath))
                {
                    Debug.LogError("No enabled scenes in Build Settings! Add at least one enabled scene.");
                    EditorApplication.isPlaying = false;
                    return;
                }

                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(bootstrapScenePath, OpenSceneMode.Single);
                }
                else
                {
                    EditorApplication.isPlaying = false;
                }
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                if (!string.IsNullOrEmpty(s_previousScene))
                {
                    EditorSceneManager.OpenScene(s_previousScene, OpenSceneMode.Single);
                    s_previousScene = null;
                }
            }
        }

        private static bool ShouldSkipAutoBootstrap()
        {
            if (IsCommandLineTestRun())
            {
                return true;
            }

            Scene activeScene = SceneManager.GetActiveScene();

            if (IsUnityGeneratedPlayModeTestScene(activeScene))
            {
                return true;
            }

            return false;
        }

        private static bool IsCommandLineTestRun()
        {
            string[] args = Environment.GetCommandLineArgs();

            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "-runTests", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsUnityGeneratedPlayModeTestScene(Scene scene)
        {
            if (!scene.IsValid())
            {
                return false;
            }

            string path = scene.path.Replace('\\', '/');

            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            string fileName = Path.GetFileNameWithoutExtension(path);

            const string testScenePrefix = "InitTestScene";

            if (!fileName.StartsWith(testScenePrefix, StringComparison.Ordinal))
            {
                return false;
            }

            string guidPart = fileName.Substring(testScenePrefix.Length);

            return Guid.TryParse(guidPart, out _);
        }

        private static bool TryGetBootstrapScenePath(out string scenePath)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

            for (int i = 0; i < scenes.Length; i++)
            {
                EditorBuildSettingsScene scene = scenes[i];

                if (scene.enabled && !string.IsNullOrEmpty(scene.path))
                {
                    scenePath = scene.path;
                    return true;
                }
            }

            scenePath = null;
            return false;
        }

        [MenuItem(FakeMGEditorMenus.AUTO_PLAY_BOOTSTRAP)]
        private static void ToggleEnabled()
        {
            s_isEnabled = !s_isEnabled;

            EditorPrefs.SetBool(EnabledKey, s_isEnabled);
            RefreshMenuCheckmark();
            string status = s_isEnabled ? "<color=green>Enabled</color>" : "<color=red>Disabled</color>";
            Debug.Log($"Auto Play Bootstrap Scene: {status}");
        }

        private static void RefreshMenuCheckmark()
        {
            Menu.SetChecked(FakeMGEditorMenus.AUTO_PLAY_BOOTSTRAP, s_isEnabled);
        }
    }
}